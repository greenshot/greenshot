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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// Builds WinForms menus from destination descriptors (the destinations don't know about menus anymore).
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
        /// Load the dynamic destinations in the background: some do slow work before their first await
        /// (the printers are enumerated synchronously, Office starts its COM thread), the menu must not wait for that.
        /// </summary>
        public static async Task<IReadOnlyList<IDestination>> LoadDynamicDestinationsAsync(IDestination destination, ICaptureDetails captureDetails)
        {
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            try
            {
                return await destination.GetDynamicDestinationsAsync(captureDetails, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Fixing Bug #3536968: skip the dynamic destinations when there is an error
                Log.ErrorFormat("Skipping the dynamic destinations of {0}, due to the following error: {1}", destination.Designation, ex.Message);
                return Array.Empty<IDestination>();
            }
        }

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

        /// <summary>
        /// Show the destination picker (a context menu at the cursor) and return the destination the user clicked, null when the user closed it.
        /// Runs on the UI thread, completes when the menu closes.
        /// </summary>
        public static Task<IDestination> ShowPickerAsync(IReadOnlyList<IDestination> destinations, ICaptureDetails captureDetails, CancellationToken cancellationToken = default)
        {
            ThreadAssert.IsUi(nameof(ShowPickerAsync));
            var picked = Tcs.Create<IDestination>();
            var coreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
            var menu = new ContextMenuStrip
            {
                ImageScalingSize = coreConfig.IconSize,
                Tag = null,
                TopLevel = true,
                // set new default font, so we are allowed to dispose it later, we will scale it later on the Opening event
                Font = new Font(FontFamily.GenericSansSerif, 9)
            };

            void Complete(IDestination destination)
            {
                if (!picked.TrySetResult(destination))
                {
                    return;
                }

                menu.Tag = destination?.Designation ?? "closed";
                CloseLater();
            }

            void CloseLater()
            {
                // We might be in the closing process, dispose later (on the UI thread) to avoid re-entrancy
                UiDispatcher.Current.InvokeAsync(() =>
                {
                    if (!menu.IsDisposed)
                    {
                        menu.Tag ??= "cancelled";
                        menu.Close();
                        menu.Dispose();
                    }
                }, CancellationToken.None).FireAndLog("Close the destination picker", Log);
            }

            var registration = cancellationToken.Register(() =>
            {
                if (picked.TrySetCanceled(cancellationToken))
                {
                    CloseLater();
                }
            });
            _ = picked.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);

            menu.Opening += (_, _) =>
            {
                // find the DPI settings for the screen where this is going to land
                var screenDpi = NativeDpiMethods.GetDpi(menu.Location);
                var scaledIconSize = DpiCalculator.ScaleWithDpi(coreConfig.IconSize, screenDpi);
                menu.SuspendLayout();
                var fontSize = DpiCalculator.ScaleWithDpi(12f, screenDpi);
                var previousFont = menu.Font;
                menu.Font = new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
                previousFont?.Dispose();
                menu.ImageScalingSize = scaledIconSize;
                menu.ResumeLayout();
            };

            menu.Closing += (_, eventArgs) =>
            {
                Log.DebugFormat("Close reason: {0}", eventArgs.CloseReason);
                switch (eventArgs.CloseReason)
                {
                    case ToolStripDropDownCloseReason.AppFocusChange:
                        // Do not allow the close if nothing was picked yet, this means the user clicked somewhere else.
                        if (menu.Tag == null)
                        {
                            eventArgs.Cancel = true;
                        }

                        break;
                    case ToolStripDropDownCloseReason.ItemClicked:
                    case ToolStripDropDownCloseReason.CloseCalled:
                        break;
                    case ToolStripDropDownCloseReason.Keyboard:
                        // Menu closed via keyboard (e.g., ESC key): declined
                        Complete(null);
                        break;
                    default:
                        eventArgs.Cancel = true;
                        break;
                }
            };
            menu.MouseEnter += (_, _) =>
            {
                // in case the menu has been unfocused, focus again so that dropdown menus will still open on mouseenter
                if (!menu.ContainsFocus)
                {
                    menu.Focus();
                }
            };

            foreach (var destination in destinations.OrderBy(d => d, DestinationComparer.Instance))
            {
                var item = CreateMenuItem(destination, captureDetails, Complete);
                item.Visible = destination.IsAvailableFor(captureDetails);
                menu.Items.Add(item);
            }

            // Close
            menu.Items.Add(new ToolStripSeparator());
            var closeItem = new ToolStripMenuItem(Language.GetString("editor_close"));
            AssignIcon(closeItem, DestinationIcons.Resource("Close.Image"));
            closeItem.Click += (_, _) => Complete(null);
            menu.Items.Add(closeItem);

            ShowMenuAtCursor(menu);
            return picked.Task;
        }

        /// <summary>
        /// This method will show the supplied context menu at the mouse cursor, also makes sure it has focus and it's not visible in the taskbar.
        /// </summary>
        private static void ShowMenuAtCursor(ContextMenuStrip menu)
        {
            // find a suitable location
            NativePoint location = Cursor.Position;
            var menuRectangle = new NativeRect(location, menu.Size);

            menuRectangle = menuRectangle.Intersect(DisplayInfo.ScreenBounds);
            if (menuRectangle.Height < menu.Height)
            {
                location = location.Offset(-40, -(menuRectangle.Height - menu.Height));
            }
            else
            {
                location = location.Offset(-40, -10);
            }

            // This prevents the problem that the context menu shows in the task-bar
            var notifyIcon = SimpleServiceProvider.Current.GetInstance<NotifyIcon>(isOptional: true);
            if (notifyIcon?.ContextMenuStrip != null)
            {
                User32Api.SetForegroundWindow(notifyIcon.ContextMenuStrip.Handle);
            }

            menu.Show(location);
            menu.Focus();
        }
    }
}
