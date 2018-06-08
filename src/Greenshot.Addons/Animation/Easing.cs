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
    ///     Easing logic, to make the animations more "fluent"
    ///		Adapted from <a href="http://www.robertpenner.com/easing/penner_chapter7_tweening.pdf">here</a>
    /// </summary>
    public static class Easing
	{
        /// <summary>
        /// Apply "ease" to ubfirnatuin
        /// </summary>
        /// <param name="linearStep">double</param>
        /// <param name="acceleration">double</param>
        /// <param name="type">double</param>
        /// <returns>double</returns>
		public static double Ease(double linearStep, double acceleration, EasingTypes type)
		{
			var easedStep = acceleration > 0 ? EaseIn(linearStep, type) : acceleration < 0 ? EaseOut(linearStep, type) : linearStep;
			// Lerp:
			return (easedStep - linearStep) * Math.Abs(acceleration) + linearStep;
		}

        /// <summary>
        /// Apply ease in
        /// </summary>
        /// <param name="linearStep">double</param>
        /// <param name="type">EasingTypes</param>
        /// <returns>double</returns>
		public static double EaseIn(double linearStep, EasingTypes type)
		{
			switch (type)
			{
				case EasingTypes.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingTypes.Linear:
					return linearStep;
				case EasingTypes.Sine:
					return EaseSine.EaseIn(linearStep);
				case EasingTypes.Quadratic:
					return EasePower.EaseIn(linearStep, 2);
				case EasingTypes.Cubic:
					return EasePower.EaseIn(linearStep, 3);
				case EasingTypes.Quartic:
					return EasePower.EaseIn(linearStep, 4);
				case EasingTypes.Quintic:
					return EasePower.EaseIn(linearStep, 5);
			}
			throw new NotImplementedException();
		}

        /// <summary>
        /// Apply ease in-out
        /// </summary>
        /// <param name="linearStep">double</param>
        /// <param name="easeInType">EasingTypes</param>
        /// <param name="easeOutType">EasingTypes</param>
        /// <returns>double</returns>
		public static double EaseInOut(double linearStep, EasingTypes easeInType, EasingTypes easeOutType)
		{
			return linearStep < 0.5 ? EaseInOut(linearStep, easeInType) : EaseInOut(linearStep, easeOutType);
		}

        /// <summary>
        /// Apply easy in out 
        /// </summary>
        /// <param name="linearStep">double</param>
        /// <param name="type">EasingTypes</param>
        /// <returns>double</returns>
		public static double EaseInOut(double linearStep, EasingTypes type)
		{
			switch (type)
			{
				case EasingTypes.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingTypes.Linear:
					return linearStep;
				case EasingTypes.Sine:
					return EaseSine.EaseInOut(linearStep);
				case EasingTypes.Quadratic:
					return EasePower.EaseInOut(linearStep, 2);
				case EasingTypes.Cubic:
					return EasePower.EaseInOut(linearStep, 3);
				case EasingTypes.Quartic:
					return EasePower.EaseInOut(linearStep, 4);
				case EasingTypes.Quintic:
					return EasePower.EaseInOut(linearStep, 5);
			}
			throw new NotImplementedException();
		}

	    /// <summary>
	    /// Apply easy out 
	    /// </summary>
	    /// <param name="linearStep">double</param>
	    /// <param name="type">EasingTypes</param>
	    /// <returns>double</returns>
        public static double EaseOut(double linearStep, EasingTypes type)
		{
			switch (type)
			{
				case EasingTypes.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingTypes.Linear:
					return linearStep;
				case EasingTypes.Sine:
					return EaseSine.EaseOut(linearStep);
				case EasingTypes.Quadratic:
					return EasePower.EaseOut(linearStep, 2);
				case EasingTypes.Cubic:
					return EasePower.EaseOut(linearStep, 3);
				case EasingTypes.Quartic:
					return EasePower.EaseOut(linearStep, 4);
				case EasingTypes.Quintic:
					return EasePower.EaseOut(linearStep, 5);
			}
			throw new NotImplementedException();
		}
	}
}