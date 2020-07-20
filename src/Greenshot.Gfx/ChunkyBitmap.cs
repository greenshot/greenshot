// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Gfx
{
    /// <summary>
    /// A bitmap wrapper with memory from Marshal.AllocHGlobal
    /// </summary>
    /// <typeparam name="TPixelLayout">struct for the pixel information</typeparam>
    public class ChunkyBitmap : UnmanagedBitmap<byte>
    {

        /// <summary>
        /// The constructor for the UnmanagedBitmap
        /// </summary>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <param name="horizontalPixelsPerInch">float</param>
        /// <param name="verticalPixelsPerInch">float</param>
        public ChunkyBitmap(int width, int height, float horizontalPixelsPerInch = 0.96f, float verticalPixelsPerInch = 0.96f) : base(width, height, horizontalPixelsPerInch, verticalPixelsPerInch)
        {
        }

        /// <summary>
        /// The constructor for the UnmanagedBitmap with already initialized bits
        /// </summary>
        /// <param name="bits">IntPtr to the bits, this will not be freed</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <param name="horizontalPixelsPerInch">float</param>
        /// <param name="verticalPixelsPerInch">float</param>
        public ChunkyBitmap(IntPtr bits, int width, int height, float horizontalPixelsPerInch = 0.96f, float verticalPixelsPerInch = 0.96f) : base(bits, width, height, horizontalPixelsPerInch, verticalPixelsPerInch)
        {
        }
    }
}
