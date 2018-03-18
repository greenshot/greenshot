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

#region Usings

using System;
using System.Drawing;
using Dapplo.Windows.Common.Structs;

#endregion

namespace Greenshot.Addons.Interfaces
{
	/// <summary>
	///     The interface to the Capture object, so Plugins can use it.
	/// </summary>
	public interface ICapture : IDisposable
	{
		// The Capture Details
		ICaptureDetails CaptureDetails { get; set; }

        // The captured Image
	    Bitmap Bitmap { get; set; }

		NativeRect ScreenBounds { get; set; }

		Icon Cursor { get; set; }

		// Boolean to specify if the cursor is available
		bool CursorVisible { get; set; }

	    NativePoint CursorLocation { get; set; }

	    NativePoint Location { get; set; }

		void NullBitmap();

        /// <summary>
        ///     Crops the capture to the specified NativeRect (with Bitmap coordinates!)
        /// </summary>
        /// <param name="cropRectangle">NativeRect with bitmap coordinates</param>
        bool Crop(NativeRect cropRectangle);

		/// <summary>
		///     Apply a translate to the mouse location. e.g. needed for crop
		/// </summary>
		/// <param name="x">x coordinates to move the mouse</param>
		/// <param name="y">y coordinates to move the mouse</param>
		void MoveMouseLocation(int x, int y);
	}
}