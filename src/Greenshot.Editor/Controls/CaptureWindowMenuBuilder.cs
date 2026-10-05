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
using Dapplo.Windows.DesktopWindowsManager;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// Fills the "Insert window" menu of the editor with the top level windows, with a live thumbnail of the window under the mouse
    /// </summary>
    public sealed class CaptureWindowMenuBuilder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CaptureWindowMenuBuilder));
        private ThumbnailForm _thumbnailForm;

        /// <summary>
        /// Replace the drop down items of the menu item with one item per window (the item's Tag is the WindowDetails)
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
            bool thumbnailPreview = coreConfig.ThumnailPreview && DwmApi.IsDwmEnabled;

            foreach (var window in WindowDetails.GetTopLevelWindows())
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(window.ToString());
                }

                string title = window.Text;
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
                captureWindowItem.AssignAutoDisposingImage(window?.DisplayIcon, needsClone: false);
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
            if (sender is not ToolStripMenuItem captureWindowItem || captureWindowItem.Tag is not WindowDetails window)
            {
                return;
            }

            _thumbnailForm ??= new ThumbnailForm();
            _thumbnailForm.ShowThumbnail(window, captureWindowItem.GetCurrentParent().TopLevelControl);
        }

        private void HideThumbnailOnLeave(object sender, EventArgs e)
        {
            _thumbnailForm?.Hide();
        }

        private void OnDropDownClosed(object sender, EventArgs e)
        {
            if (_thumbnailForm == null)
            {
                return;
            }

            _thumbnailForm.Close();
            _thumbnailForm = null;
        }
    }
}
