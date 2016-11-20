//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using Dapplo.Log;

#endregion

namespace Greenshot.Addon.Core
{
	/// <summary>
	///     Helper interface for passing base type
	/// </summary>
	public interface IAnimator
	{
		/// <summary>
		///     Current frame number
		/// </summary>
		int CurrentFrameNr { get; }

		/// <summary>
		///     The amount of frames
		/// </summary>
		int Frames { get; }

		/// <summary>
		///     Is there a next frame?
		/// </summary>
		bool hasNext { get; }
	}

	/// <summary>
	///     This class is used to store a animation leg
	/// </summary>
	internal class AnimationLeg<T>
	{
		public T Destination { get; set; }

		public EasingMode EasingMode { get; set; }

		public EasingType EasingType { get; set; }

		public int Frames { get; set; }
	}

	/// <summary>
	///     Base class for the animation logic, this only implements Properties and a constructor
	/// </summary>
	/// <typeparam name="T">Type for the animation, like Point/Rectangle/Size</typeparam>
	public abstract class AnimatorBase<T> : IAnimator
	{
		private readonly Queue<AnimationLeg<T>> _queue = new Queue<AnimationLeg<T>>();

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="first"></param>
		/// <param name="last"></param>
		/// <param name="frames"></param>
		/// <param name="easingType"></param>
		/// <param name="easingMode"></param>
		public AnimatorBase(T first, T last, int frames, EasingType easingType, EasingMode easingMode)
		{
			First = first;
			Last = last;
			Frames = frames;
			Current = first;
			EasingType = easingType;
			EasingMode = easingMode;
		}

		/// <summary>
		///     Get the current (previous) frame object
		/// </summary>
		public virtual T Current
		{
			get; protected set;
		}

		/// <summary>
		///     The EasingMode to use for the animation
		/// </summary>
		public EasingMode EasingMode { get; set; }

		/// <summary>
		///     The EasingType to use for the animation
		/// </summary>
		public EasingType EasingType { get; set; }

		/// <summary>
		///     Get the easing value, which is from 0-1 and depends on the frame
		/// </summary>
		protected double EasingValue
		{
			get
			{
				switch (EasingMode)
				{
					case EasingMode.EaseOut:
						return Easing.EaseOut(CurrentFrameNr/(double)Frames, EasingType);
					case EasingMode.EaseInOut:
						return Easing.EaseInOut(CurrentFrameNr/(double)Frames, EasingType);
					case EasingMode.EaseIn:
					default:
						return Easing.EaseIn(CurrentFrameNr/(double)Frames, EasingType);
				}
			}
		}

		/// <summary>
		///     Final animation value, this is including the legs
		/// </summary>
		public T Final
		{
			get
			{
				if (_queue.Count == 0)
				{
					return Last;
				}
				return _queue.ToArray()[_queue.Count - 1].Destination;
			}
		}

		/// <summary>
		///     First animation value
		/// </summary>
		public T First { get; protected set; }

		/// <summary>
		///     Last animation value, of this "leg"
		/// </summary>
		public T Last { get; protected set; }

