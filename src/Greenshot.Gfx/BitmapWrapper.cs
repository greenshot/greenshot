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
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Greenshot.Gfx
{
	/// <summary>
	///     Wrap an Bitmap
	/// </summary>
	public class BitmapWrapper : IBitmapWithNativeSupport
	{
		// Underlying image
		private readonly Bitmap _bitmap;

        /// <summary>
        /// Constructor taking a Bitmap
        /// </summary>
        /// <param name="bitmap"></param>
		public BitmapWrapper(Bitmap bitmap)
		{
			// Make sure the orientation is set correctly so Greenshot can process the image correctly
			bitmap.Orientate();
			_bitmap = bitmap;
		}

        /// <inheritdoc/>
		public void Dispose()
		{
			_bitmap.Dispose();
		}

        /// <inheritdoc />
        public int Height => _bitmap.Height;

		/// <inheritdoc />
		public int Width => _bitmap.Width;

		/// <inheritdoc />
		public System.Drawing.Imaging.PixelFormat PixelFormat => _bitmap.PixelFormat;

        /// <inheritdoc />
		public float HorizontalResolution => _bitmap.HorizontalResolution;

        /// <inheritdoc />
		public float VerticalResolution => _bitmap.VerticalResolution;

        /// <inheritdoc />
		public Bitmap NativeBitmap => _bitmap;

        /// <inheritdoc />
        public BitmapSource NativeBitmapSource
        {
            get
            {
                var bitmapData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadOnly, _bitmap.PixelFormat);
                try
                {
                    return BitmapSource.Create(bitmapData.Width, bitmapData.Height, _bitmap.HorizontalResolution, _bitmap.VerticalResolution, PixelFormats.Bgr24, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                }
                finally
                {
                    _bitmap.UnlockBits(bitmapData);
                }
            }
        }

        /// <inheritdoc />
        public Size Size => new Size(Width, Height);
        
        /// <summary>
		///     Factory method
		/// </summary>
		/// <param name="bitmap">Image</param>
		/// <returns>IBitmap</returns>
		public static IBitmapWithNativeSupport FromBitmap(Bitmap bitmap)
		{
			return bitmap == null ? null : new BitmapWrapper(bitmap);
		}
	}
}