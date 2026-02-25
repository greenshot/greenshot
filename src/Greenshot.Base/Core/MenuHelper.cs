/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;
using System.Windows.Forms;
using log4net;

namespace Greenshot.Base.Core;

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

    /// <summary>
    /// Assigns a clone of the specified image to the (menu) toolstrip item's icon and ensures the image is automatically disposed when the menu item is disposed.
    /// </summary>
    /// <remarks>This method clones the provided image before assigning it to the menu item. The cloned image
    /// is disposed automatically when the menu item is disposed, which helps prevent memory leaks. The caller does not
    /// need to manually manage the lifetime of the assigned image.</remarks>
    /// <param name="toolstripItem">The (menu) toolstrip item to which the image will be assigned. Cannot be null.</param>
    /// <param name="icon">The image to assign to the menu item. If null, the menu item's image is set to null.</param>
    public static void AssignAutoDisposingImage(this ToolStripItem toolstripItem, Image icon, bool needsClone = true)
    {
        if (toolstripItem == null)
        {
            return;
        }
        if (icon == null)
        {
            toolstripItem.Image = null;
            return;
        }
        var iconClone = needsClone ? ImageHelper.Clone(icon) : icon;
        if (iconClone == null)
        {
            toolstripItem.Image = null;
            return;
        }

        toolstripItem.Image = iconClone;
        // Dispose the cloned icon when the menu item is disposed to prevent memory leaks
        toolstripItem.Disposed += (sender, e) => iconClone?.Dispose();
    }

}