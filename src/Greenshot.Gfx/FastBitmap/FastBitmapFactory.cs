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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.FastBitmap
{
	/// <summary>
	///     The factory class for the fast bitmap implementation
	/// </summary>
	public static class FastBitmapFactory
	{
		/// <summary>
		///     Factory for creating a FastBitmap depending on the pixelformat of the source
		///     The supplied rectangle specifies the area for which the FastBitmap does its thing
		/// </summary>
		/// <param name="source">Bitmap to access</param>
		/// <param name="area">NativeRect which specifies the area to have access to, can be NativeRect.Empty for the whole image</param>
		/// <returns>IFastBitmap</returns>
		public static IFastBitmap Create(IBitmapWithNativeSupport source, NativeRect? area = null)
		{
			switch (source.PixelFormat)
			{
				case PixelFormat.Format8bppIndexed:
					return new FastChunkyBitmap(source, area);
				case PixelFormat.Format24bppRgb:
					return new Fast24RgbBitmap(source, area);
				case PixelFormat.Format32bppRgb:
					return new Fast32RgbBitmap(source, area);
				case PixelFormat.Format32bppArgb:
				case PixelFormat.Format32bppPArgb:
					return new Fast32ArgbBitmap(source, area);
				default:
					throw new NotSupportedException($"Not supported Pixelformat {source.PixelFormat}");
			}
		}

		/// <summary>
		///     Factory for creating a FastBitmap as a destination for the source
		/// </summary>
		/// <param name="source">Bitmap to clone</param>
		/// <param name="pixelFormat">Pixelformat of the cloned bitmap</param>
		/// <param name="area">Area of the bitmap to access, can be NativeRect.Empty for the whole</param>
		/// <returns>IFastBitmap</returns>
		public static IFastBitmap CreateCloneOf(IBitmapWithNativeSupport source, PixelFormat pixelFormat = PixelFormat.DontCare, NativeRect? area = null)
		{
			var destination = source.CloneBitmap(pixelFormat, area);
		    if (!(Create(destination) is FastBitmapBase fastBitmap))
			{
				return null;
			}
			fastBitmap.NeedsDispose = true;
			if (!area.HasValue)
			{
				return fastBitmap;
			}
			fastBitmap.Left = area.Value.Left;
			fastBitmap.Top = area.Value.Top;
			return fastBitmap;
		}

		/// <summary>
		///     Factory for creating a FastBitmap as a destination
		/// </summary>
		/// <param name="newSize">Size</param>
		/// <param name="pixelFormat">PixelFormat</param>
		/// <param name="backgroundColor">Color or null</param>
		/// <param name="horizontalResolution">float for horizontal DPI</param>
		/// <param name="verticalResolution">float for horizontal DPI</param>
		/// <returns>IFastBitmap</returns>
		public static IFastBitmap CreateEmpty(Size newSize, PixelFormat pixelFormat = PixelFormat.DontCare, Color? backgroundColor = null, float horizontalResolution = 96f, float verticalResolution = 96f)
		{
			var destination = BitmapFactory.CreateEmpty(newSize.Width, newSize.Height, pixelFormat, backgroundColor, horizontalResolution, verticalResolution);
			var fastBitmap = Create(destination);
			fastBitmap.NeedsDispose = true;
			return fastBitmap;
		}
	}
}