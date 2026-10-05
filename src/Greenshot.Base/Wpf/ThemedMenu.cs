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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Dapplo.Ini;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using log4net;
using Image = System.Windows.Controls.Image;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Builds the themed WPF menus (the tray menu and the destination picker): light or dark like the other Greenshot windows, and sized by WPF
    /// for the DPI of the monitor they open on. The menus are built right before they open, so they always use the current theme and texts.
    /// Must be used on the UI thread.
    /// </summary>
    public static class ThemedMenu
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ThemedMenu));
        private static ResourceDictionary _styles;

        private static ResourceDictionary Styles => _styles ??= new ResourceDictionary
        {
            Source = new Uri("/Greenshot.Base;component/Wpf/Styles/MenuStyles.xaml", UriKind.Relative)
        };

        /// <summary>
        /// The size of the menu icons in device independent pixels (the icon size setting), WPF scales it for the monitor
        /// </summary>
        public static double IconSize
        {
            get
            {
                var coreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
                int size = coreConfig?.IconSize.Width ?? 16;
                return size > 0 ? size : 16;
            }
        }

        /// <summary>
        /// A new, empty, themed context menu
        /// </summary>
        public static ContextMenu CreateContextMenu()
        {
            return new ContextMenu
            {
                Style = (Style)Styles["GreenshotContextMenuStyle"]
            };
        }

        /// <summary>
        /// A themed menu item
        /// </summary>
        /// <param name="text">The text, shown as is (an underscore is no access key)</param>
        /// <param name="icon">The icon or null</param>
        /// <param name="onClick">Called when the item itself (not a sub item) is clicked, can be null</param>
        public static MenuItem CreateItem(string text, ImageSource icon = null, Action onClick = null)
        {
            var item = new MenuItem
            {
                Header = text ?? string.Empty,
                Style = (Style)Styles["GreenshotMenuItemStyle"]
            };
            if (icon != null)
            {
                item.Icon = CreateIcon(icon);
            }

            if (onClick != null)
            {
                item.Click += (sender, args) =>
                {
                    // Click bubbles up from the sub items
                    if (!ReferenceEquals(args.OriginalSource, item))
                    {
                        return;
                    }

                    try
                    {
                        onClick();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error in the menu action of '{text}'", ex);
                    }
                };
            }

            return item;
        }

        /// <summary>
        /// A themed menu item which can be checked: it shows a check mark (or a highlighted icon) when checked
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="isChecked">The current state</param>
        /// <param name="onChanged">Called with the new state when the user clicks the item</param>
        public static MenuItem CreateCheckItem(string text, bool isChecked, Action<bool> onChanged)
        {
            var item = CreateItem(text);
            item.IsCheckable = true;
            item.IsChecked = isChecked;
            item.Click += (sender, args) =>
            {
                if (!ReferenceEquals(args.OriginalSource, item))
                {
                    return;
                }

                try
                {
                    onChanged?.Invoke(item.IsChecked);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error changing '{text}'", ex);
                }
            };
            return item;
        }

        /// <summary>
        /// A themed separator
        /// </summary>
        public static Separator CreateSeparator()
        {
            return new Separator
            {
                Style = (Style)Styles["GreenshotMenuSeparatorStyle"]
            };
        }

        /// <summary>
        /// The element which shows the icon in a menu item, the image is scaled to the icon size setting
        /// </summary>
        public static FrameworkElement CreateIcon(ImageSource source)
        {
            var image = new Image
            {
                Source = source,
                Width = IconSize,
                Height = IconSize,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }

        /// <summary>
        /// The icon from a GDI image (e.g. a plugin image or a window icon), null when there is none.
        /// The image stays with the caller.
        /// </summary>
        public static ImageSource ToImageSource(System.Drawing.Image image)
        {
            if (image == null)
            {
                return null;
            }

            try
            {
                var source = image.ToBitmapSource();
                source?.Freeze();
                return source;
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't convert a menu image", ex);
                return null;
            }
        }

        /// <summary>
        /// Show the icon of the key (see <see cref="DestinationIcons"/>) on the item as soon as it is loaded
        /// </summary>
        public static void AssignIcon(MenuItem item, string iconKey)
        {
            if (item == null || string.IsNullOrEmpty(iconKey))
            {
                return;
            }

            AssignIconAsync(item, iconKey).FireAndLog($"Load icon {iconKey}", Log);
        }

        private static async System.Threading.Tasks.Task AssignIconAsync(MenuItem item, string iconKey)
        {
            // Back on the UI thread after the await, the image source is frozen and can be used here
            var source = await DestinationIcons.GetImageSourceAsync(iconKey).ConfigureAwait(true);
            if (source != null)
            {
                item.Icon = CreateIcon(source);
            }
        }

        /// <summary>
        /// Open the menu at the mouse cursor, WPF keeps it inside the work area of that monitor and scales it for its DPI.
        /// The menu gets the focus, so the keyboard works and it closes when the user clicks somewhere else (unless StaysOpen is set).
        /// </summary>
        public static void ShowAtCursor(ContextMenu menu)
        {
            if (menu == null)
            {
                return;
            }

            menu.PlacementTarget = null;
            menu.Placement = PlacementMode.MousePoint;
            menu.IsOpen = true;

            // A menu of a process without a foreground window wouldn't get the keyboard and wouldn't close on a click elsewhere
            if (PresentationSource.FromVisual(menu) is HwndSource source)
            {
                User32Api.SetForegroundWindow(source.Handle);
            }

            menu.Focus();
        }
    }
}
