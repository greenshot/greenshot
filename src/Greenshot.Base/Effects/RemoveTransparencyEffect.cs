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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Greenshot.Base.Core;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// RemoveTransparencyEffect - Removes transparency from images and replaces it with a solid color
    /// </summary>
    public class RemoveTransparencyEffect : IEffect
    {
        public RemoveTransparencyEffect()
        {
            Reset();
        }

        public Color Color { get; set; }

        public void Reset()
        {
            Color = Color.White;
        }

        public Image Apply(Image sourceImage, Matrix matrix)
        {
            // Only process if the image has an alpha channel
            if (!Image.IsAlphaPixelFormat(sourceImage.PixelFormat))
            {
                return null;
            }

            // Create a new bitmap without alpha channel
            Bitmap newBitmap = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format24bppRgb);
            
            using (Graphics graphics = Graphics.FromImage(newBitmap))
            {
                graphics.Clear(Color);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);
            }

            return newBitmap;
        }
    }
}
