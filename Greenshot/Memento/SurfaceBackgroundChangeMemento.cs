/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Drawing;
using Greenshot.Configuration;
using System.Drawing.Drawing2D;

namespace Greenshot.Memento {
	/// <summary>
	/// The SurfaceCropMemento makes it possible to undo-redo an surface crop
	/// </summary>
	public class SurfaceBackgroundChangeMemento : IMemento {
		private Image image;
		private Surface surface;
		private Matrix matrix;
		
		public SurfaceBackgroundChangeMemento(Surface surface, Matrix matrix) {
			this.surface = surface;
			image = surface.Image;
			this.matrix = (Matrix)matrix.Clone();
			// Make sure the reverse is applied
			this.matrix.Invert();
		}
		
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (image != null) {
					image.Dispose();
					image = null;
				}
				surface = null;
			}
		}

		public bool Merge(IMemento otherMemento) {
			return false;
		}
		
		public LangKey ActionLanguageKey {
			get {
				//return LangKey.editor_crop;
				return LangKey.none;
			}
		}

		public IMemento Restore() {
			SurfaceBackgroundChangeMemento oldState = new SurfaceBackgroundChangeMemento(surface, matrix);

			surface.UndoBackgroundChange(image, matrix);
			surface.Invalidate();
			return oldState;
		}
	}
}
