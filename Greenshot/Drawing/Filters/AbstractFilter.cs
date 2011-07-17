/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Serialization;

using Greenshot.Drawing;
using Greenshot.Drawing.Fields;
using Greenshot.Plugin.Drawing;

/// <summary>
/// Graphical filter which can be added to DrawableContainer.
/// Subclasses should fulfill INotifyPropertyChanged contract, i.e. call
/// OnPropertyChanged whenever a public property has been changed.
/// </summary>
namespace Greenshot.Drawing.Filters {
	[Serializable()]
	public abstract class AbstractFilter : AbstractFieldHolder, IFilter {
		
		[NonSerialized]
		private PropertyChangedEventHandler propertyChanged;
		public event PropertyChangedEventHandler PropertyChanged {
			add { propertyChanged += value; }
			remove{ propertyChanged -= value; }
		}
		
		private bool invert = false;
		public bool Invert {
			get { return invert; }
			set { invert=value; OnPropertyChanged("Invert"); }
		}
		protected BitmapBuffer bbb;
		protected Rectangle applyRect;
		protected DrawableContainer parent;
		public DrawableContainer Parent {
			get {return parent;}
			set {parent = value;}
		}
		
		public AbstractFilter(DrawableContainer parent) {
			this.parent = parent;
		}
		
		public DrawableContainer GetParent() {
			return parent;
		}

		/**
		 * This method fixes the problem that we can't apply a filter outside the target bitmap,
		 * therefor the filtered-bitmap will be shifted if we try to draw it outside the target bitmap.
		 * It will also account for the Invert flag.
		 */
		protected Rectangle IntersectRectangle(Size applySize, Rectangle rect) {
			Rectangle myRect;
			if (Invert) {
				myRect = new Rectangle(0, 0, applySize.Width, applySize.Height);
			} else {
				Rectangle applyRect = new Rectangle(0,0, applySize.Width, applySize.Height);
				myRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
				myRect.Intersect(applyRect);
			}
			return myRect;
		}

		public virtual void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle rect, RenderMode renderMode) {
			applyRect = IntersectRectangle(applyBitmap.Size, rect);

			if (applyRect.Width == 0 || applyRect.Height == 0) {
				// nothing to do
				return;
			}

			bbb = new BitmapBuffer(applyBitmap, applyRect);
			try {
				bbb.Lock();
				for(int y=0;y<bbb.Height; y++) {
					for(int x=0;x<bbb.Width; x++) {
						if(parent.Contains(applyRect.Left+x, applyRect.Top+y) ^ Invert) {
							IteratePixel(x, y);
						} 
					}
				}
			} finally {
				bbb.DrawTo(graphics, applyRect.Location);
				bbb.Dispose();
				bbb = null;
			}
		}
		
		protected virtual void IteratePixel(int x, int y) {}
		
		protected void OnPropertyChanged(string propertyName) {
			if(propertyChanged != null) propertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		
	}
}
