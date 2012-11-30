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

		public AnimatorBase(T first, T last, int frames) {
			this.first = first;
			this.last = last;
			this.frames = frames;
			LOG.DebugFormat("First {0} Last {1} frames {2}", first, last, frames);
			current = first;
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

		public RectangleAnimator(Rectangle first, Rectangle last, int frames) : base(first, last, frames) {
		}

		public override Rectangle Next() {
			if (hasNext) {
				currentFrame++;

				double dx = (last.X - first.X) / frames;
				double dy = (last.Y - first.Y) / frames;
				double dw = (last.Width - first.Width) / frames;
				double dh = (last.Height - first.Height) / frames;

				LOG.DebugFormat("dx {0}, dy {1}, dw {2}, dh {3}", dx, dy, dw, dh);
				int x = first.X + (int)(currentFrame * dx);
				int y = first.Y + (int)(currentFrame * dy);
				int width = first.Width + (int)(currentFrame * dw);
				int height = first.Height + (int)(currentFrame * dh);
				current = new Rectangle(x, y, width, height);
				LOG.DebugFormat("frame {0} : {1}", currentFrame, current);
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
			: base(first, last, frames) {
		}
		public override Point Next() {
			if (hasNext) {
				currentFrame++;

				double dx = (last.X - first.X) / frames;
				double dy = (last.Y - first.Y) / frames;
				
				LOG.DebugFormat("dx {0}, dy {1}", dx ,dy);
				int x = first.X + (int)(currentFrame * dx);
				int y = first.Y + (int)(currentFrame * dy);
				current = new Point(x, y);
				LOG.DebugFormat("frame {0} : {1}", currentFrame, current);
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
			: base(first, last, frames) {
		}

		public override Size Next() {
			if (hasNext) {
				currentFrame++;

				double dw = (last.Width - first.Width) / frames;
				double dh = (last.Height - first.Height) / frames;

				LOG.DebugFormat("dw {0}, dh {1}", dw, dh);
				int width = first.Width + (int)(currentFrame * dw);
				int height = first.Height + (int)(currentFrame * dh);
				current = new Size(width, height);
				LOG.DebugFormat("frame {0} : {1}", currentFrame, current);
			}
			return current;
		}
	}

	
	/// <summary>
	/// Implementation of the FlexibleSizeAnimator
	/// </summary>
	public class FlexibleAnimator<T> : AnimatorBase<T> {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FlexibleAnimator<T>));
		public delegate bool HasNextValue(T current);
		public delegate T NextValue(T current);
		private HasNextValue hasNextValue;
		private NextValue nextValue;

		public FlexibleAnimator(T first, HasNextValue hasNextDelegate, NextValue nextValueDelegate)
			: base(first, default(T) , 0) {
			this.hasNextValue = hasNextDelegate;
			this.nextValue = nextValueDelegate;
		}
				
		public override bool hasNext {
			get {
				return hasNextValue(current);
			}
		}
		
		public override T Next() {
			if (hasNext) {
				current = nextValue(current);
			}
			return current;
		}
	}
}
