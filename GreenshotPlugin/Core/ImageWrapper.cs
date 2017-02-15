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

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Wrap an image, make it resizeable
	/// </summary>
	public class ImageWrapper : IImage
	{
		// Underlying image, is used to generate a resized version of it when needed
		private readonly Image _image;
		private Image _imageClone;

		public ImageWrapper(Image image)
		{
			// Make sure the orientation is set correctly so Greenshot can process the image correctly
			image.Orientate();
			_image = image;
			Width = _image.Width;
			Height = _image.Height;
		}

		public void Dispose()
		{
			_image.Dispose();
			_imageClone?.Dispose();
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

		public float HorizontalResolution => Image.HorizontalResolution;
		public float VerticalResolution => Image.VerticalResolution;

		public Image Image
		{
			get
			{
				if (_imageClone == null)
				{
					if (_image.Height == Height && _image.Width == Width)
					{
						return _image;
					}
				}
				if (_imageClone?.Height == Height && _imageClone?.Width == Width)
				{
					return _imageClone;
				}
				// Calculate new image clone
				_imageClone?.Dispose();
				_imageClone = ImageHelper.ResizeImage(_image, false, Width, Height, null);
				return _imageClone;
			}
		}

		/// <summary>
		///     Factory method
		/// </summary>
		/// <param name="image">Image</param>
		/// <returns>IImage</returns>
		public static IImage FromImage(Image image)
		{
			return image == null ? null : new ImageWrapper(image);
		}
	}
}