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

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Greenshot.Gfx.FastBitmap;

namespace Greenshot.Gfx.Effects
{
    /// <summary>
    ///     BlurEffect
    /// </summary>
    [TypeConverter(typeof(EffectConverter))]
	public class BlurEffect : IEffect
	{
        /// <summary>
        /// The range for the blur
        /// </summary>
	    public int Range { get; set; } = 3;

        /// <inheritdoc />
        public virtual IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
		    var result = FastBitmapFactory.CreateCloneOf(sourceBitmap);
		    result.ApplyBoxBlur(Range);
		    return result.UnlockAndReturnBitmap();
		}
	}
}