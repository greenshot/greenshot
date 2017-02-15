#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
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

#endregion

#region Usings

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Svg;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Create an image look like of the SVG
	/// </summary>
	public class SvgImage : IImage
	{
		private readonly SvgDocument _svgDocument;

		private Image _imageClone;

		/// <summary>
		///     Default constructor
		/// </summary>
		/// <param name="stream"></param>
		public SvgImage(Stream stream)
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
		public PixelFormat PixelFormat => Image.PixelFormat;

		/// <summary>
		///     Horizontal resolution of the underlying image
		/// </summary>
		public float HorizontalResolution => Image.HorizontalResolution;

		/// <summary>
		///     Vertical resolution of the underlying image
		/// </summary>
		public float VerticalResolution => Image.VerticalResolution;

		/// <summary>
		///     Unterlying image, or an on demand rendered version with different attributes as the original
		/// </summary>
		public Image Image
		{
			get
			{
				if (_imageClone?.Height == Height && _imageClone?.Width == Width)
				{
					return _imageClone;
				}
				// Calculate new image clone
				_imageClone?.Dispose();
				_imageClone = ImageHelper.CreateEmpty(Width, Height, PixelFormat.Format32bppArgb, Color.Transparent, 96, 96);
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
		/// <returns>IImage</returns>
		public static IImage FromStream(Stream stream)
		{
			return new SvgImage(stream);
		}
	}
}