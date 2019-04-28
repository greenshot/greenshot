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

using System.Drawing;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.FastBitmap
{
	/// <summary>
	///     This is the implementation of the IFastBitmap for 32 bit images (no Alpha)
	/// </summary>
	public unsafe class Fast32RgbBitmap : FastBitmapBase
	{
        /// <summary>
        /// Constructor which takes an IBitmap to wrap the fastbitmap logic around it
        /// </summary>
        /// <param name="source">IBitmapWithNativeSupport</param>
        /// <param name="area">NativeRect optional</param>
        public Fast32RgbBitmap(IBitmapWithNativeSupport source, NativeRect? area = null) : base(source, area)
		{
		}

	    /// <inheritdoc />
	    public override int BytesPerPixel { get; } = 4;

        /// <summary>
        ///     Retrieve the color at location x,y
        ///     Before the first time this is called the Lock() should be called once!
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <returns>Color</returns>
        public override Color GetColorAt(int x, int y)
		{
			var offset = x * 4 + y * Stride;
			return Color.FromArgb(255, Pointer[PixelformatIndexR + offset], Pointer[PixelformatIndexG + offset], Pointer[PixelformatIndexB + offset]);
		}

		/// <summary>
		///     Set the color at location x,y
		///     Before the first time this is called the Lock() should be called once!
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="color"></param>
		public override void SetColorAt(int x, int y, ref Color color)
		{
			var offset = x * 4 + y * Stride;
			Pointer[PixelformatIndexR + offset] = color.R;
			Pointer[PixelformatIndexG + offset] = color.G;
			Pointer[PixelformatIndexB + offset] = color.B;
		}

		/// <inheritdoc />
		public override void GetColorAt(int x, int y, byte[] color, int colorIndex = 0)
		{
			var offset = x * 4 + y * Stride;
			color[colorIndex++] = Pointer[PixelformatIndexR + offset];
			color[colorIndex++] = Pointer[PixelformatIndexG + offset];
			color[colorIndex] = Pointer[PixelformatIndexB + offset];
		}

	    /// <inheritdoc />
	    public override void GetColorAt(int x, int y, byte* color, int colorIndex = 0)
	    {
	        var offset = x * 4 + y * Stride;
	        color[colorIndex++] = Pointer[PixelformatIndexR + offset];
	        color[colorIndex++] = Pointer[PixelformatIndexG + offset];
	        color[colorIndex] = Pointer[PixelformatIndexB + offset];
	    }

        /// <inheritdoc />
        public override void SetColorAt(int x, int y, byte[] color, int colorIndex = 0)
        {
			var offset = x * 4 + y * Stride;
			Pointer[PixelformatIndexR + offset] = color[colorIndex++]; // R
			Pointer[PixelformatIndexG + offset] = color[colorIndex++];
			Pointer[PixelformatIndexB + offset] = color[colorIndex];
		}

	    /// <inheritdoc />
	    public override void SetColorAt(int x, int y, byte* color, int colorIndex = 0)
	    {
	        var offset = x * 4 + y * Stride;
	        Pointer[PixelformatIndexR + offset] = color[colorIndex++]; // R
	        Pointer[PixelformatIndexG + offset] = color[colorIndex++];
	        Pointer[PixelformatIndexB + offset] = color[colorIndex];
	    }
    }
}