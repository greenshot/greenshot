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
		public static double Ease(double linearStep, double acceleration, EasingTypes type)
		{
			var easedStep = acceleration > 0 ? EaseIn(linearStep, type) : acceleration < 0 ? EaseOut(linearStep, type) : linearStep;
			// Lerp:
			return (easedStep - linearStep) * Math.Abs(acceleration) + linearStep;
		}

		public static double EaseIn(double linearStep, EasingTypes type)
		{
			switch (type)
			{
				case EasingTypes.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingTypes.Linear:
					return linearStep;
				case EasingTypes.Sine:
					return Sine.EaseIn(linearStep);
				case EasingTypes.Quadratic:
					return Power.EaseIn(linearStep, 2);
				case EasingTypes.Cubic:
					return Power.EaseIn(linearStep, 3);
				case EasingTypes.Quartic:
					return Power.EaseIn(linearStep, 4);
				case EasingTypes.Quintic:
					return Power.EaseIn(linearStep, 5);
			}
			throw new NotImplementedException();
		}

		public static double EaseInOut(double linearStep, EasingTypes easeInType, EasingTypes easeOutType)
		{
			return linearStep < 0.5 ? EaseInOut(linearStep, easeInType) : EaseInOut(linearStep, easeOutType);
		}

		public static double EaseInOut(double linearStep, EasingTypes type)
		{
			switch (type)
			{
				case EasingTypes.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingTypes.Linear:
					return linearStep;
				case EasingTypes.Sine:
					return Sine.EaseInOut(linearStep);
				case EasingTypes.Quadratic:
					return Power.EaseInOut(linearStep, 2);
				case EasingTypes.Cubic:
					return Power.EaseInOut(linearStep, 3);
				case EasingTypes.Quartic:
					return Power.EaseInOut(linearStep, 4);
				case EasingTypes.Quintic:
					return Power.EaseInOut(linearStep, 5);
			}
			throw new NotImplementedException();
		}

		public static double EaseOut(double linearStep, EasingTypes type)
		{
			switch (type)
			{
				case EasingTypes.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingTypes.Linear:
					return linearStep;
				case EasingTypes.Sine:
					return Sine.EaseOut(linearStep);
				case EasingTypes.Quadratic:
					return Power.EaseOut(linearStep, 2);
				case EasingTypes.Cubic:
					return Power.EaseOut(linearStep, 3);
				case EasingTypes.Quartic:
					return Power.EaseOut(linearStep, 4);
				case EasingTypes.Quintic:
					return Power.EaseOut(linearStep, 5);
			}
			throw new NotImplementedException();
		}

		private static class Sine
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

		private static class Power
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
}