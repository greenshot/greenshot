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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This is the default .NET bitmap file format handler
    /// </summary>
    public class DefaultFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DefaultFileFormatHandler));
        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".png", ".bmp", ".gif", ".jpg", ".jpeg", ".tiff", ".tif" };
        public DefaultFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
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
            surfaceOutputSettings ??= new SurfaceOutputSettings();
            var imageEncoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(ie => ie.FilenameExtension.ToLowerInvariant().Contains(extension));
            if (imageEncoder == null)
            {
                return false;
            }
            // Don't use quality parameter for PNG (it doesn't support it and can cause corruption)
            EncoderParameters parameters = null;
            if (imageFormat.Guid != ImageFormat.Png.Guid)
            {
                parameters = new EncoderParameters(1)
                {
                    Param =
                    {
                        [0] = new EncoderParameter(Encoder.Quality, surfaceOutputSettings.JPGQuality)
                    }
                };
            }
            // For those images which are with Alpha, but the format doesn't support this, change it to 24bpp
            if (imageFormat.Guid == ImageFormat.Jpeg.Guid && Image.IsAlphaPixelFormat(bitmap.PixelFormat))
            {
                var nonAlphaImage = ImageHelper.Clone(bitmap, PixelFormat.Format24bppRgb);
                try
                {
                    // Set that this file was written by Greenshot
                    nonAlphaImage.AddTag();
                }
                catch (Exception ex)
                {
                    Log.Warn("Couldn't set 'software used' tag on image.", ex);
                }

                try
                {
                    nonAlphaImage.Save(destination, imageEncoder, parameters);
                }
                catch (Exception ex)
                {
                    Log.Error("Couldn't save image: ", ex);
                }
                finally
                {
                    nonAlphaImage.Dispose();
                }
            }
            else
            {
                // Set that this file was written by Greenshot
                bitmap.AddTag();
                bitmap.Save(destination, imageEncoder, parameters);
            }

            return true;
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            try
            {
                using var tmpImage = Image.FromStream(stream, true, true);
                bitmap = ImageHelper.Clone(tmpImage, PixelFormat.DontCare);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't load image: ", ex);
            }

            bitmap = null;
            return false;
        }
    }
}
