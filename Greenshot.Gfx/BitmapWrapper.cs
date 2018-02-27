#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System.Drawing;
using System.Drawing.Imaging;

#endregion

namespace Greenshot.Gfx
{
	/// <summary>
	///     Wrap an Bitmap, make it resizeable
	/// </summary>
	public class BitmapWrapper : IBitmap
	{
		// Underlying image, is used to generate a resized version of it when needed
		private readonly Bitmap _bitmap;
		private Bitmap _bitmapClone;

		public BitmapWrapper(Bitmap bitmap)
		{
			// Make sure the orientation is set correctly so Greenshot can process the image correctly
			bitmap.Orientate();
			_bitmap = bitmap;
			Width = _bitmap.Width;
			Height = _bitmap.Height;
		}

		public void Dispose()
		{
			_bitmap.Dispose();
			_bitmapClone?.Dispose();
		}

		/// <summary>
		///     Height of the image, can be set to change
		/// </summary>
		public int Height { get; set; }

		/// <summary>
		///     Width of the image, can be set to change.
		/// </summary>
		public int Width { get; set; }

		/// <summary>
		///     Size of the image
		/// </summary>
		public Size Size => new Size(Width, Height);

		/// <summary>
		///     Pixelformat of the underlying image
		/// </summary>
		public PixelFormat PixelFormat => Bitmap.PixelFormat;

		public float HorizontalResolution => Bitmap.HorizontalResolution;
		public float VerticalResolution => Bitmap.VerticalResolution;

		public Bitmap Bitmap
		{
			get
			{
				if (_bitmapClone == null)
				{
					if (_bitmap.Height == Height && _bitmap.Width == Width)
					{
						return _bitmap;
					}
				}
				if (_bitmapClone?.Height == Height && _bitmapClone?.Width == Width)
				{
					return _bitmapClone;
				}
				// Calculate new image clone
				_bitmapClone?.Dispose();
				_bitmapClone = _bitmap.Resize(false, Width, Height);
				return _bitmapClone;
			}
		}

		/// <summary>
		///     Factory method
		/// </summary>
		/// <param name="bitmap">Image</param>
		/// <returns>IBitmap</returns>
		public static IBitmap FromImage(Bitmap bitmap)
		{
			return bitmap == null ? null : new BitmapWrapper(bitmap);
		}
	}
}