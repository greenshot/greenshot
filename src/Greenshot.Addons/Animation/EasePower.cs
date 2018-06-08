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
    /// A supporting class to do Pow calculations for easing
    /// </summary>
    public static class EasePower
	{
		public static double EaseIn(double s, int power)
		{
			return Math.Pow(s, power);
		}

		public static double EaseInOut(double s, int power)
		{
			s *= 2;
			if (s < 1)
			{
				return EaseIn(s, power) / 2;
			}
			var sign = power % 2 == 0 ? -1 : 1;
			return sign / 2.0 * (Math.Pow(s - 2, power) + sign * 2);
		}

		public static double EaseOut(double s, int power)
		{
			var sign = power % 2 == 0 ? -1 : 1;
			return sign * (Math.Pow(s - 1, power) + sign);
		}
	}
}