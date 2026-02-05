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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using Image = SixLabors.ImageSharp.Image;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This is the ImageSharp file format handler
    /// </summary>
    public class ImageSharpFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".png", ".bmp", ".gif", ".jpg", ".jpeg", ".tiff", ".tif", ".tga", ".pbm", ".webp"};
        public ImageSharpFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToFile] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromFile] = _ourExtensions;
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            var image = ImageSharpHelper.ConvertToImageSharp(bitmap);
            IImageEncoder encoder = extension switch
            {
                ".png" => new PngEncoder(),
                ".bmp" => new BmpEncoder(),
                ".gif" => new GifEncoder(),
                ".jpg" => new JpegEncoder() { Quality = surfaceOutputSettings.JPGQuality},
                ".jpeg" => new JpegEncoder() {Quality = surfaceOutputSettings.JPGQuality},
                ".tiff" => new TiffEncoder(),
                ".tif" => new TiffEncoder(),
                ".tga" => new TgaEncoder(),
                ".pbm" => new PbmEncoder(),
                ".webp" => new WebpEncoder(),
                _ => null
            };
            if (encoder == null)
            {
                return false;
            }
            image.Save(destination, encoder);
            return true;
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            IImageDecoder decoder = extension switch
            {
                ".png" => new PngDecoder(),
                ".bmp" => new BmpDecoder(),
                ".gif" => new GifDecoder(),
                ".jpg" => new JpegDecoder(),
                ".jpeg" => new JpegDecoder(),
                ".tiff" => new TiffDecoder(),
                ".tif" => new TiffDecoder(),
                ".tga" => new TgaDecoder(),
                ".pbm" => new PbmDecoder(),
                ".webp" => new WebpDecoder(),
                _ => null
            };
            if (decoder == null)
            {
                bitmap = null;
                return false;
            }
            using (var image = Image.Load(stream, decoder))
            {
                bitmap = ImageSharpHelper.ToBitmap(image);
            }
            return true;
        }
        
    }
}
