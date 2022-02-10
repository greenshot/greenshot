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
using System.IO;
using System.Windows.Media.Imaging;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Core;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This is the default .NET bitmap file format handler
    /// </summary>
    public class WmpFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        protected override string[] OurExtensions { get; } = { ".jxr", ".wdp", ".wmp" };

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            try
            {
                var bitmapSource = bitmap.ToBitmapSource();
                var bitmapFrame = BitmapFrame.Create(bitmapSource);
                var jpegXrEncoder = new WmpBitmapEncoder();
                jpegXrEncoder.Frames.Add(bitmapFrame);
                // TODO: Support supplying a quality
                //jpegXrEncoder.ImageQualityLevel = quality / 100f;
                jpegXrEncoder.Save(destination);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            var decoder = new WmpBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
            var bitmapSource = decoder.Frames[0];
            bitmap = bitmapSource.ToBitmap();
            return true;
        }
    }
}
