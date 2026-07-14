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
    /// This provides a resizable Metafile container, redrawing the Metafile in the size the container takes.
    /// </summary>
    public class MetafileContainer : VectorGraphicsContainer
    {
        private Metafile _metafile;
        public Metafile Metafile
        {
            get => _metafile;
        }

        /// <summary>
        /// Original file content is used for serialization.
        /// More Information: GDI+ does not support saving .wmf or .emf files, because there is no encoder.
        /// So we need to save the original file content for deserialization.
        /// </summary>
        public MemoryStream MetafileContent = new MemoryStream();

        /// <summary>
        /// We need to store the extension of the original file content to determine the file extension for the image file in the zip archive during serialization.
        /// </summary>
        public string MetafileContentExtension;

        public MetafileContainer(Stream stream, ISurface parent) : base(parent)
        {
            stream.CopyTo(MetafileContent);
            stream.Seek(0, SeekOrigin.Begin);
            MetafileContentExtension = DetermineMetafileExtension(stream);
            var image = Image.FromStream(stream, true, true);
            if (image is Metafile metaFile)
            {
                _metafile = metaFile;
                Size = new NativeSize(_metafile.Width / 4, _metafile.Height / 4);
            }
            else if (image is Bitmap imageFile)
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

        /// <summary>
        /// Determines the appropriate file extension for a metafile based on its header magic number.
        /// </summary>
        /// <remarks>The method checks the first four bytes of the stream to identify the metafile format.
        /// It recognizes EMF and WMF formats based on their respective magic numbers. If the format cannot be
        /// determined, it defaults to the EMF extension.</remarks>
        /// <param name="stream">The stream containing the metafile data. This stream must be seekable and contain at least four bytes of
        /// data to determine the metafile type.</param>
        private string DetermineMetafileExtension(Stream stream)
        {
            var assumedExtension = String.Empty;
            long originalPosition = stream.Position;
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                byte[] header = new byte[4];
                if (stream.Read(header, 0, 4) == 4)
                {
                    uint magic = BitConverter.ToUInt32(header, 0);

                    // Check for EMF magic number (0x464D4520 = "EMF ")
                    if (magic == 0x464D4520)
                    {
                        assumedExtension = ".emf";
                    }
                    // Check for WMF magic numbers (0xD7CDC69A or 0x01000900)
                    else if (magic == 0xD7CDC69A || magic == 0x01000900)
                    {
                        assumedExtension = ".wmf";
                    }
                    else
                    {
                        // Default to emf if unable to determine
                        assumedExtension = ".emf";
                    }
                }
            }
            finally
            {
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }
            return assumedExtension;
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