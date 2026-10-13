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
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// Fills the "Insert window" menu of the editor with the top level windows, with a live thumbnail of the window under the mouse
    /// </summary>
    public sealed class CaptureWindowMenuBuilder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureWindowMenuBuilder));
        private ThumbnailWindow _thumbnailWindow;

        /// <summary>
        /// Replace the drop down items of the menu item with one item per window (the item's Tag is the IInteropWindow)
        /// </summary>
        /// <param name="menuItem">ToolStripMenuItem</param>
        /// <param name="onClick">Click handler of the window items</param>
        public void Fill(ToolStripMenuItem menuItem, EventHandler onClick)
        {
            var coreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
            menuItem.DropDownClosed -= OnDropDownClosed;
            menuItem.DropDownClosed += OnDropDownClosed;
            menuItem.DropDownItems.Clear();
            // check if thumbnailPreview is enabled and DWM is enabled
            bool thumbnailPreview = coreConfig.ThumnailPreview;

            foreach (var window in WindowHelper.GetTopLevelWindows())
            {
                string title = window.GetCaption();
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Window {window.Handle} '{title}' ({window.GetClassname()})");
                }
                if (string.IsNullOrEmpty(title))
                {
                    continue;
                }

                if (title.Length > coreConfig.MaxMenuItemLength)
                {
                    title = title.Substring(0, Math.Min(title.Length, coreConfig.MaxMenuItemLength));
                }

                ToolStripItem captureWindowItem = menuItem.DropDownItems.Add(title);
                captureWindowItem.Tag = window;
                captureWindowItem.Click += onClick;
                // Dispose the icon when the menu item is disposed to prevent memory leaks
                captureWindowItem.AssignAutoDisposingImage(window.GetDisplayIcon(), needsClone: false);
                // Only show preview when enabled
                if (thumbnailPreview)
                {
                    captureWindowItem.MouseEnter += ShowThumbnailOnEnter;
                    captureWindowItem.MouseLeave += HideThumbnailOnLeave;
                }
            }
        }

        private void ShowThumbnailOnEnter(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem captureWindowItem || captureWindowItem.Tag is not IInteropWindow window)
            {
                return;
            }

            // The drop down with the windows: its screen bounds in pixels and its handle, the thumbnail goes above (or under) it
            var dropDown = captureWindowItem.GetCurrentParent()?.TopLevelControl;
            NativeRect? alignTo = dropDown == null ? null : new NativeRect(dropDown.Left, dropDown.Top, dropDown.Width, dropDown.Height);

            _thumbnailWindow ??= new ThumbnailWindow();
            _thumbnailWindow.ShowThumbnail(window, alignTo, dropDown?.Handle ?? IntPtr.Zero);
        }

        private void HideThumbnailOnLeave(object sender, EventArgs e)
        {
            _thumbnailWindow?.Hide();
        }

        private void OnDropDownClosed(object sender, EventArgs e)
        {
            if (_thumbnailWindow == null)
            {
                return;
            }

            _thumbnailWindow.Close();
            _thumbnailWindow = null;
        }
    }
}
