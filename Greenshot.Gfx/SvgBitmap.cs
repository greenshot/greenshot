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
using System.IO;
using Svg;

#endregion

namespace Greenshot.Gfx
{
	/// <summary>
	///     Create an image look like of the SVG
	/// </summary>
	public class SvgBitmap : IBitmap
	{
		private readonly SvgDocument _svgDocument;

		private Bitmap _imageClone;

		/// <summary>
		///     Default constructor
		/// </summary>
		/// <param name="stream"></param>
		public SvgBitmap(Stream stream)
		{
			_svgDocument = SvgDocument.Open<SvgDocument>(stream);
			Height = (int) _svgDocument.ViewBox.Height;
			Width = (int) _svgDocument.ViewBox.Width;
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

		/// <summary>
		///     Horizontal resolution of the underlying image
		/// </summary>
		public float HorizontalResolution => Bitmap.HorizontalResolution;

		/// <summary>
		///     Vertical resolution of the underlying image
		/// </summary>
		public float VerticalResolution => Bitmap.VerticalResolution;

		/// <summary>
		///     Unterlying image, or an on demand rendered version with different attributes as the original
		/// </summary>
		public Bitmap Bitmap
        {
			get
			{
				if (_imageClone?.Height == Height && _imageClone?.Width == Width)
				{
					return _imageClone;
				}
				// Calculate new image clone
				_imageClone?.Dispose();
				_imageClone = BitmapFactory.CreateEmpty(Width, Height, PixelFormat.Format32bppArgb, Color.Transparent, 96, 96);
				_svgDocument.Draw((Bitmap) _imageClone);
				return _imageClone;
			}
		}

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			_imageClone?.Dispose();
		}

		/// <summary>
		///     Factory to create via a stream
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <returns>IBitmap</returns>
		public static IBitmap FromStream(Stream stream)
		{
			return new SvgBitmap(stream);
		}
	}
}