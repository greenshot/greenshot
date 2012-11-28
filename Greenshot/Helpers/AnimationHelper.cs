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
	/// Description of AnimationHelper.
	/// </summary>
	public class AnimationHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AnimationHelper));
		private Rectangle first;
		private Rectangle last;
		private Rectangle current;
		private double frames;
		private double currentFrame = 0;
		
		public AnimationHelper(Rectangle first, Rectangle last, int frames) {
			this.first = first;
			this.last = last;
			this.frames = frames;
			LOG.DebugFormat("First {0} Last {1} frames {2}", first, last, frames);
			current = first;
		}
		
		public Rectangle Current {
			get {
				return current;
			}
		}
		
		public bool hasNext {
			get {
				return currentFrame < frames;
			}
		}

		public Rectangle Next() {
			if (hasNext) {
				currentFrame++;
				
				double dx = (last.X - first.X) / frames;
				double dy = (last.Y - first.Y) / frames;
				double dw = (last.Width - first.Width) / frames;
				double dh = (last.Height - first.Height) / frames;
				
				LOG.DebugFormat("dx {0}, dy {1}, dw {2}, dh {3}", dx ,dy, dw, dh);
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
}
