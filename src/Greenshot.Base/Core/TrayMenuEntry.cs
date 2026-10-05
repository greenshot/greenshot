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
using System.Drawing;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// An entry a plugin adds to the Greenshot tray menu with <see cref="PluginUtils.AddToContextMenu"/>, independent of the UI toolkit of the menu.
    /// The tray menu is built every time it opens, so changes of the text, image or visibility show the next time it opens.
    /// Disposing the entry removes it from the tray menu.
    /// </summary>
    public sealed class TrayMenuEntry : IDisposable
    {
        public TrayMenuEntry()
        {
        }

        public TrayMenuEntry(string text)
        {
            Text = text;
        }

        /// <summary>
        /// The text of the entry
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The image of the entry, null for none. The plugin owns it.
        /// </summary>
        public Image Image { get; set; }

        /// <summary>
        /// Only visible entries are in the tray menu
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Raised on the UI thread when the entry is clicked
        /// </summary>
        public event EventHandler Click;

        /// <summary>
        /// Called by the tray menu when the entry is clicked
        /// </summary>
        public void PerformClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Remove the entry from the tray menu
        /// </summary>
        public void Dispose()
        {
            PluginUtils.RemoveFromContextMenu(this);
        }
    }
}
