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
using System.Runtime.Serialization;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// This is the base container for vector graphics, these ae graphics which can resize without loss of quality.
    /// Examples for this are SVG, WMF or EMF, but also graphics based on fonts (e.g. Emoji)
    /// </summary>
    [Serializable]
    public abstract class VectorGraphicsContainer : DrawableContainer
    {
        protected int RotationAngle;

        /// <summary>
        /// This is the cached version of the bitmap, pre-rendered to save performance
        /// Do not serialized, it can be rebuild with some other information.
        /// </summary>
        [NonSerialized] private Image _cachedImage;

        public VectorGraphicsContainer(ISurface parent) : base(parent)
        {
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            CreateDefaultAdorners();
        }

        /// <summary>
        /// The bulk of the clean-up code is implemented in Dispose(bool)
        /// This Dispose is called from the Dispose and the Destructor.
        /// When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResetCachedBitmap();
            }

            base.Dispose(disposing);
        }

        public override void Transform(Matrix matrix)
        {
            RotationAngle += CalculateAngle(matrix);
            RotationAngle %= 360;

            ResetCachedBitmap();

            base.Transform(matrix);
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (_cachedImage != null && _cachedImage.Size != Bounds.Size)
            {
                ResetCachedBitmap();
            }

            _cachedImage ??= ComputeBitmap(); 

            graphics.DrawImage(_cachedImage, Bounds);
        }

        protected virtual Image ComputeBitmap()
        {
            var image = ImageHelper.CreateEmpty(Width, Height, PixelFormat.Format32bppRgb, Color.Transparent);

            if (RotationAngle == 0) return image;

            var newImage = image.Rotate(RotationAngle);
            image.Dispose();
            return newImage;

        }

        private void ResetCachedBitmap()
        {
            _cachedImage?.Dispose();
            _cachedImage = null;
        }
    }
}