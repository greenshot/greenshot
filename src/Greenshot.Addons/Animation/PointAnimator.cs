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

using Dapplo.Windows.Common.Structs;

#endregion

namespace Greenshot.Addons.Animation
{
	/// <summary>
	///     Implementation of the PointAnimator
	/// </summary>
	public class PointAnimator : AnimatorBase<NativePoint>
	{
		public PointAnimator(NativePoint first, NativePoint last, int frames, EasingTypes easingType = EasingTypes.Linear, EasingModes easingMode = EasingModes.EaseIn)
			: base(first, last, frames, easingType, easingMode)
		{
		}

		/// <summary>
		///     Calculate the next frame value
		/// </summary>
		/// <returns>NativePoint</returns>
		public override NativePoint Next()
		{
			if (!NextFrame)
			{
				return Current;
			}
			var easingValue = EasingValue;
			double dx = Last.X - First.X;
			double dy = Last.Y - First.Y;

			var x = First.X + (int) (easingValue * dx);
			var y = First.Y + (int) (easingValue * dy);
			Current = new NativePoint(x, y);
			return Current;
		}
	}
}