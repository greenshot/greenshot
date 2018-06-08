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

#region using

using System;

#endregion

namespace Greenshot.Addons.Animation
{
    /// <summary>
    /// This is a supporting class to do Sin calculations for the easing
    /// </summary>
    public static class EaseSine
	{
		public static double EaseIn(double s)
		{
			return Math.Sin(s * (Math.PI / 2) - Math.PI / 2) + 1;
		}

		public static double EaseInOut(double s)
		{
			return Math.Sin(s * Math.PI - Math.PI / 2 + 1) / 2;
		}

		public static double EaseOut(double s)
		{
			return Math.Sin(s * (Math.PI / 2));
		}
	}
}