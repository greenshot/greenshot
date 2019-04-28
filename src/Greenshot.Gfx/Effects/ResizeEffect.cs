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

using System.Drawing.Drawing2D;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	/// This effect resizes the bitmap
	/// </summary>
	public class ResizeEffect : IEffect
	{
        /// <summary>
        /// The constructor which takes the new width and height and if the aspect ratio should be maintained
        /// </summary>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <param name="maintainAspectRatio">bool</param>
		public ResizeEffect(int width, int height, bool maintainAspectRatio)
		{
			Width = width;
			Height = height;
			MaintainAspectRatio = maintainAspectRatio;
		}

        /// <summary>
        /// The new width
        /// </summary>
		public int Width { get; set; }

        /// <summary>
        /// The new height
        /// </summary>
		public int Height { get; set; }

        /// <summary>
        /// Do we need to maintain the aspect ration
        /// </summary>
		public bool MaintainAspectRatio { get; set; }

        /// <inheritdoc />
		public IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			return sourceBitmap.Resize(MaintainAspectRatio, Width, Height, matrix);
		}
	}
}