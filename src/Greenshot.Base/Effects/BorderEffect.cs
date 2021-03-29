/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Drawing2D;
using Greenshot.Base.Core;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// BorderEffect
    /// </summary>
    public class BorderEffect : IEffect
    {
        public BorderEffect()
        {
            Reset();
        }

        public Color Color { get; set; }
        public int Width { get; set; }

        public void Reset()
        {
            Width = 2;
            Color = Color.Black;
        }

        public Image Apply(Image sourceImage, Matrix matrix)
        {
            return ImageHelper.CreateBorder(sourceImage, Width, Color, sourceImage.PixelFormat, matrix);
        }
    }
}