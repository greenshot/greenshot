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

using System;
using System.Drawing;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.FastBitmap
{
	/// <summary>
	///     The interface for the FastBitmap
	/// </summary>
	public unsafe interface IFastBitmap : IDisposable
	{
		/// <summary>
		///     Size of the underlying image
		/// </summary>
		Size Size { get; }

		/// <summary>
		///     Height of the image area that this fastbitmap covers
		/// </summary>
		int Height { get; }

		/// <summary>
		///     Width of the image area that this fastbitmap covers
		/// </summary>
		int Width { get; }

		/// <summary>
		///     Top of the image area that this fastbitmap covers
		/// </summary>
		int Top { get; }

		/// <summary>
		///     Left of the image area that this fastbitmap covers
		/// </summary>
		int Left { get; }

		/// <summary>
		///     Right of the image area that this fastbitmap covers
		/// </summary>
		int Right { get; }

		/// <summary>
		///     Bottom of the image area that this fastbitmap covers
		/// </summary>
		int Bottom { get; }

		/// <summary>
		///     Does the underlying image need to be disposed
		/// </summary>
		bool NeedsDispose { get; set; }

		/// <summary>
		///     Returns if this FastBitmap has an alpha channel
		/// </summary>
		bool HasAlphaChannel { get; }

        /// <summary>
        /// The number of bytes per pixel
        /// </summary>
        int BytesPerPixel { get; }

	    /// <summary>
	    /// Calculate the the hash-code for a horizontal line
	    /// </summary>
	    /// <param name="y">int with y coordinate</param>
	    /// <param name="right">optional x starting coordinate of the hash calculation</param>
	    /// <param name="left">optional x ending coordinate of the hash calculation</param>
	    /// <returns>uint with the hash</returns>
        uint HorizontalHash(int y, int? right = null, int? left = null);

        /// <summary>
        ///     Get the color at x,y
        ///     The returned Color object depends on the underlying pixel format
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <returns>Color</returns>
        Color GetColorAt(int x, int y);

		/// <summary>
		///     Set the color at the specified location
		/// </summary>
		/// <param name="x">int x</param>
		/// <param name="y">int y</param>
		/// <param name="color">Color</param>
		void SetColorAt(int x, int y, ref Color color);

	    /// <summary>
	    ///     Get the color at x,y
	    ///     The returned byte[] color depends on the underlying pixel format
	    /// </summary>
	    /// <param name="x">int x</param>
	    /// <param name="y">int y</param>
	    /// <param name="color">byte array to place the color in</param>
	    /// <param name="colorIndex">int with offset in the byte array</param>
	    void GetColorAt(int x, int y, byte[] color, int colorIndex = 0);

	    /// <summary>
	    ///     Get the color at x,y
	    ///     The returned byte[] color depends on the underlying pixel format
	    /// </summary>
	    /// <param name="x">int x</param>
	    /// <param name="y">int y</param>
	    /// <param name="color">byte point to place the color in</param>
	    /// <param name="colorIndex">int with offset in the byte point</param>
	    void GetColorAt(int x, int y, byte* color, int colorIndex = 0);

        /// <summary>
        ///     Set the color at the specified location
        /// </summary>
        /// <param name="x">int x</param>
        /// <param name="y">int y</param>
        /// <param name="color">byte[] color</param>
        /// <param name="colorIndex">int with offset in the byte array</param>
	    void SetColorAt(int x, int y, byte[] color, int colorIndex = 0);

        /// <summary>
	    ///     Set the color at the specified location
	    /// </summary>
	    /// <param name="x">int x</param>
	    /// <param name="y">int y</param>
	    /// <param name="color">byte[] color</param>
	    /// <param name="colorIndex">int with offset in the byte array</param>
	    void SetColorAt(int x, int y, byte* color, int colorIndex = 0);

		/// <summary>
		///     Lock the bitmap
		/// </summary>
		void Lock();

		/// <summary>
		///     Unlock the bitmap
		/// </summary>
		void Unlock();

		/// <summary>
		///     Unlock the bitmap and get the underlying bitmap in one call
		/// </summary>
		/// <returns></returns>
        IBitmapWithNativeSupport UnlockAndReturnBitmap();

		/// <summary>
		///     Draw the stored bitmap to the destionation bitmap at the supplied point
		/// </summary>
		/// <param name="graphics">Graphics</param>
		/// <param name="destination">NativePoint with location</param>
		void DrawTo(Graphics graphics, NativePoint destination);

		/// <summary>
		///     Draw the stored Bitmap on the Destination bitmap with the specified rectangle
		///     Be aware that the stored bitmap will be resized to the specified rectangle!!
		/// </summary>
		/// <param name="graphics">Graphics</param>
		/// <param name="destinationRect">NativeRect with destination</param>
		void DrawTo(Graphics graphics, NativeRect destinationRect);

		/// <summary>
		///     Return true if the coordinates are inside the FastBitmap
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		bool Contains(int x, int y);

		/// <summary>
		///     Set the bitmap resolution
		/// </summary>
		/// <param name="horizontal"></param>
		/// <param name="vertical"></param>
		void SetResolution(float horizontal, float vertical);
	}
}