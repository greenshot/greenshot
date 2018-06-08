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
	///     Implementation of the SizeAnimator
	/// </summary>
	public class SizeAnimator : AnimatorBase<NativeSize>
	{
		public SizeAnimator(NativeSize first, NativeSize last, int frames, EasingTypes easingType = EasingTypes.Linear, EasingModes easingMode = EasingModes.EaseIn)
			: base(first, last, frames, easingType, easingMode)
		{
		}

        /// <summary>
        ///     Calculate the next frame values
        /// </summary>
        /// <returns>NativeSize</returns>
        public override NativeSize Next()
		{
			if (!NextFrame)
			{
				return Current;
			}
			var easingValue = EasingValue;
			double dw = Last.Width - First.Width;
			double dh = Last.Height - First.Height;
			var width = First.Width + (int) (easingValue * dw);
			var height = First.Height + (int) (easingValue * dh);
			Current = new NativeSize(width, height);
			return Current;
		}
	}
}