#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Drawing;

#endregion

namespace GreenshotPlugin.Interfaces
{
	/// <summary>
	///     The interface to the Capture object, so Plugins can use it.
	/// </summary>
	public interface ICapture : IDisposable
	{
		// The Capture Details
		ICaptureDetails CaptureDetails { get; set; }

		// The captured Image
		Image Image { get; set; }

		Rectangle ScreenBounds { get; set; }

		Icon Cursor { get; set; }

		// Boolean to specify if the cursor is available
		bool CursorVisible { get; set; }

		Point CursorLocation { get; set; }

		Point Location { get; set; }

		void NullImage();

		/// <summary>
		///     Crops the capture to the specified rectangle (with Bitmap coordinates!)
		/// </summary>
		/// <param name="cropRectangle">Rectangle with bitmap coordinates</param>
		bool Crop(Rectangle cropRectangle);

		/// <summary>
		///     Apply a translate to the mouse location. e.g. needed for crop
		/// </summary>
		/// <param name="x">x coordinates to move the mouse</param>
		/// <param name="y">y coordinates to move the mouse</param>
		void MoveMouseLocation(int x, int y);

		///// </summary>
		///// Add a new element to the capture

		///// <summary>
		//void MoveElements(int x, int y);
		///// <param name="y">y coordinates to move the elements</param>
		///// <param name="x">x coordinates to move the elements</param>
		///// </summary>
		///// Apply a translate to the elements e.g. needed for crop
		///// <summary>

		// / TODO: Enable when the elements are usable again.
		///// <param name="element">Rectangle</param>
		//void AddElement(ICaptureElement element);

		///// <summary>
		///// Returns a list of rectangles which represent objects that are "on" the capture
		///// </summary>
		//List<ICaptureElement> Elements {
		//    get;
		//    set;
		//}
	}
}