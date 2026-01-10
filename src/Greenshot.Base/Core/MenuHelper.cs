/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Windows.Forms;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Helper class for menu related functionality
    /// </summary>
    public static class MenuHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MenuHelper));

        /// <summary>
        /// Sets up automatic disposal for ContextMenuStrip when closed.
        /// This prevents memory leaks by ensuring the menu is properly disposed
        /// when it's no longer needed. Uses delayed disposal to avoid ObjectDisposedException.
        /// </summary>
        /// <param name="menu">The ContextMenuStrip to setup auto-disposal for</param>
        public static void SetupAutoDispose(this ContextMenuStrip menu)
        {
            menu.Closed += (sender, e) =>
            {
                Log.Debug("ContextMenuStrip closed, scheduling delayed disposal");

                // delayed disposal -- waiting for ClickHandler beeing finished
                menu.BeginInvoke(new System.Action(() =>
                {
                    if (!menu.IsDisposed)
                    {
                        Log.Debug("Disposing ContextMenuStrip");
                        menu.Dispose();
                    }
                }));
            };
        }
    }
}