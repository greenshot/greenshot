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
	///     This interface is implemented when there is a alpha-blending possibility
	/// </summary>
	public interface IFastBitmapWithBlend : IFastBitmap
	{
		/// <summary>
		/// Color to blend with
		/// </summary>
		Color BackgroundBlendColor { get; set; }

		/// <summary>
		/// Get the color at location x,y and blend
		/// </summary>
		/// <param name="x">int</param>
		/// <param name="y">int</param>
		/// <returns>Color</returns>
		Color GetBlendedColorAt(int x, int y);
	}
}