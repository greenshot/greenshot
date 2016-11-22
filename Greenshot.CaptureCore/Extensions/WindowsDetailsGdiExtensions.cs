//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Drawing;
using Greenshot.Core;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.CaptureCore.Extensions
{
	public static class WindowsDetailsGdiExtensions
	{
		/// <summary>
		///     Simple screen capture for the Window
		/// </summary>
		/// <param name="window">WindowDetails to make a capture from</param>
		/// <returns>Bitmap</returns>
		public static Bitmap CaptureFromScreen(this WindowDetails window)
		{
			return WindowCapture.CaptureRectangle(window.WindowRectangle);
		}

		/// <summary>
		///     Capture Window with GDI+
		/// </summary>
		/// <param name="windowToCapture">WindowDetails for the window to capture</param>
		/// <param name="capture">The capture to fill</param>
		/// <returns>ICapture</returns>
		public static ICapture CaptureGdiWindow(this WindowDetails windowToCapture, ICapture capture)
		{
			Image capturedImage = windowToCapture.PrintWindow();
			if (capturedImage == null)
			{
				return null;
			}
			capture.Image = capturedImage;
			capture.Location = windowToCapture.Location;
			return capture;
		}
	}
}