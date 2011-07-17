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
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using Greenshot.Plugin.Drawing;

namespace Greenshot.Plugin {
	/// <summary>
	/// Alignment Enums for possitioning
	/// </summary>
	//public enum HorizontalAlignment {LEFT, CENTER, RIGHT};
	public enum VerticalAlignment {TOP, CENTER, BOTTOM};

	/// <summary>
	/// The interface to the Surface object, so Plugins can use it.
	/// </summary>
	public interface ISurface {
		/// <summary>
		/// Get/Set the image to the Surface
		/// get will give the image as is currently visible
		/// set will overwrite both the visible image as the underlying image 
		/// 
		/// important notice:
		/// The setter will clone the passed bitmap and dispose it when the Surface is disposed
		/// This means that the supplied image needs to be disposed by the calling code (if needed!)
		/// </summary>
		Image Image {
			get;
			set;
		}
		
		/// <summary>
		/// Get the current Image from the Editor for Exporting (save/upload etc)
		/// Don't forget to call image.Dispose() when finished!!!
		/// </summary>
		/// <returns>Bitmap</returns>
		Image GetImageForExport();
		
		/// <summary>
		/// Add a TextContainer, at the given location, to the Surface.
		/// The TextContainer will be "re"sized to the text size.
		/// </summary>
		/// <param name="text">String to show</param>
		/// <param name="horizontalAlignment">Left, Center, Right</param>
		/// <param name="verticalAlignment">TOP, CENTER, BOTTOM</param>
		/// <param name="family">FontFamily</param>
		/// <param name="size">Font Size in float</param>
		/// <param name="italic">bool true if italic</param>
		/// <param name="bold">bool true if bold</param>
		/// <param name="shadow">bool true if shadow</param>
		/// <param name="borderSize">size of border (0 for none)</param>
		/// <param name="color">Color of string</param>
		/// <param name="fillColor">Color of background (e.g. Color.Transparent)</param>
		ITextContainer AddTextContainer(string text, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, FontFamily family, float size, bool italic, bool bold, bool shadow, int borderSize, Color color, Color fillColor);

		IBitmapContainer AddBitmapContainer(Bitmap bitmap, int x, int y);
		ICursorContainer AddCursorContainer(Cursor cursor, int x, int y);
		IIconContainer AddIconContainer(Icon icon, int x, int y);
		IMetafileContainer AddMetafileContainer(Metafile metafile, int x, int y);
		IBitmapContainer AddBitmapContainer(string filename, int x, int y);
		ICursorContainer AddCursorContainer(string filename, int x, int y);
		IIconContainer AddIconContainer(string filename, int x, int y);
		IMetafileContainer AddMetafileContainer(string filename, int x, int y);

		bool HasSelectedElements();
		void RemoveSelectedElements();
		void CutSelectedElements();
		void CopySelectedElements();
		void PasteElementFromClipboard();
		void DuplicateSelectedElements();
		void DeselectElement(IDrawableContainer container);
		void DeselectAllElements();
		void SelectElement(IDrawableContainer container);
		void Invalidate(Rectangle rectangleToInvalidate);
		void Invalidate();
		bool Modified {
			get;
			set;
		}
	}
}
