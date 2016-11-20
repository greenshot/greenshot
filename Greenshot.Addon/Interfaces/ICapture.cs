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
using System.Drawing;
using System.IO;
using System.Windows.Forms;

#endregion

namespace Greenshot.Addon.Interfaces
{
	/// <summary>
	///     The interface to the Capture object, so Plugins can use it.
	/// </summary>
	public interface ICapture : IDisposable
	{
		// The Capture Details
		ICaptureDetails CaptureDetails { get; set; }

		Cursor Cursor { get; set; }

		Point CursorLocation { get; set; }

		/// <summary>
		///     Boolean to specify if the cursor is available
		/// </summary>
		bool CursorVisible { get; set; }

		// The captured Image
		Image Image { get; set; }

		Point Location { get; set; }

		/// <summary>
		///     Boolean to specify if the capture has modifications (reset on export/save)
		/// </summary>
		bool Modified { get; set; }

		Rectangle ScreenBounds { get; set; }

		/// <summary>
		///     Crops the capture to the specified rectangle (with Bitmap coordinates!)
		/// </summary>
		/// <param name="cropRectangle">Rectangle with bitmap coordinates</param>
		bool ApplyCrop(Rectangle cropRectangle);

		/// <summary>
		///     Get the current Image from the Editor for Exporting (save/upload etc)
		///     Don't forget to call image.Dispose() when finished!!!
		/// </summary>
		/// <returns>Bitmap</returns>
		Image GetImageForExport();

		void LoadElementsFromStream(Stream stream);

		/// <summary>
		///     Apply a translate to the mouse location. e.g. needed for crop
		/// </summary>
		/// <param name="x">x coordinates to move the mouse</param>
		/// <param name="y">y coordinates to move the mouse</param>
		void MoveMouseLocation(int x, int y);

		void NullImage();

		long SaveElementsToStream(Stream stream);
	}
}