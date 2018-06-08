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

using System.Collections.Generic;

#endregion

namespace Greenshot.Addons.Animation
{
    /// <summary>
    ///     Base class for the animation logic, this only implements Properties and a constructor
    /// </summary>
    /// <typeparam name="T">Type for the animation, like NativePoint/NativeRectNative/NativeSize</typeparam>
    public abstract class AnimatorBase<T> : IAnimator
	{
		private readonly Queue<AnimationLeg<T>> _queue = new Queue<AnimationLeg<T>>();

		/// <summary>
		///     Constructor
		/// </summary>
		/// <param name="first">T</param>
		/// <param name="last">T</param>
		/// <param name="frames">int</param>
		/// <param name="easingType">EasingTypes</param>
		/// <param name="easingMode">EasingModes</param>
		public AnimatorBase(T first, T last, int frames, EasingTypes easingType = EasingTypes.Linear, EasingModes easingMode = EasingModes.EaseIn)
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
		public T Current { get; set; }

		/// <summary>
		///     The EasingModes to use for the animation
		/// </summary>
		public EasingModes EasingMode { get; set; }

		/// <summary>
		///     The EasingTypes to use for the animation
		/// </summary>
		public EasingTypes EasingType { get; set; }

		/// <summary>
		///     Get the easing value, which is from 0-1 and depends on the frame
		/// </summary>
		protected double EasingValue
		{
			get
			{
				switch (EasingMode)
				{
					case EasingModes.EaseOut:
						return Easing.EaseOut(CurrentFrameNr / (double) Frames, EasingType);
					case EasingModes.EaseInOut:
						return Easing.EaseInOut(CurrentFrameNr / (double) Frames, EasingType);
					default:
						return Easing.EaseIn(CurrentFrameNr / (double) Frames, EasingType);
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
		public T First { get; private set; }

		/// <summary>
		///     Last animation value, of this "leg"
		/// </summary>
		public T Last { get; private set; }

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

				if (_queue.Count <= 0)
				{
					return false;
				}
				First = Current;
				CurrentFrameNr = 0;
				var nextLeg = _queue.Dequeue();
				Last = nextLeg.Destination;
				Frames = nextLeg.Frames;
				EasingType = nextLeg.EasingType;
				EasingMode = nextLeg.EasingMode;
				return true;
			}
		}

		/// <summary>
		///     The amount of frames
		/// </summary>
		public int Frames { get; private set; }

		/// <summary>
		///     Current frame number
		/// </summary>
		public int CurrentFrameNr { get; private set; }

		/// <summary>
		///     Are there more frames to animate?
		/// </summary>
		public virtual bool HasNext
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
		/// </summary>
		/// <param name="queuedDestination">T</param>
		/// <param name="frames">int</param>
		/// <param name="easingType">EasingTypes</param>
		/// <param name="easingMode">EasingModes</param>
		public void QueueDestinationLeg(T queuedDestination, int? frames = null, EasingTypes? easingType = null, EasingModes? easingMode = null)
		{
			var leg = new AnimationLeg<T>
			{
				Destination = queuedDestination,
				Frames = frames ?? Frames,
				EasingType = easingType ?? EasingType,
				EasingMode = easingMode ?? EasingMode
			};
			_queue.Enqueue(leg);
		}
	}
}