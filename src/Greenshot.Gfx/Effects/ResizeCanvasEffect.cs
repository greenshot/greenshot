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
using System.Drawing.Drawing2D;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	/// This effect will enlange the bitmap with the specified pixels to the left, right, top, bottom
	/// </summary>
	public class ResizeCanvasEffect : IEffect
	{
        /// <summary>
        /// The constructor which takes the sizes to grow the canvas
        /// </summary>
        /// <param name="left">int</param>
        /// <param name="right">int</param>
        /// <param name="top">int</param>
        /// <param name="bottom">int</param>
		public ResizeCanvasEffect(int left, int right, int top, int bottom)
		{
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
			BackgroundColor = Color.Empty; // Uses the default background color depending on the format
		}

        /// <summary>
        /// The pixels which need to be added left
        /// </summary>
		public int Left { get; set; }

        /// <summary>
        /// The pixels which need to be added right
        /// </summary>
		public int Right { get; set; }

        /// <summary>
        /// The pixels which need to be added top
        /// </summary>        
        public int Top { get; set; }

        /// <summary>
        /// The pixels which need to be added bottom
        /// </summary>
		public int Bottom { get; set; }

        /// <summary>
        /// The color of the new pixels
        /// </summary>
		public Color BackgroundColor { get; set; }

        /// <inheritdoc />
		public IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			return BitmapHelper.ResizeCanvas(sourceBitmap, BackgroundColor, Left, Right, Top, Bottom, matrix);
		}
	}
}