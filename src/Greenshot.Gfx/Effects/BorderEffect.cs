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
	///     BorderEffect
	/// </summary>
	public class BorderEffect : IEffect
	{
        /// <summary>
        /// The color of the border
        /// </summary>
	    public Color Color { get; set; } = Color.Black;

        /// <summary>
        /// The width of the border
        /// </summary>
	    public int Width { get; set; } = 2;

        /// <inheritdoc />
        public IBitmapWithNativeSupport Apply(IBitmapWithNativeSupport sourceBitmap, Matrix matrix)
		{
			return CreateBorder(sourceBitmap, Width, Color, sourceBitmap.PixelFormat, matrix);
		}

	    /// <summary>
	    ///     Create a new bitmap where the sourceBitmap has a Simple border around it
	    /// </summary>
	    /// <param name="sourceBitmap">Bitmap to make a border on</param>
	    /// <param name="borderSize">Size of the border</param>
	    /// <param name="borderColor">Color of the border</param>
	    /// <param name="targetPixelformat">What pixel format must the returning bitmap have</param>
	    /// <param name="matrix">
	    ///     The transform matrix which describes how the elements need to be transformed to stay at the same
	    ///     location
	    /// </param>
	    /// <returns>Bitmap with the shadow, is bigger than the sourceBitmap!!</returns>
	    public static IBitmapWithNativeSupport CreateBorder(IBitmapWithNativeSupport sourceBitmap, int borderSize, Color borderColor, PixelFormat targetPixelformat, Matrix matrix)
	    {
	        // "return" the shifted offset, so the caller can e.g. move elements
	        var offset = new NativePoint(borderSize, borderSize);
	        matrix.Translate(offset.X, offset.Y, MatrixOrder.Append);

	        // Create a new "clean" image
	        var newImage = BitmapFactory.CreateEmpty(sourceBitmap.Width + borderSize * 2, sourceBitmap.Height + borderSize * 2, targetPixelformat, Color.Empty, sourceBitmap.HorizontalResolution,
	            sourceBitmap.VerticalResolution);
	        using (var graphics = Graphics.FromImage(newImage.NativeBitmap))
	        {
	            // Make sure we draw with the best quality!
	            graphics.SmoothingMode = SmoothingMode.HighQuality;
	            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
	            graphics.CompositingQuality = CompositingQuality.HighQuality;
	            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
	            using (var path = new GraphicsPath())
	            {
	                path.AddRectangle(new NativeRect(borderSize >> 1, borderSize >> 1, newImage.Width - borderSize, newImage.Height - borderSize));
	                using (var pen = new Pen(borderColor, borderSize))
	                {
	                    pen.LineJoin = LineJoin.Round;
	                    pen.StartCap = LineCap.Round;
	                    pen.EndCap = LineCap.Round;
	                    graphics.DrawPath(pen, path);
	                }
	            }
	            // draw original with a TextureBrush so we have nice anti-aliasing!
	            using (Brush textureBrush = new TextureBrush(sourceBitmap.NativeBitmap, WrapMode.Clamp))
	            {
	                // We need to do a translate-tranform otherwise the image is wrapped
	                graphics.TranslateTransform(offset.X, offset.Y);
	                graphics.FillRectangle(textureBrush, 0, 0, sourceBitmap.Width, sourceBitmap.Height);
	            }
	        }
	        return newImage;
	    }

    }
}