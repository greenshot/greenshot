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

using System.Drawing;

namespace Greenshot.Addons.Animation
{
	/// <summary>
	///     Implementation of the ColorAnimator
	/// </summary>
	public class ColorAnimator : AnimatorBase<Color>
	{
        /// <summary>
        /// Create a color animator
        /// </summary>
        /// <param name="first">Color to start with</param>
        /// <param name="last">Color to end with</param>
        /// <param name="frames">int amount of frames to animate</param>
        /// <param name="easingType">EasingTypes, the easing type to use</param>
        /// <param name="easingMode">EasingModes, the easing mode to use</param>
		public ColorAnimator(Color first, Color last, int frames, EasingTypes easingType = EasingTypes.Linear, EasingModes easingMode = EasingModes.EaseIn)
			: base(first, last, frames, easingType, easingMode)
		{
		}

        /// <inheritdoc />
        public override Color Next()
		{
			if (!NextFrame)
			{
				return Current;
			}
			var easingValue = EasingValue;
			double da = Last.A - First.A;
			double dr = Last.R - First.R;
			double dg = Last.G - First.G;
			double db = Last.B - First.B;
			var a = First.A + (int) (easingValue * da);
			var r = First.R + (int) (easingValue * dr);
			var g = First.G + (int) (easingValue * dg);
			var b = First.B + (int) (easingValue * db);
			Current = Color.FromArgb(a, r, g, b);
			return Current;
		}
	}
}