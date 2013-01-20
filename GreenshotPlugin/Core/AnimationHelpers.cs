/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;

namespace GreenshotPlugin.Core {

	/// <summary>
	/// Helper interface for passing base type
	/// </summary>
	public interface IAnimator {
		/// <summary>
		/// Is there a next frame?
		/// </summary>
		bool hasNext {
			get;
		}
		/// <summary>
		/// The amount of frames
		/// </summary>
		int Frames {
			get;
		}

		/// <summary>
		/// Current frame number
		/// </summary>
		int CurrentFrameNr {
			get;
		}
	}
	
	/// <summary>
	/// This class is used to store a animation leg
	/// </summary>
	internal class AnimationLeg<T> {
		public T Destination {
			get;
			set;
		}
		
		public int Frames {
			get;
			set;
		}
		
		public EasingType EasingType {
			get;
			set;
		}
		
		public EasingMode EasingMode {
			get;
			set;
		}
	}

	/// <summary>
	/// Base class for the animation logic, this only implements Properties and a constructor
	/// </summary>
	/// <typeparam name="T">Type for the animation, like Point/Rectangle/Size</typeparam>
	public abstract class AnimatorBase<T> : IAnimator {
		protected T first;
		protected T last;
		protected T current;
		private Queue<AnimationLeg<T>> queue = new Queue<AnimationLeg<T>>();
		protected int frames;
		protected int currentFrameNr = 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="first"></param>
		/// <param name="last"></param>
		/// <param name="frames"></param>
		/// <param name="easingType"></param>
		/// <param name="easingMode"></param>
		public AnimatorBase(T first, T last, int frames, EasingType easingType, EasingMode easingMode) {
			this.first = first;
			this.last = last;
			this.frames = frames;
			this.current = first;
			this.EasingType = easingType;
			this.EasingMode = easingMode;
		}

		/// <summary>
		/// The amount of frames
		/// </summary>
		public int Frames {
			get { return frames; }
		}

		/// <summary>
		/// Current frame number
		/// </summary>
		public int CurrentFrameNr {
			get { return currentFrameNr; }
		}

		/// <summary>
		/// First animation value
		/// </summary>
		public T First {
			get { return first; }
		}
		
		/// <summary>
		/// Last animation value, of this "leg"
		/// </summary>
		public T Last {
			get { return last; }
		}

		/// <summary>
		/// Final animation value, this is including the legs
		/// </summary>
		public T Final {
			get {
				if (queue.Count == 0) {
					return last;
				}
				return queue.ToArray()[queue.Count - 1].Destination;
			}
		}
		
		/// <summary>
		/// This restarts the current animation and changes the last frame
		/// </summary>
		/// <param name="newDestination"></param>
		public void ChangeDestination(T newDestination) {
			ChangeDestination(newDestination, frames);
		}
		
		/// <summary>
		/// This restarts the current animation and changes the last frame
		/// </summary>
		/// <param name="newDestination"></param>
		/// <param name="frames"></param>
		public void ChangeDestination(T newDestination, int frames) {
			queue.Clear();
			this.first = current;
			this.currentFrameNr = 0;
			this.frames = frames;
			this.last = newDestination;
		}

		/// <summary>
		/// Queue the destination, it will be used to continue at the last frame
		/// All values will stay the same
		/// </summary>
		/// <param name="queuedDestination"></param>
		public void QueueDestinationLeg(T queuedDestination) {
			QueueDestinationLeg(queuedDestination, Frames, EasingType, EasingMode);
		}

		/// <summary>
		/// Queue the destination, it will be used to continue at the last frame
		/// </summary>
		/// <param name="queuedDestination"></param>
		/// <param name="frames"></param>
		public void QueueDestinationLeg(T queuedDestination, int frames) {
			QueueDestinationLeg(queuedDestination, frames, EasingType, EasingMode);
		}

		/// <summary>
		/// Queue the destination, it will be used to continue at the last frame
		/// </summary>
		/// <param name="queuedDestination"></param>
		/// <param name="frames"></param>
		/// <param name="easingType">EasingType</param>
		public void QueueDestinationLeg(T queuedDestination, int frames, EasingType easingType) {
			QueueDestinationLeg(queuedDestination, frames, easingType, EasingMode);
		}

		/// <summary>
		/// Queue the destination, it will be used to continue at the last frame
		/// </summary>
		/// <param name="queuedDestination"></param>
		/// <param name="frames"></param>
		/// <param name="easingType"></param>
		/// <param name="easingMode"></param>
		public void QueueDestinationLeg(T queuedDestination, int frames, EasingType easingType, EasingMode easingMode) {
			AnimationLeg<T> leg = new AnimationLeg<T>();
			leg.Destination = queuedDestination;
			leg.Frames = frames;
			leg.EasingType = easingType;
			leg.EasingMode = easingMode;
			queue.Enqueue(leg);
		}

		/// <summary>
		/// The EasingType to use for the animation
		/// </summary>
		public EasingType EasingType {
			get;
			set;
		}
		
		/// <summary>
		/// The EasingMode to use for the animation
		/// </summary>
		public EasingMode EasingMode {
			get;
			set;
		}

		/// <summary>
		/// Get the easing value, which is from 0-1 and depends on the frame
		/// </summary>
		protected double EasingValue {
			get {
				switch (EasingMode) {
					case EasingMode.EaseOut:
						return Easing.EaseOut((double)currentFrameNr / (double)frames, EasingType);
					case EasingMode.EaseInOut:
						return Easing.EaseInOut((double)currentFrameNr / (double)frames, EasingType);
					case EasingMode.EaseIn:
					default:
						return Easing.EaseIn((double)currentFrameNr / (double)frames, EasingType);
				}
			}
		}

