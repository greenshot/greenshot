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
	///     Interface to describe an effect
	/// </summary>
	public interface IEffect
	{
		/// <summary>
		///     Apply this IEffect to the supplied sourceBitmap.
		///     In the process of applying the supplied matrix will be modified to represent the changes.
		/// </summary>
		/// <param name="sourceBitmap">Image to apply the effect to</param>
		/// <param name="matrix">
		///     Matrix with the modifications like rotate, translate etc. this can be used to calculate the new
		///     location of elements on a canvas
		/// </param>
		/// <returns>new image with applied effect</returns>
        IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix);
	}
}