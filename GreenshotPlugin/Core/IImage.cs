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

using System;
using System.Drawing;
using System.Drawing.Imaging;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     The image interface, this abstracts an image
	/// </summary>
	public interface IImage : IDisposable
	{
		/// <summary>
		///     Height of the image, can be set to change
		/// </summary>
		int Height { get; set; }

		/// <summary>
		///     Width of the image, can be set to change.
		/// </summary>
		int Width { get; set; }

		/// <summary>
		///     Size of the image
		/// </summary>
		Size Size { get; }

		/// <summary>
		///     Pixelformat of the underlying image
		/// </summary>
		PixelFormat PixelFormat { get; }

		/// <summary>
		///     Vertical resolution of the underlying image
		/// </summary>
		float VerticalResolution { get; }

		/// <summary>
		///     Horizontal resolution of the underlying image
		/// </summary>
		float HorizontalResolution { get; }

		/// <summary>
		///     Unterlying image, or an on demand rendered version with different attributes as the original
		/// </summary>
		Image Image { get; }
	}
}