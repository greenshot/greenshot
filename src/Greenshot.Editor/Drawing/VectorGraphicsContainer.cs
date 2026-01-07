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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using log4net;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// This is the base container for vector graphics, these ae graphics which can resize without loss of quality.
    /// Examples for this are SVG, WMF or EMF, but also graphics based on fonts (e.g. Emoji)
    /// </summary>
    public abstract class VectorGraphicsContainer : DrawableContainer
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(VectorGraphicsContainer));

        /// <inheritdoc cref="RotationAngle"/>
        private int _rotationAngle;

        /// /// <summary>
        /// This is the rotation angle of the vector graphics. It is used to rotate the graphics when rendering in <see cref="ComputeBitmap"/>.
        /// </summary>
        public int RotationAngle
        {
            get => _rotationAngle;
            set => _rotationAngle = value;
        }

        /// <summary>
        /// This is the cached version of the bitmap, pre-rendered to save performance
        /// Do not serialized, it can be rebuild with other information.
        /// </summary>
        private Image _cachedImage;

        /// <summary>
        /// Constructor takes care of creating adorners
        /// </summary>
        /// <param name="parent">ISurface</param>
        protected VectorGraphicsContainer(ISurface parent) : base(parent)
        {
            InitAdorners();
        }

        /// <summary>
        /// For vector graphics the <see cref="DrawableContainer.CreateDefaultAdorners"/> are not used. so we need to initialize the adorners here.
        /// </summary>
        private void InitAdorners()
        {
            // Check if the adorners are already defined!
            if (Adorners.Count > 0)
            {
                LOG.Warn("Adorners are already defined!");
                return;
            }

            Adorners.Add(new ResizeAdorner(this, Positions.TopLeft));
            Adorners.Add(new ResizeAdorner(this, Positions.TopRight));
            Adorners.Add(new ResizeAdorner(this, Positions.BottomLeft));
            Adorners.Add(new ResizeAdorner(this, Positions.BottomRight));
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

        /// <inheritdoc cref="IDrawableContainer"/>
        public override void Transform(Matrix matrix)
        {
            RotationAngle += CalculateAngle(matrix);
            RotationAngle %= 360;

            ResetCachedBitmap();

            base.Transform(matrix);
        }

        /// <inheritdoc cref="IDrawableContainer"/>
        public override void Draw(Graphics graphics, RenderMode rm)
        {
            if (_cachedImage != null && _cachedImage.Size != Bounds.Size)
            {
                ResetCachedBitmap();
            }

            _cachedImage ??= ComputeBitmap();
            if (_cachedImage == null)
            {
                return;
            }
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;


            graphics.DrawImage(_cachedImage, Bounds);
        }

        /// <summary>
        /// Implement this to compute the new bitmap according to the size of the container
        /// </summary>
        /// <returns>Image</returns>
        protected abstract Image ComputeBitmap();

        /// <summary>
        /// Dispose of the cached bitmap, forcing the code to regenerate it
        /// </summary>
        protected void ResetCachedBitmap()
        {
            _cachedImage?.Dispose();
            _cachedImage = null;
        }
    }
}