		/// <summary>
		/// Get the current (previous) frame object
		/// </summary>
		public virtual T Current {
			get {
				return current;
			}
		}
		
		/// <summary>
		/// Returns if there are any frame left, and if this is the case than the frame is increased.
		/// </summary>
		public virtual bool NextFrame {
			get {
				if (currentFrameNr < frames) {
					currentFrameNr++;
					return true;
				}
				if (queue.Count > 0) {
					this.first = current;
					this.currentFrameNr = 0;
					AnimationLeg<T> nextLeg = queue.Dequeue();
					this.last = nextLeg.Destination;
					this.frames = nextLeg.Frames;
					this.EasingType = nextLeg.EasingType;
					this.EasingMode = nextLeg.EasingMode;
					return true;
				}
				return false;
			}
		}
		
		/// <summary>
		/// Are there more frames to animate?
		/// </summary>
		public virtual bool hasNext {
			get {
				if (currentFrameNr < frames) {
					return true;
				}
				return queue.Count > 0;
			}
		}

		/// <summary>
		/// Get the next animation frame value object
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Calculate the next frame object
		/// </summary>
		/// <returns>Rectangle</returns>
		public override Rectangle Next() {
			if (NextFrame) {
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
		
		/// <summary>
		/// Calculate the next frame value
		/// </summary>
		/// <returns>Point</returns>
		public override Point Next() {
			if (NextFrame) {
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

		/// <summary>
		/// Calculate the next frame values
		/// </summary>
		/// <returns>Size</returns>
		public override Size Next() {
			if (NextFrame) {
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
	/// Implementation of the ColorAnimator
	/// </summary>
	public class ColorAnimator : AnimatorBase<Color> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ColorAnimator));
		public ColorAnimator(Color first, Color last, int frames)
			: base(first, last, frames, EasingType.Linear, EasingMode.EaseIn) {
		}
		public ColorAnimator(Color first, Color last, int frames, EasingType easingType)
			: base(first, last, frames, easingType, EasingMode.EaseIn) {
		}
		public ColorAnimator(Color first, Color last, int frames, EasingType easingType, EasingMode easingMode)
			: base(first, last, frames, easingType, easingMode) {
		}

		/// <summary>
		/// Calculate the next frame values
		/// </summary>
		/// <returns>Color</returns>
		public override Color Next() {
			if (NextFrame) {
				double easingValue = EasingValue;
				double da = last.A - first.A;
				double dr = last.R - first.R;
				double dg = last.G - first.G;
				double db = last.B - first.B;
				int a = first.A + (int)(easingValue * da);
				int r = first.R + (int)(easingValue * dr);
				int g = first.G + (int)(easingValue * dg);
				int b = first.B + (int)(easingValue * db);
				current = Color.FromArgb(a,r,g,b);
			}
			return current;
		}
	}

	/// <summary>
	/// Implementation of the IntAnimator
	/// </summary>
	public class IntAnimator : AnimatorBase<int> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(IntAnimator));
		public IntAnimator(int first, int last, int frames)
			: base(first, last, frames, EasingType.Linear, EasingMode.EaseIn) {
		}
		public IntAnimator(int first, int last, int frames, EasingType easingType)
			: base(first, last, frames, easingType, EasingMode.EaseIn) {
		}
		public IntAnimator(int first, int last, int frames, EasingType easingType, EasingMode easingMode)
			: base(first, last, frames, easingType, easingMode) {
		}

		/// <summary>
		/// Calculate the next frame values
		/// </summary>
		/// <returns>int</returns>
		public override int Next() {
			if (NextFrame) {
				double easingValue = EasingValue;
				double delta = last - first;
				current = first + (int)(easingValue * delta);
			}
			return current;
		}
	}

	/// <summary>
	/// Easing logic, to make the animations more "fluent"
	/// </summary>
	public static class Easing {
		// Adapted from http://www.robertpenner.com/easing/penner_chapter7_tweening.pdf

		public static double Ease(double linearStep, double acceleration, EasingType type) {
			double easedStep = acceleration > 0 ? EaseIn(linearStep, type) : acceleration < 0 ? EaseOut(linearStep, type) : (double)linearStep;
			// Lerp:
			return ((easedStep - linearStep) * Math.Abs(acceleration) + linearStep);
		}

		public static double EaseIn(double linearStep, EasingType type) {
			switch (type) {
				case EasingType.Step:
					return linearStep < 0.5 ? 0 : 1;
				case EasingType.Linear:
					return linearStep;
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
					return linearStep;
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
					return linearStep;
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
				return Math.Sin(s * (Math.PI / 2) - (Math.PI / 2)) + 1;
			}
			public static double EaseOut(double s) {
				return Math.Sin(s * (Math.PI / 2));
			}
			public static double EaseInOut(double s) {
				return Math.Sin(s * Math.PI - (Math.PI / 2) + 1) / 2;
			}
		}

		static class Power {
			public static double EaseIn(double s, int power) {
				return Math.Pow(s, power);
			}
			public static double EaseOut(double s, int power) {
				var sign = power % 2 == 0 ? -1 : 1;
				return sign * (Math.Pow(s - 1, power) + sign);
			}
			public static double EaseInOut(double s, int power) {
				s *= 2;
				if (s < 1) {
					return EaseIn(s, power) / 2;
				}
				var sign = power % 2 == 0 ? -1 : 1;
				return (sign / 2.0 * (Math.Pow(s - 2, power) + sign * 2));
			}
		}
	}

	/// <summary>
	/// This defines the way the animation works
	/// </summary>
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
}
