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
using System.Drawing.Imaging;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	///     GrayscaleEffect
	/// </summary>
	public class GrayscaleEffect : IEffect
	{
        /// <summary>
        /// Apply this effect to the specified bitmap
        /// </summary>
        /// <param name="sourceBitmap">Bitmap</param>
        /// <param name="matrix">Matrix</param>
        /// <returns>Bitmap</returns>
		public IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			return CreateGrayscale(sourceBitmap);
		}

	    /// <summary>
	    ///     Create a new bitmap where the sourceBitmap is in grayscale
	    /// </summary>
	    /// <param name="sourceBitmap">Original bitmap</param>
	    /// <returns>Bitmap with grayscale</returns>
	    public static IBitmapWithNativeSupport CreateGrayscale(IBitmapWithNativeSupport sourceBitmap)
	    {
	        var clone = sourceBitmap.CloneBitmap();
	        var grayscaleMatrix = new ColorMatrix(new[]
	        {
	            new[] {.3f, .3f, .3f, 0, 0},
	            new[] {.59f, .59f, .59f, 0, 0},
	            new[] {.11f, .11f, .11f, 0, 0},
	            new float[] {0, 0, 0, 1, 0},
	            new float[] {0, 0, 0, 0, 1}
	        });
	        clone.ApplyColorMatrix(grayscaleMatrix);
	        return clone;
	    }

    }
}