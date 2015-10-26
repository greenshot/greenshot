/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Dapplo.Windows.SafeHandles;

namespace GreenshotPlugin.Extensions
{
	public static class BitmapExtensions
	{
		/// <summary>
		/// Convert a Bitmap to a BitmapSource
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns>BitmapSource</returns>
		public static BitmapSource ToBitmapSource(this Bitmap bitmap)
		{
			using (var hBitmap = new SafeHBitmapHandle(bitmap.GetHbitmap()))
			{
				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
		}

		/// <summary>
		/// Extension method to convert an Icon to ImageSource (used for WPF)
		/// </summary>
		/// <param name="icon"></param>
		/// <returns>BitmapSource</returns>
		public static BitmapSource ToBitmapSource(this Icon icon)
		{
			var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

			return bitmapSource;
		}

	}
}
