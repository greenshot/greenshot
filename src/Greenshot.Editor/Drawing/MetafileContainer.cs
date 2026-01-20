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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// This provides a resizable SVG container, redrawing the SVG in the size the container takes.
    /// </summary>
    public class MetafileContainer : VectorGraphicsContainer
    {
        private Metafile _metafile;
        public Metafile Metafile
        {
            get => _metafile;
        }

        /// <summary>
        /// Original file content. Is used for serialization.
        /// More Information: GDI+ does not support saving .wmf or .emf files, because there is no encoder.
        /// So we need to save the original file content for deserialization.
        /// </summary>
        public MemoryStream MetafileContent = new MemoryStream();

        public MetafileContainer(Stream stream, ISurface parent) : base(parent)
        {

            stream.CopyTo(MetafileContent);
            stream.Seek(0, SeekOrigin.Begin);
            var image = Image.FromStream(stream, true, true);
            if (image is Metafile metaFile)
            {
                _metafile = metaFile;
                Size = new NativeSize(_metafile.Width / 4, _metafile.Height / 4);
            } else  if (image is Bitmap imageFile)
            {
                // Fallback to support old files version 1.03
                // if the stream is not a Metafile, we create a Metafile from the Bitmap.
                _metafile = CreateMetafileFromImage(imageFile);
                Size = new NativeSize(imageFile.Width, imageFile.Height);
            }
            else
            {
                throw new ArgumentException("Stream is not a valid Metafile");
            }
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

        /// <summary>
        /// Creates a new <see cref="Metafile"/> from the specified <see cref="Image"/>.
        /// </summary>
        /// <param name="image">The source <see cref="Image"/> to be converted into a <see cref="Metafile"/>. Cannot be <see
        /// langword="null"/>.</param>
        /// <returns>A <see cref="Metafile"/> object that contains the graphical content of the specified <see cref="Image"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="image"/> is <see langword="null"/>.</exception>
        private static Metafile CreateMetafileFromImage(Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            using (Bitmap tempBitmap = new Bitmap(1, 1))
            using (Graphics referenceGraphics = Graphics.FromImage(tempBitmap))
            {
                IntPtr hdc = referenceGraphics.GetHdc();
                try
                {
                    // Create a new Metafile with the size of the image
                    Metafile metafile = new Metafile(hdc, new Rectangle(0, 0, image.Width, image.Height), MetafileFrameUnit.Pixel, EmfType.EmfOnly);
                    using (Graphics gMetafile = Graphics.FromImage(metafile))
                    {
                        gMetafile.DrawImage(image, 0, 0, image.Width, image.Height);
                    }
                    return metafile;
                }
                finally
                {
                    referenceGraphics.ReleaseHdc(hdc);
                }
            }
        }
        public override bool HasDefaultSize => true;

        public override NativeSize DefaultSize => new NativeSize(_metafile.Width, _metafile.Height);
    }
}