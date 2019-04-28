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

using System.Windows;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.Extensions
{
    /// <summary>
    /// These extensions are for the writable bitmap
    /// </summary>
    public static class WriteableBitmapExtensions
    {
        /// <summary>
        /// Copy the rect from source to target 
        /// </summary>
        /// <param name="target">WriteableBitmap</param>
        /// <param name="source">BitmapSource</param>
        /// <param name="rect">BitmapSource</param>
        public static void CopyPixels(this WriteableBitmap target, BitmapSource source, NativeRect rect)
        {
            // Calculate stride of source
            int stride = source.PixelWidth * (source.Format.BitsPerPixel / 8);

            // Create data array to hold source pixel data
            byte[] data = new byte[stride * source.PixelHeight];

            // Copy source image pixels to the data array
            source.CopyPixels(data, stride, 0);

            // Write the pixel data to the WriteableBitmap.
            target.WritePixels(new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight), data, stride, 0);
        }
    }
}
