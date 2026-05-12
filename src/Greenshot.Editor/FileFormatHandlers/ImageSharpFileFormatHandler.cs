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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Image = SixLabors.ImageSharp.Image;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This is the ImageSharp file format handler
    /// </summary>
    public class ImageSharpFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".png", ".bmp", ".gif", ".jpg", ".jpeg", ".tiff", ".tif", ".tga", ".pbm", ".webp" };
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

            bool hasAlpha = bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb || bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb;

            var versionString = "Greenshot " + EnvironmentInfo.GetGreenshotVersion(true);
            if (extension == ".png")
            {
                surfaceOutputSettings ??= new SurfaceOutputSettings(Base.Core.Enums.OutputFormat.png);
                // Access the PNG-specific metadata
                var pngMetadata = image.Metadata.GetPngMetadata();
                // Add or update the "Software" text chunk
                pngMetadata.TextData.Add(new PngTextData("Software", versionString, "en", "en"));
            }

            // Ensure an EXIF profile exists
            if (image.Metadata.ExifProfile == null)
            {
                image.Metadata.ExifProfile = new ExifProfile();
            }
            // Set the Software tag
            image.Metadata.ExifProfile.SetValue(ExifTag.Software, versionString);

            surfaceOutputSettings ??= new SurfaceOutputSettings();

            // Support reducing colors for formats that support it, but only if the setting is enabled
            IQuantizer quantizer = null;
            if (surfaceOutputSettings.ReduceColors)
            {
                quantizer = new SixLabors.ImageSharp.Processing.Processors.Quantization.WuQuantizer(new QuantizerOptions
                {
                    MaxColors = 256,
                    Dither = null // Disables dithering
                });
            }

            IImageEncoder encoder = extension switch
            {
                ".png" => new PngEncoder() { Quantizer = quantizer, ColorType = surfaceOutputSettings.ReduceColors ? PngColorType.Palette : hasAlpha ? PngColorType.RgbWithAlpha : PngColorType.Rgb },
                ".bmp" => new BmpEncoder() { Quantizer = quantizer, BitsPerPixel = surfaceOutputSettings.ReduceColors ? BmpBitsPerPixel.Pixel8 : hasAlpha ? BmpBitsPerPixel.Pixel32 : BmpBitsPerPixel.Pixel24 },
                ".gif" => new GifEncoder() { Quantizer = quantizer },
                ".jpg" => new JpegEncoder() { Quality = surfaceOutputSettings.JPGQuality },
                ".jpeg" => new JpegEncoder() { Quality = surfaceOutputSettings.JPGQuality },
                ".tiff" => new TiffEncoder() { Quantizer = quantizer, BitsPerPixel = surfaceOutputSettings.ReduceColors ? TiffBitsPerPixel.Bit8 : TiffBitsPerPixel.Bit24 },
                ".tif" => new TiffEncoder() { Quantizer = quantizer, BitsPerPixel = surfaceOutputSettings.ReduceColors ? TiffBitsPerPixel.Bit8 : TiffBitsPerPixel.Bit24 },
                ".tga" => new TgaEncoder(),
                ".pbm" => new PbmEncoder(),
                ".webp" => new WebpEncoder() { Quality = surfaceOutputSettings.JPGQuality },
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