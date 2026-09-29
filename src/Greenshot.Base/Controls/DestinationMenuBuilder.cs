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
        /// Create the menu item for the destination, dynamic destinations are added as sub items when it opens.
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
                // A placeholder makes the item open a drop down, it is replaced when the dynamic destinations arrive
                var placeholder = new ToolStripMenuItem("…") { Enabled = false };
                menuItem.DropDownItems.Add(placeholder);
                bool loading = false;
                menuItem.DropDownOpening += (_, _) =>
                {
                    if (loading)
                    {
                        return;
                    }

                    loading = true;
                    AddDynamicDestinationsAsync(menuItem, destination, captureDetails, onClick).FireAndLog($"Dynamic destinations of {destination.Designation}", Log);
                };
            }

            return menuItem;
        }

        private static async Task AddDynamicDestinationsAsync(ToolStripMenuItem menuItem, IDestination destination, ICaptureDetails captureDetails, Action<IDestination> onClick)
        {
            IReadOnlyList<IDestination> subDestinations;
            try
            {
                // The destination may ask a COM server (Office) for its documents, this doesn't block the UI
                subDestinations = await destination.GetDynamicDestinationsAsync(captureDetails, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                // Fixing Bug #3536968: skip the dynamic destinations when there is an error
                Log.ErrorFormat("Skipping the dynamic destinations of {0}, due to the following error: {1}", destination.Designation, ex.Message);
                subDestinations = Array.Empty<IDestination>();
            }

            if (menuItem.IsDisposed)
            {
                return;
            }

            menuItem.DropDownItems.Clear();
            var validSubDestinations = subDestinations.Where(d => d != null).OrderBy(d => d, DestinationComparer.Instance).ToList();
            if (validSubDestinations.Count == 0)
            {
                return;
            }

            if (destination.Descriptor.UseDynamicsOnly && validSubDestinations.Count == 1)
            {
                // Only one: the item itself becomes that destination
                menuItem.Tag = validSubDestinations[0];
                menuItem.Text = validSubDestinations[0].Descriptor.DisplayName;
                return;
            }

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
                // We might be in the closing process, dispose later to avoid re-entrancy
                menu.BeginInvoke(new Action(() =>
                {
                    if (!menu.IsDisposed)
                    {
                        menu.Close();
                        menu.Dispose();
                    }
                }));
            }

            var registration = cancellationToken.Register(() => picked.TrySetCanceled(cancellationToken));
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
