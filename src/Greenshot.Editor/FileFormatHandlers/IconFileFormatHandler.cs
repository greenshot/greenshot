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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// THis is the .ico format handler
    /// </summary>
    public class IconFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IconFileFormatHandler));

        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".ico" };

        public IconFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToFile] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromFile] = _ourExtensions;
        }

        public override bool TrySaveToStream(Bitmap bitmap, Stream stream, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            IList<Image> images = new List<Image>
            {
                bitmap
            };

            var binaryWriter = new BinaryWriter(stream);
            //
            // ICONDIR structure
            //
            binaryWriter.Write((short)0); // reserved
            binaryWriter.Write((short)1); // image type (icon)
            binaryWriter.Write((short)images.Count); // number of images

            IList<Size> imageSizes = new List<Size>();
            IList<MemoryStream> encodedImages = new List<MemoryStream>();
            foreach (var image in images)
            {
                // Pick the best fit
                var sizes = new[]
                {
                    16, 32, 48
                };
                int size = 256;
                foreach (var possibleSize in sizes)
                {
                    if (image.Width <= possibleSize && image.Height <= possibleSize)
                    {
                        size = possibleSize;
                        break;
                    }
                }

                var imageStream = new MemoryStream();
                if (image.Width == size && image.Height == size)
                {
                    using var clonedImage = ImageHelper.Clone(image, PixelFormat.Format32bppArgb);
                    clonedImage.Save(imageStream, ImageFormat.Png);
                    imageSizes.Add(new Size(size, size));
                }
                else
                {
                    // Resize to the specified size, first make sure the image is 32bpp
                    using var clonedImage = ImageHelper.Clone(image, PixelFormat.Format32bppArgb);
                    using var resizedImage = ImageHelper.ResizeImage(clonedImage, true, true, Color.Empty, size, size, null);
                    resizedImage.Save(imageStream, ImageFormat.Png);
                    imageSizes.Add(resizedImage.Size);
                }

                imageStream.Seek(0, SeekOrigin.Begin);
                encodedImages.Add(imageStream);
            }

            //
            // ICONDIRENTRY structure
            //
            const int iconDirSize = 6;
            const int iconDirEntrySize = 16;

            var offset = iconDirSize + (images.Count * iconDirEntrySize);
            for (int i = 0; i < images.Count; i++)
            {
                var imageSize = imageSizes[i];
                // Write the width / height, 0 means 256
                binaryWriter.Write(imageSize.Width == 256 ? (byte)0 : (byte)imageSize.Width);
                binaryWriter.Write(imageSize.Height == 256 ? (byte)0 : (byte)imageSize.Height);
                binaryWriter.Write((byte)0); // no palette
                binaryWriter.Write((byte)0); // reserved
                binaryWriter.Write((short)0); // no color planes
                binaryWriter.Write((short)32); // 32 bpp
                binaryWriter.Write((int)encodedImages[i].Length); // image data length
                binaryWriter.Write(offset);
                offset += (int)encodedImages[i].Length;
            }

            binaryWriter.Flush();
            //
            // Write image data
            //
            foreach (var encodedImage in encodedImages)
            {
                encodedImage.WriteTo(stream);
                encodedImage.Dispose();
            }

            // TODO: Implement this
            return true;
        }

        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            try
            {
                // The biggest icon, ToBitmap also reads the PNG compressed (256x256) icons
                stream.Position = stream.Seek(0, SeekOrigin.Begin);
                // We create a copy of the bitmap, so everything else can be disposed
                using Icon tmpIcon = new Icon(stream, new Size(1024, 1024));
                using Image tmpImage = tmpIcon.ToBitmap();
                bitmap = ImageHelper.Clone(tmpImage, PixelFormat.Format32bppArgb);
                return true;
            }
            catch (Exception iconException)
            {
                Log.Warn("Can't read icon", iconException);
            }

            bitmap = null;
            return false;
        }
    }
}
