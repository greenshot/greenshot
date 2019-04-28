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
using System.Threading.Tasks;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	///     MonochromeEffect
	/// </summary>
	public class MonochromeEffect : IEffect
	{
		private readonly byte _threshold;

        /// <param name="threshold">Threshold for monochrome filter (0 - 255), lower value means less black</param>
        public MonochromeEffect(byte threshold)
		{
			_threshold = threshold;
		}

        /// <inheritdoc />
        public IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			return CreateMonochrome(sourceBitmap, _threshold);
		}

	    /// <summary>
	    ///     Returns a b/w of Bitmap
	    /// </summary>
	    /// <param name="sourceBitmap">Bitmap to create a b/w of</param>
	    /// <param name="threshold">Threshold for monochrome filter (0 - 255), lower value means less black</param>
	    /// <returns>b/w bitmap</returns>
	    public static IBitmapWithNativeSupport CreateMonochrome(IBitmapWithNativeSupport sourceBitmap, byte threshold)
	    {
	        using (var fastBitmap = FastBitmapFactory.CreateCloneOf(sourceBitmap, sourceBitmap.PixelFormat))
	        {
	            Parallel.For(0, fastBitmap.Height, y =>
	            {
                    // TODO: use stackalloc / unsafe
	                for (var x = 0; x < fastBitmap.Width; x++)
	                {
	                    var color = fastBitmap.GetColorAt(x, y);
	                    var colorBrightness = (color.R + color.G + color.B) / 3 > threshold ? 255 : 0;
	                    var monoColor = Color.FromArgb(color.A, colorBrightness, colorBrightness, colorBrightness);
	                    fastBitmap.SetColorAt(x, y, ref monoColor);
	                }
	            });
	            return fastBitmap.UnlockAndReturnBitmap();
	        }
	    }


    }
}