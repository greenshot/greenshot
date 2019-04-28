// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;

namespace Greenshot.Gfx.FastBitmap
{
	/// <summary>
	///     This interface can be used for when offsetting is needed
	/// </summary>
	public unsafe interface IFastBitmapWithOffset : IFastBitmap
	{
        /// <summary>
        /// Specify the x offset for the IFastBitmap
        /// </summary>
		new int Left { get; set; }

        /// <summary>
        /// Specify the y offset for the IFastBitmap
        /// </summary>
        new int Top { get; set; }

		/// <summary>
		///     Return true if the coordinates are inside the FastBitmap
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		new bool Contains(int x, int y);

		/// <summary>
		///     Set the color at the specified location, using offsetting so the original coordinates can be used
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">Color color</param>
		new void SetColorAt(int x, int y, ref Color color);

        /// <summary>
        ///     Set the color at the specified location, using offsetting so the original coordinates can be used
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte[] color</param>
        /// <param name="colorIndex">int with index in the byte array</param>
		new void SetColorAt(int x, int y, byte[] color, int colorIndex = 0);

        /// <summary>
        ///     Set the color at the specified location, using offsetting so the original coordinates can be used
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte[] color</param>
        /// <param name="colorIndex">int with offset in the byte pointer</param>
        new void SetColorAt(int x, int y, byte* color, int colorIndex = 0);

        /// <summary>
        ///     Get the color at x,y
        ///     The returned Color object depends on the underlying pixel format
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <returns>Color</returns>
        new Color GetColorAt(int x, int y);

        /// <summary>
        ///     Get the color at x,y, using offsetting so the original coordinates can be used
        ///     The returned byte[] color depends on the underlying pixel format
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte array</param>
        /// <param name="colorIndex">int with offset in the byte array</param>
        new void GetColorAt(int x, int y, byte[] color, int colorIndex = 0);

	    /// <summary>
	    ///     Get the color at x,y, using offsetting so the original coordinates can be used
	    ///     The color is placed to the byte* and depends on the underlying pixel format
	    /// </summary>
	    /// <param name="x">int x</param>
	    /// <param name="y">int y</param>
	    /// <param name="color">byte pojter</param>
	    /// <param name="colorIndex">int with offset in the byte pointer</param>
	    new void GetColorAt(int x, int y, byte* color, int colorIndex = 0);
    }
}