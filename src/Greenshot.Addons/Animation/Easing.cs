// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
		public static double EaseIn(double linearStep, EasingTypes type) =>
            type switch
            {
                EasingTypes.Step => (linearStep < 0.5 ? 0 : 1),
                EasingTypes.Linear => linearStep,
                EasingTypes.Sine => EaseSine.EaseIn(linearStep),
                EasingTypes.Quadratic => EasePower.EaseIn(linearStep, 2),
                EasingTypes.Cubic => EasePower.EaseIn(linearStep, 3),
                EasingTypes.Quartic => EasePower.EaseIn(linearStep, 4),
                EasingTypes.Quintic => EasePower.EaseIn(linearStep, 5),
                _ => throw new NotImplementedException()
            };

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
		public static double EaseInOut(double linearStep, EasingTypes type) =>
            type switch
            {
                EasingTypes.Step => (linearStep < 0.5 ? 0 : 1),
                EasingTypes.Linear => linearStep,
                EasingTypes.Sine => EaseSine.EaseInOut(linearStep),
                EasingTypes.Quadratic => EasePower.EaseInOut(linearStep, 2),
                EasingTypes.Cubic => EasePower.EaseInOut(linearStep, 3),
                EasingTypes.Quartic => EasePower.EaseInOut(linearStep, 4),
                EasingTypes.Quintic => EasePower.EaseInOut(linearStep, 5),
                _ => throw new NotImplementedException()
            };

        /// <summary>
	    /// Apply easy out 
	    /// </summary>
	    /// <param name="linearStep">double</param>
	    /// <param name="type">EasingTypes</param>
	    /// <returns>double</returns>
        public static double EaseOut(double linearStep, EasingTypes type) =>
            type switch
            {
                EasingTypes.Step => (linearStep < 0.5 ? 0 : 1),
                EasingTypes.Linear => linearStep,
                EasingTypes.Sine => EaseSine.EaseOut(linearStep),
                EasingTypes.Quadratic => EasePower.EaseOut(linearStep, 2),
                EasingTypes.Cubic => EasePower.EaseOut(linearStep, 3),
                EasingTypes.Quartic => EasePower.EaseOut(linearStep, 4),
                EasingTypes.Quintic => EasePower.EaseOut(linearStep, 5),
                _ => throw new NotImplementedException()
            };
    }
}