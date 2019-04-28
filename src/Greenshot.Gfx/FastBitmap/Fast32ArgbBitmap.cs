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
	///     This is the implementation of the IFastBitmap for 32 bit images with Alpha
	/// </summary>
	public unsafe class Fast32ArgbBitmap : FastBitmapBase, IFastBitmapWithBlend
	{
        /// <summary>
        /// Constructor which takes an IBitmap to wrap the fastbitmap logic around it
        /// </summary>
        /// <param name="source">IBitmapWithNativeSupport</param>
        /// <param name="area">NativeRect optional</param>
        public Fast32ArgbBitmap(IBitmapWithNativeSupport source, NativeRect? area = null) : base(source, area)
		{
			BackgroundBlendColor = Color.White;
		}

        /// <inheritdoc />
        public override int BytesPerPixel { get; } = 4;

        /// <inheritdoc />
        public override bool HasAlphaChannel => true;

        /// <inheritdoc />
        public Color BackgroundBlendColor { get; set; }

		/// <inheritdoc />
		public override Color GetColorAt(int x, int y)
		{
			var offset = x * 4 + y * Stride;
			return Color.FromArgb(
				Pointer[PixelformatIndexA + offset],
				Pointer[PixelformatIndexR + offset],
				Pointer[PixelformatIndexG + offset],
				Pointer[PixelformatIndexB + offset]);
		}

		/// <inheritdoc />
		public override void SetColorAt(int x, int y, ref Color color)
		{
			var offset = x * 4 + y * Stride;
			Pointer[PixelformatIndexA + offset] = color.A;
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
			color[colorIndex++] = Pointer[PixelformatIndexB + offset];
			color[colorIndex] = Pointer[PixelformatIndexA + offset];
		}

		/// <inheritdoc />
	    public override void GetColorAt(int x, int y, byte* color, int colorIndex = 0)
	    {
	        var offset = x * 4 + y * Stride;
	        color[colorIndex++] = Pointer[PixelformatIndexR + offset];
	        color[colorIndex++] = Pointer[PixelformatIndexG + offset];
	        color[colorIndex++] = Pointer[PixelformatIndexB + offset];
	        color[colorIndex] = Pointer[PixelformatIndexA + offset];
	    }

		/// <inheritdoc />
		public override void SetColorAt(int x, int y, byte[] color, int colorIndex = 0)
		{
			var offset = x * 4 + y * Stride;
			Pointer[PixelformatIndexR + offset] = color[colorIndex++]; // R
			Pointer[PixelformatIndexG + offset] = color[colorIndex++];
			Pointer[PixelformatIndexB + offset] = color[colorIndex++];
			Pointer[PixelformatIndexA + offset] = color[colorIndex];
		}

		/// <inheritdoc />
	    public override void SetColorAt(int x, int y, byte* color, int colorIndex = 0)
	    {
	        var offset = x * 4 + y * Stride;
	        Pointer[PixelformatIndexR + offset] = color[colorIndex++]; // R
	        Pointer[PixelformatIndexG + offset] = color[colorIndex++];
	        Pointer[PixelformatIndexB + offset] = color[colorIndex++];
	        Pointer[PixelformatIndexA + offset] = color[colorIndex];
	    }

		/// <inheritdoc />
        public Color GetBlendedColorAt(int x, int y)
		{
			var offset = x * 4 + y * Stride;
			int a = Pointer[PixelformatIndexA + offset];
			int red = Pointer[PixelformatIndexR + offset];
			int green = Pointer[PixelformatIndexG + offset];
			int blue = Pointer[PixelformatIndexB + offset];

			if (a < 255)
			{
				// As the request is to get without alpha, we blend.
				var rem = 255 - a;
				red = (red * a + BackgroundBlendColor.R * rem) / 255;
				green = (green * a + BackgroundBlendColor.G * rem) / 255;
				blue = (blue * a + BackgroundBlendColor.B * rem) / 255;
			}
			return Color.FromArgb(255, red, green, blue);
		}
	}
}