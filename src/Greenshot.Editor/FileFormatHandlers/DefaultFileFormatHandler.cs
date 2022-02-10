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

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This is the default .NET bitmap file format handler
    /// </summary>
    public class DefaultFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private readonly List<string> _ourExtensions = new() { ".png", ".bmp", ".gif", ".jpg", ".jpeg", ".tiff", ".tif" };

        public DefaultFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            ImageFormat imageFormat = extension switch
            {
                ".png" => ImageFormat.Png,
                ".bmp" => ImageFormat.Bmp,
                ".gif" => ImageFormat.Gif,
                ".jpg" => ImageFormat.Jpeg,
                ".jpeg" => ImageFormat.Jpeg,
                ".tiff" => ImageFormat.Tiff,
                ".tif" => ImageFormat.Tiff,
                _ => null
            };

            if (imageFormat == null)
            {
                return false;
            }
            bitmap.Save(destination, imageFormat);
            return true;
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            using var tmpImage = Image.FromStream(stream, true, true);
            bitmap = ImageHelper.Clone(tmpImage, PixelFormat.Format32bppArgb);
            return true;
        }
    }
}
