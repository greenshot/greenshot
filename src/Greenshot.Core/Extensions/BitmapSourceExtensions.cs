// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Core.Enums;

namespace Greenshot.Core.Extensions
{
    public static class BitmapSourceExtensions
    {
        /// <summary>
        /// Write the BitmapSource to a stream
        /// </summary>
        /// <param name="bitmapSource">BitmapSource</param>
        /// <param name="outputFormat">OutputFormats</param>
        /// <returns>Stream</returns>
        public static Stream ToStream(this BitmapSource bitmapSource, OutputFormats outputFormat)
        {
            var returnStream = new MemoryStream();
            BitmapEncoder bitmapEncoder;
            switch (outputFormat)
            {
                case OutputFormats.gif:
                    bitmapEncoder = new GifBitmapEncoder();
                    break;
                case OutputFormats.jpg:
                    bitmapEncoder = new JpegBitmapEncoder();
                    break;
                case OutputFormats.png:
                    bitmapEncoder = new PngBitmapEncoder();
                    break;
                case OutputFormats.tiff:
                    bitmapEncoder = new TiffBitmapEncoder();
                    break;
                case OutputFormats.ico:
                case OutputFormats.greenshot:
                    throw new NotSupportedException($"Format {outputFormat}");
                default:
                    bitmapEncoder = new BmpBitmapEncoder();
                    break;
            }
            bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            bitmapEncoder.Save(returnStream);

            return returnStream;
        }

        /// <summary>
        /// Create a NativeSizeFloat object from a BitmapSource
        /// </summary>
        /// <param name="bitmapSource"></param>
        /// <returns></returns>
        public static NativeSizeFloat Size(this BitmapSource bitmapSource)
        {
            return new NativeSizeFloat(bitmapSource.Width, bitmapSource.Height);
        }
    }
}
