// Dapplo - building blocks for desktop applications
// Copyright (C) 2019 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.
// 

using Greenshot.Gfx.Structs;

namespace Greenshot.Gfx.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class BitmapExtensions
    {
        /// <summary>
        /// Scale 2x
        /// </summary>
        /// <param name="bitmap">IBitmapWithNativeSupport</param>
        /// <returns>IBitmapWithNativeSupport</returns>
        public static IBitmapWithNativeSupport Scale2X(this IBitmapWithNativeSupport bitmap)
        {
            switch (bitmap)
            {
                case UnmanagedBitmap<Bgra32> unmanagedBitmap:
                    return unmanagedBitmap.Scale2X();
                case UnmanagedBitmap<Bgr32> unmanagedBitmap:
                    return unmanagedBitmap.Scale2X();
            }

            return ScaleX.Scale2X(bitmap);
        }
        
        /// <summary>
        /// Scale3x
        /// </summary>
        /// <param name="bitmap">IBitmapWithNativeSupport</param>
        /// <returns>IBitmapWithNativeSupport</returns>
        public static IBitmapWithNativeSupport Scale3X(this IBitmapWithNativeSupport bitmap)
        {
            switch (bitmap)
            {
                case UnmanagedBitmap<Bgra32> unmanagedBitmap:
                    return unmanagedBitmap.Scale3X();
                case UnmanagedBitmap<Bgr32> unmanagedBitmap:
                    return unmanagedBitmap.Scale3X();
            }

            return ScaleX.Scale3X(bitmap);
        }
    }
}