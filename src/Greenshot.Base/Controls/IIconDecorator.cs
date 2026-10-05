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

using System.Drawing;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// A tool strip item which draws something on its icon (e.g. the selected color), see <see cref="IconBinder"/>.
    /// The binder calls it for every new copy of the icon, before the item shows it.
    /// </summary>
    public interface IIconDecorator
    {
        /// <summary>
        /// Draw on the icon, the image has the final size and belongs to the item
        /// </summary>
        /// <param name="icon">Image</param>
        void DecorateIcon(Image icon);
    }
}
