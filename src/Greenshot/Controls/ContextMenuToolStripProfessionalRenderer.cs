/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;

namespace Greenshot.Controls
{
    /// <summary>
    /// ToolStripProfessionalRenderer which draws the Check correctly when the icons are larger
    /// </summary>
    public class ContextMenuToolStripProfessionalRenderer : ToolStripProfessionalRenderer
    {
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private static Image _scaledCheckbox;

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            if (_scaledCheckbox == null || _scaledCheckbox.Size != CoreConfig.ScaledIconSize)
            {
                _scaledCheckbox?.Dispose();
                _scaledCheckbox = ImageHelper.ResizeImage(e.Image, true, CoreConfig.ScaledIconSize.Width, CoreConfig.ScaledIconSize.Height, null);
            }

            Rectangle old = e.ImageRectangle;
            ToolStripItemImageRenderEventArgs clone = new ToolStripItemImageRenderEventArgs(e.Graphics, e.Item, _scaledCheckbox, new Rectangle(old.X, 0, old.Width, old.Height));
            base.OnRenderItemCheck(clone);
        }
    }
}