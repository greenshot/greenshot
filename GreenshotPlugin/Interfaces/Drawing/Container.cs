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
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Greenshot.Plugin.Drawing {
	public enum RenderMode {EDIT, EXPORT};
	public enum EditStatus {UNDRAWN, DRAWING, MOVING, RESIZING, IDLE};

	public interface IDrawableContainer {
		ISurface Parent {
			get;
		}
		bool Selected {
			get;
			set;
		}
		
		int Left {
			get;
			set;
		}
		
		int Top {
			get;
			set;
		}
		
		int Width {
			get;
			set;
		}
		
		int Height {
			get;
			set;
		}
		
		Point Location {
			get;
		}

		Size Size {
			get;
		}
		
		Rectangle Bounds {
			get;
		}

		Rectangle DrawingBounds {
			get;
		}
		
		bool hasFilters {
			get;
		}

		void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment);
		void Invalidate();
		void Dispose();
	}

	public interface ITextContainer: IDrawableContainer {
		string Text {
			get;
			set;
		}
		void FitToText();
	}

	public interface IBitmapContainer: IDrawableContainer {
		Bitmap Bitmap {
			get;
			set;
		}
		void Load(string filename);
	}
	public interface ICursorContainer: IDrawableContainer {
		Cursor Cursor {
			get;
			set;
		}
		void Load(string filename);
	}
	public interface IIconContainer: IDrawableContainer {
		Icon Icon {
			get;
			set;
		}
		void Load(string filename);
	}

	public interface IMetafileContainer: IDrawableContainer {
		Metafile Metafile {
			get;
			set;
		}
		void Load(string filename);
	}
}
