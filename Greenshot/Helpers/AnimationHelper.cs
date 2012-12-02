/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
	
namespace Greenshot.Helpers {
	/// <summary>
	/// Base class for the animation logic, this only implements Properties and a constructor
	/// </summary>
	/// <typeparam name="T">Type for the animation, like Point/Rectangle/Size</typeparam>
	public abstract class AnimatorBase<T> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AnimatorBase<T>));
		protected T first;
		protected T last;
		protected T current;
		protected double frames;
		protected double currentFrame = 0;
		
		public double Frames {
			get { return frames; }
		}

		public double CurrentFrame {
			get { return currentFrame; }
		}

		public T First {
			get { return first; }
		}
		
		public T Last {
			get { return last; }
		}
		
		public void ChangeDestination(T last) {
			ChangeDestination(last, frames);
		}

		public void ChangeDestination(T last, double frames) {
			this.first = current;
			this.currentFrame = 0;
			this.frames = frames;
			this.last = last;
		}

		public EasingType EasingType {
			get;
			set;
		}
		public EasingMode EasingMode {
			get;
			set;
		}

		protected double EasingValue {
			get {
				switch (EasingMode) {
					case EasingMode.EaseOut:
						return Easing.EaseOut(currentFrame / frames, EasingType);
					case EasingMode.EaseInOut:
						return Easing.EaseInOut(currentFrame / frames, EasingType);
					case EasingMode.EaseIn:
					default:
						return Easing.EaseIn(currentFrame / frames, EasingType);
				}
			}
		}

		public AnimatorBase(T first, T last, int frames, EasingType easingType, EasingMode easingMode) {
			this.first = first;
			this.last = last;
			this.frames = frames;
			this.current = first;
			this.EasingType = easingType;
			this.EasingMode = easingMode;
		}
		
		public virtual void Reset() {
			currentFrame = 0;
			current = first;
		}

		public virtual T Current {
			get {
				return current;
			}
		}
		
		public virtual bool hasNext {
			get {
				return currentFrame < frames;
			}
		}

		public abstract T Next();
	}

	/// <summary>
	/// Implementation of the RectangleAnimator
	/// </summary>
	public class RectangleAnimator : AnimatorBase<Rectangle> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(RectangleAnimator));

		public RectangleAnimator(Rectangle first, Rectangle last, int frames)
			: base(first, last, frames, EasingType.Linear, EasingMode.EaseIn) {
		}
		public RectangleAnimator(Rectangle first, Rectangle last, int frames, EasingType easingType)
			: base(first, last, frames, easingType, EasingMode.EaseIn) {
		}

		public RectangleAnimator(Rectangle first, Rectangle last, int frames, EasingType easingType, EasingMode easingMode)
			: base(first, last, frames, easingType, easingMode) {
		}

		public override Rectangle Next() {
			if (hasNext) {
				currentFrame++;

				double easingValue = EasingValue;
				double dx = last.X - first.X;
				double dy = last.Y - first.Y;

				int x = first.X + (int)(easingValue * dx);
				int y = first.Y + (int)(easingValue * dy);
				double dw = last.Width - first.Width;
				double dh = last.Height - first.Height;
				int width = first.Width + (int)(easingValue * dw);
				int height = first.Height + (int)(easingValue * dh);
				current = new Rectangle(x, y, width, height);
			}
			return current;
		}
	}

	/// <summary>
	/// Implementation of the PointAnimator
	/// </summary>
	public class PointAnimator : AnimatorBase<Point> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PointAnimator));
		public PointAnimator(Point first, Point last, int frames)
			: base(first, last, frames, EasingType.Linear, EasingMode.EaseIn) {
		}
		public PointAnimator(Point first, Point last, int frames, EasingType easingType)
			: base(first, last, frames, easingType, EasingMode.EaseIn) {
		}
		public PointAnimator(Point first, Point last, int frames, EasingType easingType, EasingMode easingMode)
			: base(first, last, frames, easingType, easingMode) {
		}
		public override Point Next() {
			if (hasNext) {
				currentFrame++;

				double easingValue = EasingValue;
				double dx = last.X - first.X;
				double dy = last.Y - first.Y;

				int x = first.X + (int)(easingValue * dx);
				int y = first.Y + (int)(easingValue * dy);
				current = new Point(x, y);
			}
			return current;
		}
	}

	/// <summary>
	/// Implementation of the SizeAnimator
	/// </summary>
	public class SizeAnimator : AnimatorBase<Size> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(SizeAnimator));
		public SizeAnimator(Size first, Size last, int frames)
			: base(first, last, frames, EasingType.Linear, EasingMode.EaseIn) {
		}
		public SizeAnimator(Size first, Size last, int frames, EasingType easingType)
			: base(first, last, frames, easingType, EasingMode.EaseIn) {
		}
		public SizeAnimator(Size first, Size last, int frames, EasingType easingType, EasingMode easingMode)
			: base(first, last, frames, easingType, easingMode) {
		}

		public override Size Next() {
			if (hasNext) {
				currentFrame++;

				double easingValue = EasingValue;
				double dw = last.Width - first.Width;
				double dh = last.Height - first.Height;
				int width = first.Width + (int)(easingValue * dw);
				int height = first.Height + (int)(easingValue * dh);
				current = new Size(width, height);
			}
			return current;
		}
	}

	/// <summary>
	/// Easing logic, to make the animations more "fluent"
	/// </summary>
	public static class Easing {
		// Adapted from http://www.robertpenner.com/easing/penner_chapter7_tweening.pdf

		public static float Ease(double linearStep, float acceleration, EasingType type) {
			double easedStep = acceleration > 0 ? EaseIn(linearStep, type) : acceleration < 0 ? EaseOut(linearStep, type) : (float)linearStep;
			return MathHelper.Lerp(linearStep, easedStep, Math.Abs(acceleration));
		}

		public static double EaseIn(double linearStep, EasingType type) {
			switch (type) {
				case EasingType.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingType.Linear:
					return (double)linearStep;
				case EasingType.Sine:
					return Sine.EaseIn(linearStep);
				case EasingType.Quadratic:
					return Power.EaseIn(linearStep, 2);
				case EasingType.Cubic:
					return Power.EaseIn(linearStep, 3);
				case EasingType.Quartic:
					return Power.EaseIn(linearStep, 4);
				case EasingType.Quintic:
					return Power.EaseIn(linearStep, 5);
			}
			throw new NotImplementedException();
		}

		public static double EaseOut(double linearStep, EasingType type) {
			switch (type) {
				case EasingType.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingType.Linear:
					return (double)linearStep;
				case EasingType.Sine:
					return Sine.EaseOut(linearStep);
				case EasingType.Quadratic:
					return Power.EaseOut(linearStep, 2);
				case EasingType.Cubic:
					return Power.EaseOut(linearStep, 3);
				case EasingType.Quartic:
					return Power.EaseOut(linearStep, 4);
				case EasingType.Quintic:
					return Power.EaseOut(linearStep, 5);
			}
			throw new NotImplementedException();
		}

		public static double EaseInOut(double linearStep, EasingType easeInType, EasingType easeOutType) {
			return linearStep < 0.5 ? EaseInOut(linearStep, easeInType) : EaseInOut(linearStep, easeOutType);
		}

		public static double EaseInOut(double linearStep, EasingType type) {
			switch (type) {
				case EasingType.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingType.Linear:
					return (float)linearStep;
				case EasingType.Sine:
					return Sine.EaseInOut(linearStep);
				case EasingType.Quadratic:
					return Power.EaseInOut(linearStep, 2);
				case EasingType.Cubic:
					return Power.EaseInOut(linearStep, 3);
				case EasingType.Quartic:
					return Power.EaseInOut(linearStep, 4);
				case EasingType.Quintic:
					return Power.EaseInOut(linearStep, 5);
			}
			throw new NotImplementedException();
		}

		static class Sine {
			public static double EaseIn(double s) {
				return (double)Math.Sin(s * MathHelper.HalfPi - MathHelper.HalfPi) + 1;
			}
			public static double EaseOut(double s) {
				return (double)Math.Sin(s * MathHelper.HalfPi);
			}
			public static double EaseInOut(double s) {
				return (double)(Math.Sin(s * MathHelper.Pi - MathHelper.HalfPi) + 1) / 2;
			}
		}

		static class Power {
			public static double EaseIn(double s, int power) {
				return (double)Math.Pow(s, power);
			}
			public static double EaseOut(double s, int power) {
				var sign = power % 2 == 0 ? -1 : 1;
				return (double)(sign * (Math.Pow(s - 1, power) + sign));
			}
			public static double EaseInOut(double s, int power) {
				s *= 2;
				if (s < 1)
					return EaseIn(s, power) / 2;
				var sign = power % 2 == 0 ? -1 : 1;
				return (float)(sign / 2.0 * (Math.Pow(s - 2, power) + sign * 2));
			}
		}
	}

	public enum EasingType {
		Step,
		Linear,
		Sine,
		Quadratic,
		Cubic,
		Quartic,
		Quintic
	}

	public enum EasingMode {
		EaseIn,
		EaseOut,
		EaseInOut
	}

	public static class MathHelper {
		public const float Pi = (float)Math.PI;
		public const float HalfPi = (float)(Math.PI / 2);

		public static float Lerp(double from, double to, double step) {
			return (float)((to - from) * step + from);
		}
	}
}