		/// <summary>
		///     Returns if there are any frame left, and if this is the case than the frame is increased.
		/// </summary>
		public virtual bool NextFrame
		{
			get
			{
				if (CurrentFrameNr < Frames)
				{
					CurrentFrameNr++;
					return true;
				}
				if (_queue.Count > 0)
				{
					First = Current;
					CurrentFrameNr = 0;
					AnimationLeg<T> nextLeg = _queue.Dequeue();
					Last = nextLeg.Destination;
					Frames = nextLeg.Frames;
					EasingType = nextLeg.EasingType;
					EasingMode = nextLeg.EasingMode;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		///     The amount of frames
		/// </summary>
		public int Frames { get; protected set; }

		/// <summary>
		///     Current frame number
		/// </summary>
		public int CurrentFrameNr { get; protected set; }

		/// <summary>
		///     Are there more frames to animate?
		/// </summary>
		public virtual bool hasNext
		{
			get
			{
				if (CurrentFrameNr < Frames)
				{
					return true;
				}
				return _queue.Count > 0;
			}
		}

		/// <summary>
		///     This restarts the current animation and changes the last frame
		/// </summary>
		/// <param name="newDestination"></param>
		public void ChangeDestination(T newDestination)
		{
			ChangeDestination(newDestination, Frames);
		}

		/// <summary>
		///     This restarts the current animation and changes the last frame
		/// </summary>
		/// <param name="newDestination"></param>
		/// <param name="frames"></param>
		public void ChangeDestination(T newDestination, int frames)
		{
			_queue.Clear();
			First = Current;
			CurrentFrameNr = 0;
			Frames = frames;
			Last = newDestination;
		}

		/// <summary>
		///     Get the next animation frame value object
		/// </summary>
		/// <returns></returns>
		public abstract T Next();

		/// <summary>
		///     Queue the destination, it will be used to continue at the last frame
		///     All values will stay the same
		/// </summary>
		/// <param name="queuedDestination"></param>
		public void QueueDestinationLeg(T queuedDestination)
		{
			QueueDestinationLeg(queuedDestination, Frames, EasingType, EasingMode);
		}

		/// <summary>
		///     Queue the destination, it will be used to continue at the last frame
		/// </summary>
		/// <param name="queuedDestination"></param>
		/// <param name="frames"></param>
		public void QueueDestinationLeg(T queuedDestination, int frames)
		{
			QueueDestinationLeg(queuedDestination, frames, EasingType, EasingMode);
		}

		/// <summary>
		///     Queue the destination, it will be used to continue at the last frame
		/// </summary>
		/// <param name="queuedDestination"></param>
		/// <param name="frames"></param>
		/// <param name="easingType">EasingType</param>
		public void QueueDestinationLeg(T queuedDestination, int frames, EasingType easingType)
		{
			QueueDestinationLeg(queuedDestination, frames, easingType, EasingMode);
		}

		/// <summary>
		///     Queue the destination, it will be used to continue at the last frame
		/// </summary>
		/// <param name="queuedDestination"></param>
		/// <param name="frames"></param>
		/// <param name="easingType"></param>
		/// <param name="easingMode"></param>
		public void QueueDestinationLeg(T queuedDestination, int frames, EasingType easingType, EasingMode easingMode)
		{
			AnimationLeg<T> leg = new AnimationLeg<T>();
			leg.Destination = queuedDestination;
			leg.Frames = frames;
			leg.EasingType = easingType;
			leg.EasingMode = easingMode;
			_queue.Enqueue(leg);
		}
	}

	/// <summary>
	///     Implementation of the RectangleAnimator
	/// </summary>
	public class RectangleAnimator : AnimatorBase<Rectangle>
	{
		private static readonly LogSource Log = new LogSource();

		public RectangleAnimator(Rectangle first, Rectangle last, int frames) : base(first, last, frames, EasingType.Linear, EasingMode.EaseIn)
		{
		}

		public RectangleAnimator(Rectangle first, Rectangle last, int frames, EasingType easingType) : base(first, last, frames, easingType, EasingMode.EaseIn)
		{
		}

		public RectangleAnimator(Rectangle first, Rectangle last, int frames, EasingType easingType, EasingMode easingMode) : base(first, last, frames, easingType, easingMode)
		{
		}

		/// <summary>
		///     Calculate the next frame object
		/// </summary>
		/// <returns>Rectangle</returns>
		public override Rectangle Next()
		{
			if (NextFrame)
			{
				double easingValue = EasingValue;
				double dx = Last.X - First.X;
				double dy = Last.Y - First.Y;

				int x = First.X + (int) (easingValue*dx);
				int y = First.Y + (int) (easingValue*dy);
				double dw = Last.Width - First.Width;
				double dh = Last.Height - First.Height;
				int width = First.Width + (int) (easingValue*dw);
				int height = First.Height + (int) (easingValue*dh);
				Current = new Rectangle(x, y, width, height);
			}
			return Current;
		}
	}

	/// <summary>
	///     Implementation of the ColorAnimator
	/// </summary>
	public class ColorAnimator : AnimatorBase<Color>
	{
		private static readonly LogSource Log = new LogSource();

		public ColorAnimator(Color first, Color last, int frames) : base(first, last, frames, EasingType.Linear, EasingMode.EaseIn)
		{
		}

		public ColorAnimator(Color first, Color last, int frames, EasingType easingType) : base(first, last, frames, easingType, EasingMode.EaseIn)
		{
		}

		public ColorAnimator(Color first, Color last, int frames, EasingType easingType, EasingMode easingMode) : base(first, last, frames, easingType, easingMode)
		{
		}

		/// <summary>
		///     Calculate the next frame values
		/// </summary>
		/// <returns>Color</returns>
		public override Color Next()
		{
			if (NextFrame)
			{
				double easingValue = EasingValue;
				double da = Last.A - First.A;
				double dr = Last.R - First.R;
				double dg = Last.G - First.G;
				double db = Last.B - First.B;
				int a = First.A + (int) (easingValue*da);
				int r = First.R + (int) (easingValue*dr);
				int g = First.G + (int) (easingValue*dg);
				int b = First.B + (int) (easingValue*db);
				Current = Color.FromArgb(a, r, g, b);
			}
			return Current;
		}
	}

	/// <summary>
	///     Easing logic, to make the animations more "fluent"
	/// </summary>
	public static class Easing
	{
		// Adapted from http://www.robertpenner.com/easing/penner_chapter7_tweening.pdf

		public static double Ease(double linearStep, double acceleration, EasingType type)
		{
			double easedStep = acceleration > 0 ? EaseIn(linearStep, type) : acceleration < 0 ? EaseOut(linearStep, type) : linearStep;
			// Lerp:
			return (easedStep - linearStep)*Math.Abs(acceleration) + linearStep;
		}

		public static double EaseIn(double linearStep, EasingType type)
		{
			switch (type)
			{
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

		public static double EaseInOut(double linearStep, EasingType easeInType, EasingType easeOutType)
		{
			return linearStep < 0.5 ? EaseInOut(linearStep, easeInType) : EaseInOut(linearStep, easeOutType);
		}

		public static double EaseInOut(double linearStep, EasingType type)
		{
			switch (type)
			{
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

		public static double EaseOut(double linearStep, EasingType type)
		{
			switch (type)
			{
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

		private static class Sine
		{
			public static double EaseIn(double s)
			{
				return Math.Sin(s*(Math.PI/2) - Math.PI/2) + 1;
			}

			public static double EaseInOut(double s)
			{
				return Math.Sin(s*Math.PI - Math.PI/2 + 1)/2;
			}

			public static double EaseOut(double s)
			{
				return Math.Sin(s*(Math.PI/2));
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
					return EaseIn(s, power)/2;
				}
				var sign = power%2 == 0 ? -1 : 1;
				return sign/2.0*(Math.Pow(s - 2, power) + sign*2);
			}

			public static double EaseOut(double s, int power)
			{
				var sign = power%2 == 0 ? -1 : 1;
				return sign*(Math.Pow(s - 1, power) + sign);
			}
		}
	}

	/// <summary>
	///     This defines the way the animation works
	/// </summary>
	public enum EasingType
	{
		Step,
		Linear,
		Sine,
		Quadratic,
		Cubic,
		Quartic,
		Quintic
	}

	public enum EasingMode
	{
		EaseIn,
		EaseOut,
		EaseInOut
	}
}