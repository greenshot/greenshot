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
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	///     DropShadowEffect
	/// </summary>
	[TypeConverter(typeof(EffectConverter))]
	public class DropShadowEffect : IEffect
	{
        /// <summary>
        /// The darkness
        /// </summary>
	    public float Darkness { get; set; } = 0.6f;

        /// <summary>
        /// The size of the shadow
        /// </summary>
	    public int ShadowSize { get; set; } = 7;

        /// <summary>
        /// Offset of the shadow
        /// </summary>
	    public NativePoint ShadowOffset { get; set; } = new NativePoint(-1, -1);

        /// <summary>
        /// Apply this effect to the specified bitmap
        /// </summary>
        /// <param name="sourceBitmap">Bitmap</param>
        /// <param name="matrix">Matrix</param>
        /// <returns>Bitmap</returns>
		public virtual IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			return sourceBitmap.CreateShadow(Darkness, ShadowSize, ShadowOffset, matrix, PixelFormat.Format32bppArgb);
		}
	}
}