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
using System.Drawing.Drawing2D;
using Dapplo.Log;
using Greenshot.Gfx.Quantizer;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	///     ReduceColorsEffect
	/// </summary>
	public class ReduceColorsEffect : IEffect
	{
		private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// The amount of colors the bitmap is allowed to have
        /// </summary>
        public int Colors { get; set; } = 256;

        /// <inheritdoc />
        public IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			using (var quantizer = new WuQuantizer(sourceBitmap))
			{
				var colorCount = quantizer.GetColorCount();
			    if (colorCount <= Colors)
			    {
			        return null;
			    }
			    try
			    {
			        return quantizer.GetQuantizedImage(Colors);
			    }
			    catch (Exception e)
			    {
			        Log.Warn().WriteLine(e, "Error occurred while Quantizing the image, ignoring and using original. Error: ");
			    }
			}
			return null;
		}
	}
}