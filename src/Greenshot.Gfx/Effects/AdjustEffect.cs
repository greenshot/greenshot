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
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx.Effects
{
	/// <summary>
	///     AdjustEffect
	/// </summary>
	public class AdjustEffect : IEffect
	{
        /// <summary>
        /// The contrast for the effect
        /// </summary>
	    public float Contrast { get; set; } = 1f;

        /// <summary>
        /// The brightness for the effect
        /// </summary>
	    public float Brightness { get; set; } = 1f;

        /// <summary>
        /// The gamma for the effect
        /// </summary>
	    public float Gamma { get; set; } = 1f;

        /// <inheritdoc />
	    public IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
        {
			return Adjust(sourceBitmap, Brightness, Contrast, Gamma);
		}

        /// <summary>
        ///     Adjust the brightness, contract or gamma of an image.
        ///     Use the value "1.0f" for no changes.
        /// </summary>
        /// <param name="sourceBitmap">Original bitmap</param>
        /// <param name="brightness"></param>
        /// <param name="contrast"></param>
        /// <param name="gamma"></param>
        /// <returns>Bitmap with grayscale</returns>
        public static IBitmapWithNativeSupport Adjust(IBitmapWithNativeSupport sourceBitmap, float brightness, float contrast, float gamma)
	    {
	        //create a blank bitmap the same size as original
	        // If using 8bpp than the following exception comes: A Graphics object cannot be created from an image that has an indexed pixel format. 
	        var newBitmap = BitmapFactory.CreateEmpty(sourceBitmap.Width, sourceBitmap.Height, PixelFormat.Format24bppRgb, Color.Empty, sourceBitmap.HorizontalResolution,
	            sourceBitmap.VerticalResolution);
	        using (var adjustAttributes = CreateAdjustAttributes(brightness, contrast, gamma))
	        {
	            sourceBitmap.ApplyImageAttributes(NativeRect.Empty, newBitmap, NativeRect.Empty, adjustAttributes);
	        }
	        return newBitmap;
	    }

	    /// <summary>
	    ///     Create ImageAttributes to modify
	    /// </summary>
	    /// <param name="brightness"></param>
	    /// <param name="contrast"></param>
	    /// <param name="gamma"></param>
	    /// <returns>ImageAttributes</returns>
	    public static ImageAttributes CreateAdjustAttributes(float brightness, float contrast, float gamma)
	    {
	        var adjustedBrightness = brightness - 1.0f;
	        var applyColorMatrix = new ColorMatrix(
	            new[]
	            {
	                new[] {contrast, 0, 0, 0, 0}, // scale red
	                new[] {0, contrast, 0, 0, 0}, // scale green
	                new[] {0, 0, contrast, 0, 0}, // scale blue
	                new[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
	                new[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}
	            });

	        //create some image attributes
	        var attributes = new ImageAttributes();
	        attributes.ClearColorMatrix();
	        attributes.SetColorMatrix(applyColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
	        attributes.SetGamma(gamma, ColorAdjustType.Bitmap);
	        return attributes;
	    }

    }
}