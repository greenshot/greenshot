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

using System;
using System.Drawing;
using Dapplo.Windows.Native;

#endregion

namespace Greenshot.Core
{
	/// <summary>
	///     Utilities for getting information on the screen
	/// </summary>
	public static class ScreenHelper
	{
		/// <summary>
		///     Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7. This implementation
		///     can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
		/// </summary>
		/// <returns>
		///     Point with cursor location, relative to the top left corner of the monitor setup (which itself might actually not
		///     be on any screen)
		/// </returns>
		public static Point GetCursorLocationRelativeToScreenBounds()
		{
			var cursorLocation = User32.GetCursorLocation();
			return GetLocationRelativeToScreenBounds(new Point((int) cursorLocation.X, (int) cursorLocation.Y));
		}

		/// <summary>
		///     Converts locationRelativeToScreenOrigin to be relative to top left corner of all screen bounds, which might
		///     be different in multiscreen setups. This implementation
		///     can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
		/// </summary>
		/// <param name="locationRelativeToScreenOrigin"></param>
		/// <returns></returns>
		public static Point GetLocationRelativeToScreenBounds(Point locationRelativeToScreenOrigin)
		{
			Point ret = locationRelativeToScreenOrigin;
			Rectangle bounds = GetScreenBounds();
			ret.Offset(-bounds.X, -bounds.Y);
			return ret;
		}

		/// <summary>
		///     Get the bounds of all screens combined.
		/// </summary>
		/// <returns>A Rectangle of the bounds of the entire display area.</returns>
		public static Rectangle GetScreenBounds()
		{
			int left = 0, top = 0, bottom = 0, right = 0;
			foreach (var display in User32.AllDisplays())
			{
				left = Math.Min(left, display.BoundsRectangle.X);
				top = Math.Min(top, display.BoundsRectangle.Y);
				int screenAbsRight = display.BoundsRectangle.X + display.BoundsRectangle.Width;
				int screenAbsBottom = display.BoundsRectangle.Y + display.BoundsRectangle.Height;
				right = Math.Max(right, screenAbsRight);
				bottom = Math.Max(bottom, screenAbsBottom);
			}
			return new Rectangle(left, top, right + Math.Abs(left), bottom + Math.Abs(top));
		}
	}
}