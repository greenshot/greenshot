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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// THis is the default .NET bitmap file format handler
    /// </summary>
    public class IconFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageHelper));

        private static readonly string[] OurExtensions = { ".ico" };

        /// <inheritdoc />
        public IEnumerable<string> SupportedExtensions(FileFormatHandlerActions fileFormatHandlerAction)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.SaveToStream)
            {
                return Enumerable.Empty<string>();
            }

            return OurExtensions;
        }

        /// <inheritdoc />
        public bool Supports(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.SaveToStream)
            {
                return false;
            }

            return OurExtensions.Contains(NormalizeExtension(extension));
        }

        /// <inheritdoc />
        public int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return int.MaxValue;
        }

        public bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            // TODO: Implement this
            return false;
        }

        public bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            _ = stream.Seek(0, SeekOrigin.Current);

            // Icon logic, try to get the Vista icon, else the biggest possible
            try
            {
                using Image tmpImage = ExtractVistaIcon(stream);
                if (tmpImage != null)
                {
                    bitmap = ImageHelper.Clone(tmpImage, PixelFormat.Format32bppArgb);
                    return true;
                }
            }
            catch (Exception vistaIconException)
            {
                Log.Warn("Can't read icon", vistaIconException);
            }

            try
            {
                // No vista icon, try normal icon
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


        public bool TryLoadDrawableFromStream(Stream stream, string extension, out IDrawableContainer drawableContainer, ISurface surface = null)
        {
            if (TryLoadFromStream(stream, extension, out var bitmap))
            {
                var imageContainer = new ImageContainer(surface)
                {
                    Image = bitmap
                };
                drawableContainer = imageContainer;
                return true;
            }

            drawableContainer = null;
            return true;
        }

        /// <summary>
        /// Based on: https://www.codeproject.com/KB/cs/IconExtractor.aspx
        /// And a hint from: https://www.codeproject.com/KB/cs/IconLib.aspx
        /// </summary>
        /// <param name="iconStream">Stream with the icon information</param>
        /// <returns>Bitmap with the Vista Icon (256x256)</returns>
        private static Bitmap ExtractVistaIcon(Stream iconStream)
        {
            const int sizeIconDir = 6;
            const int sizeIconDirEntry = 16;
            Bitmap bmpPngExtracted = null;
            try
            {
                byte[] srcBuf = new byte[iconStream.Length];
                // TODO: Check if there is a need to process the result
                _ = iconStream.Read(srcBuf, 0, (int)iconStream.Length);
                int iCount = BitConverter.ToInt16(srcBuf, 4);
                for (int iIndex = 0; iIndex < iCount; iIndex++)
                {
                    int iWidth = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex];
                    int iHeight = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex + 1];
                    if (iWidth != 0 || iHeight != 0) continue;

                    int iImageSize = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 8);
                    int iImageOffset = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 12);
                    using MemoryStream destStream = new MemoryStream();
                    destStream.Write(srcBuf, iImageOffset, iImageSize);
                    destStream.Seek(0, SeekOrigin.Begin);
                    bmpPngExtracted = new Bitmap(destStream); // This is PNG! :)
                    break;
                }
            }
            catch
            {
                return null;
            }

            return bmpPngExtracted;
        }
    }
}
