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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// This provides a resizable SVG container, redrawing the SVG in the size the container takes.
    /// </summary>
    [Serializable]
    public class MetafileContainer : VectorGraphicsContainer
    {
        private readonly Metafile _metafile;

        public MetafileContainer(Metafile metafile, ISurface parent) : base(parent)
        {
            _metafile = metafile;
            Size = new NativeSize(metafile.Width/4, metafile.Height/4);
        }
        
        protected override Image ComputeBitmap()
        {
            var image = ImageHelper.CreateEmpty(Width, Height, PixelFormat.Format32bppArgb, Color.Transparent);
            
            var dstRect = new NativeRect(0, 0, Width, Height);
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(_metafile, dstRect);
            }
            
            if (RotationAngle == 0) return image;

            var newImage = image.Rotate(RotationAngle);
            image.Dispose();
            return newImage;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _metafile?.Dispose();
            }
            
            base.Dispose(disposing);
        }

        public override bool HasDefaultSize => true;

        public override NativeSize DefaultSize => new NativeSize(_metafile.Width, _metafile.Height);
    }
}