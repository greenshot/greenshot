/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// Builds the WinForms menus of the editor (toolbar drop downs, File menu) from destination descriptors.
    /// The tray menu and the destination picker are WPF, see Greenshot.Base.Wpf.DestinationPicker.
    /// Must be used on the UI thread; icons and dynamic destinations are loaded asynchronously, the menu is updated when they arrive.
    /// </summary>
    public static class DestinationMenuBuilder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationMenuBuilder));
        private static readonly KeysConverter ShortcutConverter = new KeysConverter();

        /// <summary>
        /// The shortcut of the descriptor ("Ctrl+Shift+C") as WinForms keys, Keys.None when there is none.
        /// </summary>
        public static Keys ToKeys(string shortcut)
        {
            if (string.IsNullOrEmpty(shortcut))
            {
                return Keys.None;
            }

            try
            {
                return (Keys)ShortcutConverter.ConvertFromInvariantString(shortcut);
            }
            catch (Exception ex)
            {
                Log.Warn($"Invalid shortcut '{shortcut}'", ex);
                return Keys.None;
            }
        }

        /// <summary>
        /// Show the icon of the key on the item, as soon as it is available (right away for built in icons).
        /// </summary>
        public static void AssignIcon(ToolStripItem item, string iconKey)
        {
            if (item == null || string.IsNullOrEmpty(iconKey))
            {
                return;
            }

            LoadIconAsync(item, iconKey).FireAndLog($"Load icon {iconKey}", Log);
        }

        private static async Task LoadIconAsync(ToolStripItem item, string iconKey)
        {
            // Completes synchronously for icons which don't need to wait, the continuation stays on the UI thread
            var image = await DestinationIcons.GetIconAsync(iconKey).ConfigureAwait(true);
            if (image == null)
            {
                return;
            }

            if (item.IsDisposed)
            {
                image.Dispose();
                return;
            }

            item.AssignAutoDisposingImage(image, needsClone: false);
        }

        /// <summary>
        /// Create the menu item for the destination, dynamic destinations are loaded right away and added as sub items when they arrive.
        /// </summary>
        /// <param name="destination">IDestination</param>
        /// <param name="captureDetails">The capture the menu is for, null when there is none</param>
        /// <param name="onClick">Called with the clicked (sub) destination</param>
        /// <param name="addDynamics">Add the dynamic destinations</param>
        public static ToolStripMenuItem CreateMenuItem(IDestination destination, ICaptureDetails captureDetails, Action<IDestination> onClick, bool addDynamics = true)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            var descriptor = destination.Descriptor;
            var menuItem = new ToolStripMenuItem(descriptor.DisplayName)
            {
                Tag = destination
            };
            AssignIcon(menuItem, descriptor.IconKey);
            menuItem.Click += (_, _) =>
            {
                if (menuItem.Tag is IDestination clicked)
                {
                    onClick?.Invoke(clicked);
                }
            };

            if (descriptor.HasDynamicDestinations && addDynamics)
            {
                // Menus are built right before they are shown: the dynamic destinations are loaded in the background right away (not when the sub menu opens),
                // so they are usually in place before the user reaches the item. Until then a placeholder shows that there is more.
                var placeholder = new ToolStripMenuItem("…") { Enabled = false };
                menuItem.DropDownItems.Add(placeholder);
                AddDynamicDestinationsAsync(menuItem, destination, captureDetails, onClick).FireAndLog($"Dynamic destinations of {destination.Designation}", Log);
            }

            return menuItem;
        }

        private static async Task AddDynamicDestinationsAsync(ToolStripMenuItem menuItem, IDestination destination, ICaptureDetails captureDetails, Action<IDestination> onClick)
        {
            var ui = UiDispatcher.Current;
            var subDestinations = await LoadDynamicDestinationsAsync(destination, captureDetails).ConfigureAwait(false);
            // Back to the UI thread for the menu, the menu is shown by now (or even closed again)
            await ui.InvokeAsync(() => ApplyDynamicDestinations(menuItem, destination, subDestinations, captureDetails, onClick), CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Load the dynamic destinations in the background, see <see cref="DestinationHelper.LoadDynamicDestinationsAsync"/>
        /// </summary>
        public static Task<IReadOnlyList<IDestination>> LoadDynamicDestinationsAsync(IDestination destination, ICaptureDetails captureDetails) =>
            DestinationHelper.LoadDynamicDestinationsAsync(destination, captureDetails);

        private static void ApplyDynamicDestinations(ToolStripMenuItem menuItem, IDestination destination, IReadOnlyList<IDestination> subDestinations, ICaptureDetails captureDetails, Action<IDestination> onClick)
        {
            if (menuItem.IsDisposed)
            {
                return;
            }

            UpdateDropDownItems(menuItem, () => ReplaceWithDynamicDestinations(menuItem, destination, subDestinations, captureDetails, onClick));
        }

        /// <summary>
        /// Change the drop down items of the item, on the UI thread.
        /// Changing the items of a drop down while it is shown leaves it with a wrong size and location (an empty line, or at the top left of the screen):
        /// it is hidden while the items change, and shown again when there is something to show.
        /// </summary>
        /// <param name="item">The item with the drop down</param>
        /// <param name="update">Changes the items</param>
        /// <param name="beforeReopen">Called before the drop down is shown again (its DropDownOpening event is raised again)</param>
        public static void UpdateDropDownItems(ToolStripDropDownItem item, Action update, Action beforeReopen = null)
        {
            if (item == null || item.IsDisposed)
            {
                return;
            }

            bool wasShown = item.DropDown.Visible;
            if (wasShown)
            {
                item.HideDropDown();
            }

            update();

            if (wasShown && !item.IsDisposed && item.HasDropDownItems && item.Owner is { Visible: true })
            {
                beforeReopen?.Invoke();
                item.ShowDropDown();
            }
        }

        private static void ReplaceWithDynamicDestinations(ToolStripMenuItem menuItem, IDestination destination, IReadOnlyList<IDestination> subDestinations, ICaptureDetails captureDetails, Action<IDestination> onClick)
        {
            var previousItems = menuItem.DropDownItems.Cast<ToolStripItem>().ToList();
            menuItem.DropDownItems.Clear();
            foreach (var previousItem in previousItems)
            {
                previousItem.Dispose();
            }

            var validSubDestinations = (subDestinations ?? Array.Empty<IDestination>()).Where(d => d != null).OrderBy(d => d, DestinationComparer.Instance).ToList();
            if (destination.Descriptor.UseDynamicsOnly && validSubDestinations.Count == 1)
            {
                // Only one: the item itself becomes that destination, without a sub menu
                menuItem.Tag = validSubDestinations[0];
                menuItem.Text = validSubDestinations[0].Descriptor.DisplayName;
                return;
            }

            // No dynamic destinations: the item stays a plain item for the destination itself
            foreach (var subDestination in validSubDestinations)
            {
                menuItem.DropDownItems.Add(CreateMenuItem(subDestination, captureDetails, onClick, false));
            }
        }
    }
}
