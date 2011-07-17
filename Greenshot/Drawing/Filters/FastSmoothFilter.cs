/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;

using Greenshot.Drawing.Fields;
using Greenshot.Plugin.Drawing;

namespace Greenshot.Drawing.Filters {
	[Serializable()]
	public class FastSmoothFilter : AbstractFilter {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FastSmoothFilter));

		public FastSmoothFilter(DrawableContainer parent) : base(parent) {
			AddField(GetType(), FieldType.BLUR_RADIUS, 3);
		}
		
		public unsafe override void Apply(Graphics graphics, Bitmap applyBitmap, Rectangle applyRect, RenderMode renderMode) {
			Rectangle sourceRect = new Rectangle(applyRect.X, applyRect.Y, applyRect.Width, applyRect.Height);
			Rectangle bitmapRect = new Rectangle(0,0, applyBitmap.Width, applyBitmap.Height);
			
			if(sourceRect.Equals(Rectangle.Empty)) {
				sourceRect = bitmapRect;
			} else {
				sourceRect.Intersect(bitmapRect);
			}
			// Does the rect have any pixels?
			if (sourceRect.Height <= 0 || sourceRect.Width <= 0) {
				return;
			}
			
			int blurRadius = GetFieldValueAsInt(FieldType.BLUR_RADIUS);

			// Calculate new sourceRect to include the matrix size if possible

			int leftOffset = Math.Min(sourceRect.X, blurRadius);
			int rightOffset = Math.Min(applyBitmap.Width - (sourceRect.X + sourceRect.Width), blurRadius);
			int topOffset = Math.Min(sourceRect.Y, blurRadius);
			int bottomOffset = Math.Min(applyBitmap.Height - (sourceRect.Y + sourceRect.Height), blurRadius);
			LOG.Debug(String.Format("Offsets: {0},{1},{2},{3}", leftOffset, rightOffset, topOffset, bottomOffset));
			LOG.Debug(String.Format("SourceRect before: {0},{1},{2},{3}", sourceRect.X, sourceRect.Width, sourceRect.Y, sourceRect.Height));
			sourceRect.X -= leftOffset;
			sourceRect.Width += leftOffset + rightOffset;
			sourceRect.Y -= topOffset;
			sourceRect.Height += topOffset + bottomOffset;
			LOG.Debug(String.Format("SourceRect after: {0},{1},{2},{3}", sourceRect.X, sourceRect.Width, sourceRect.Y, sourceRect.Height));
			// Make a copy of the applyBitmap for reading

			using (Bitmap sourceBitmap = applyBitmap.Clone(sourceRect, applyBitmap.PixelFormat)) {
				ApplySmooth(sourceBitmap, applyBitmap, sourceRect, blurRadius);
			}
		}

		public static bool ApplySmooth(Bitmap sourceBitmap, Bitmap destinationBitmap, Rectangle destRectangle, int blurRadius) {
			if (blurRadius < 1) {
				return false;
			}

			BitmapData destbitmapData = destinationBitmap.LockBits(destRectangle, ImageLockMode.WriteOnly, destinationBitmap.PixelFormat); 
			BitmapData srcBitmapData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), 
							   ImageLockMode.ReadOnly, 
							   sourceBitmap.PixelFormat); 

			int destStride = destbitmapData.Stride;
			int srcStride = srcBitmapData.Stride;
		
			IntPtr destScan0 = destbitmapData.Scan0; 
			IntPtr srcScan0 = srcBitmapData.Scan0; 

			int bIndex;
			int gIndex;
			int rIndex;
			//int aIndex;
			int bytesPerPixel;
			switch(destinationBitmap.PixelFormat) {
				case PixelFormat.Format32bppArgb:
					// GDI+ lies to us - the return format is BGR, NOT RGB. 
					bIndex = 0;
					gIndex = 1;
					rIndex = 2;
					//aIndex = 3;
					bytesPerPixel = 4;
					break;
				case PixelFormat.Format32bppRgb:
					// GDI+ lies to us - the return format is BGR, NOT RGB.
					bIndex = 0;
					gIndex = 1;
					rIndex = 2;
					bytesPerPixel = 4;
					break;
				case PixelFormat.Format24bppRgb:
					// GDI+ lies to us - the return format is BGR, NOT RGB.
					bIndex = 0;
					gIndex = 1;
					rIndex = 2;
					bytesPerPixel = 3;
					break;
				default: 
					throw new FormatException("Bitmap.Pixelformat."+destinationBitmap.PixelFormat+" is currently not supported. Supported: Format32bpp(A)Rgb, Format24bppRgb");
			}
			long ticks = DateTime.Now.Ticks;
			unsafe { 
				int width = destRectangle.Width - (blurRadius*2);
				int height = destRectangle.Height - (blurRadius*2);
				int diameter = (2 * blurRadius) + 1;
				int mid = ((diameter-1)/2) + 1;
				int midPixel = mid * bytesPerPixel;
				int midLine = (mid - 1) * destStride;
				int writeRPixelOffset = midLine + midPixel + rIndex;
				int writeGPixelOffset = midLine + midPixel + gIndex;
				int writeBPixelOffset = midLine + midPixel + bIndex;
				int readRPixelOffset = diameter * bytesPerPixel + rIndex;
				int readGPixelOffset = diameter * bytesPerPixel + gIndex;
				int readBPixelOffset = diameter * bytesPerPixel + bIndex;

				int destOffset = destStride - (width*bytesPerPixel);
				int srcOffset = srcStride - (width*bytesPerPixel);
				int factor = diameter * diameter;

				byte *destBitmapPointer = (byte *)(void *)destScan0;
				byte *srcBitmapPointer = (byte *)(void *)srcScan0;
				int pixel, lineOffset;
				
				int rTotal, gTotal, bTotal;
				int [,]r = new int[diameter,diameter];
				int [,]g = new int[diameter,diameter];
				int [,]b = new int[diameter,diameter];
				for (int y=0; y < height; y++) {
					// Reset totals
					rTotal = gTotal = bTotal = 0;
					
					// Intial load: Do the complete radius, fill the arrays with their values
					lineOffset = 0;	// Offset relative to the srcBitmapPointer
					// Vertical loop
					for (int v = 0; v < diameter; v++) {
						// Start at beginning of the current line
						int index = lineOffset;
						// Horizontal loop
						for (int currentRow = 0; currentRow < diameter; currentRow++) {
							// Get & add values
							rTotal += r[v,currentRow] = srcBitmapPointer[index + rIndex];
							gTotal += g[v,currentRow] = srcBitmapPointer[index + gIndex];
							bTotal += b[v,currentRow] = srcBitmapPointer[index + bIndex];
							// Move 1px to the right
							index += bytesPerPixel;
						}
						// Move to next line
						lineOffset += srcStride;
					}

					// Now loop the complete line from left to right
					for (int x=0; x < width; x++ ) {
						// Draw Pixel with calculated values
						pixel = rTotal / factor;
						if (pixel < 0) pixel = 0;
						if (pixel > 255) pixel = 255;
						destBitmapPointer[writeRPixelOffset]= (byte)pixel;

						pixel = gTotal / factor;
						if (pixel < 0) pixel = 0;
						if (pixel > 255) pixel = 255;
						destBitmapPointer[writeGPixelOffset]= (byte)pixel;

						pixel = bTotal / factor;
						if (pixel < 0) pixel = 0;
						if (pixel > 255) pixel = 255;
						destBitmapPointer[writeBPixelOffset]= (byte)pixel;

						// Update values with next "row"
						int oldRow = x % diameter;
						srcBitmapPointer += bytesPerPixel;
						lineOffset = 0;
						// Vertical Loop, subtract the stored value and add the new value
						for (int v = 0; v < diameter; v++) {
							// Subtrace the first value, so we can add the new later
							rTotal -= r[v, oldRow];
							gTotal -= g[v, oldRow];
							bTotal -= b[v, oldRow];
							
							// Get & add the next values
							rTotal += r[v, oldRow] = srcBitmapPointer[readRPixelOffset + lineOffset];
							gTotal += g[v, oldRow] = srcBitmapPointer[readGPixelOffset + lineOffset];
							bTotal += b[v, oldRow] = srcBitmapPointer[readBPixelOffset + lineOffset];
							
							// Goto next line
							lineOffset += srcStride;
						}
						destBitmapPointer += bytesPerPixel;
					}
					srcBitmapPointer += srcOffset;
					destBitmapPointer += destOffset;
				}
			}
			LOG.Info("Ticks = " + (DateTime.Now.Ticks - ticks));
			
			destinationBitmap.UnlockBits(destbitmapData); 
			sourceBitmap.UnlockBits(srcBitmapData); 
			return true; 
		}

		/**
		 * Checks if the supplied Bitmap has a PixelFormat we support
		 */
		private bool SupportsPixelFormat(Bitmap bitmap) {
			return (bitmap.PixelFormat.Equals(PixelFormat.Format32bppArgb) || 
					bitmap.PixelFormat.Equals(PixelFormat.Format32bppRgb) || 
					bitmap.PixelFormat.Equals(PixelFormat.Format24bppRgb));
		}

	}
}
