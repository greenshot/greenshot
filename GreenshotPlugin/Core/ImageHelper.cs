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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;

using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Core;
using IniFile;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of ImageHelper.
	/// </summary>
	public static class ImageHelper {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImageHelper));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		public static Image CreateThumbnail(Image image, int thumbWidth, int thumbHeight) {
			return CreateThumbnail(image, thumbWidth, thumbHeight, -1, -1);
		}
		public static Image CreateThumbnail(Image image, int thumbWidth, int thumbHeight, int maxWidth, int maxHeight) {
			int srcWidth=image.Width;
			int srcHeight=image.Height; 
			if (thumbHeight < 0) {
				thumbHeight = (int)(thumbWidth * ((float)srcHeight / (float)srcWidth));
			}
			if (thumbWidth < 0) {
				thumbWidth = (int)(thumbHeight * ((float)srcWidth / (float)srcHeight));
			}
			if (maxWidth > 0 && thumbWidth > maxWidth) {
				thumbWidth = Math.Min(thumbWidth, maxWidth);
				thumbHeight = (int)(thumbWidth * ((float)srcHeight / (float)srcWidth));
			}
			if (maxHeight > 0 && thumbHeight > maxHeight) {
				thumbHeight = Math.Min(thumbHeight, maxHeight);
				thumbWidth = (int)(thumbHeight * ((float)srcWidth / (float)srcHeight));
			}

			Bitmap bmp = new Bitmap(thumbWidth, thumbHeight);
			using (Graphics gr = System.Drawing.Graphics.FromImage(bmp)) {
				gr.SmoothingMode = SmoothingMode.HighQuality  ; 
				gr.CompositingQuality = CompositingQuality.HighQuality; 
				gr.InterpolationMode = InterpolationMode.High; 
				System.Drawing.Rectangle rectDestination = new System.Drawing.Rectangle(0, 0, thumbWidth, thumbHeight);
				gr.DrawImage(image, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);  
			}
			return bmp;
		}
		
		/// <summary>
		/// Crops the image to the specified rectangle
		/// </summary>
		/// <param name="image">Image to crop</param>
		/// <param name="cropRectangle">Rectangle with bitmap coordinates, will be "intersected" to the bitmap</param>
		public static bool Crop(ref Image image, ref Rectangle cropRectangle) {
			Image returnImage = null;
			if (image != null && image is Bitmap && ((image.Width * image.Height) > 0))  {
				cropRectangle.Intersect(new Rectangle(0,0, image.Width, image.Height));
				if (cropRectangle.Width != 0 || cropRectangle.Height != 0) {
					returnImage = (image as Bitmap).Clone(cropRectangle, image.PixelFormat);
					image.Dispose();
					image = returnImage;
					return true;
				}
			}
			LOG.Warn("Can't crop a null/zero size image!");
			return false;
		}
		
		private static Rectangle FindAutoCropRectangle(BitmapBuffer buffer, Point colorPoint) {
			Rectangle cropRectangle = Rectangle.Empty;
			Color referenceColor = buffer.GetColorAtWithoutAlpha(colorPoint.X,colorPoint.Y);
			Point min = new Point(int.MaxValue, int.MaxValue);
			Point max = new Point(int.MinValue, int.MinValue);
			
			if (conf.AutoCropDifference > 0) {
				for(int y = 0; y < buffer.Height; y++) {
					for(int x = 0; x < buffer.Width; x++) {
						Color currentColor = buffer.GetColorAt(x, y);
						int diffR = Math.Abs(currentColor.R - referenceColor.R);
						int diffG = Math.Abs(currentColor.G - referenceColor.G);
						int diffB = Math.Abs(currentColor.B - referenceColor.B);
						if (((diffR+diffG+diffB)/3) > conf.AutoCropDifference) {
							if (x < min.X) min.X = x;
							if (y < min.Y) min.Y = y;
							if (x > max.X) max.X = x;
							if (y > max.Y) max.Y = y;
						}
					}
				}
			} else {
				for(int y = 0; y < buffer.Height; y++) {
					for(int x = 0; x < buffer.Width; x++) {
						Color currentColor = buffer.GetColorAtWithoutAlpha(x, y);
						if (referenceColor.Equals(currentColor)) {
							if (x < min.X) min.X = x;
							if (y < min.Y) min.Y = y;
							if (x > max.X) max.X = x;
							if (y > max.Y) max.Y = y;
						}
					}
				}
			}

			if (!(Point.Empty.Equals(min) && max.Equals(new Point(buffer.Width-1, buffer.Height-1)))) {
				if (!(min.X == int.MaxValue || min.Y == int.MaxValue || max.X == int.MinValue || min.X == int.MinValue)) {
					cropRectangle = new Rectangle(min.X, min.Y, max.X - min.X + 1, max.Y - min.Y + 1);
				}
			}
			return cropRectangle;
		}
		/// <summary>
		/// Get a rectangle for the image which crops the image of all colors equal to that on 0,0
		/// </summary>
		/// <param name="image"></param>
		/// <returns>Rectangle</returns>
		public static Rectangle FindAutoCropRectangle(Image image) {
			Rectangle cropRectangle = Rectangle.Empty;
			using (BitmapBuffer buffer = new BitmapBuffer((Bitmap)image, false)) {
				buffer.Lock();
				Rectangle currentRectangle = Rectangle.Empty;
				List<Point> checkPoints = new List<Point>();
				// Top Left
				checkPoints.Add(new Point(0, 0));
				// Bottom Left
				checkPoints.Add(new Point(0, image.Height-1));
				// Top Right
				checkPoints.Add(new Point(image.Width-1, 0));
				// Bottom Right
				checkPoints.Add( new Point(image.Width-1, image.Height-1));

				// find biggest area
				foreach(Point checkPoint in checkPoints) {
					currentRectangle = FindAutoCropRectangle(buffer, checkPoint);
					if (currentRectangle.Width * currentRectangle.Height > cropRectangle.Width * cropRectangle.Height) {
						cropRectangle = currentRectangle;
					}
				}
			}
			return cropRectangle;
		}
		
		public static Bitmap LoadBitmap(string filename) {
			if (string.IsNullOrEmpty(filename)) {
				return null;
			}
			Bitmap fileBitmap = null;
			LOG.InfoFormat("Loading image from file {0}", filename);
			// Fixed lock problem Bug #3431881
			using (Stream imageFileStream = File.OpenRead(filename)) {
				// And fixed problem that the bitmap stream is disposed... by Cloning the image
				// This also ensures the bitmap is correctly created
				
				if (filename.EndsWith(".ico")) {
					// Icon logic, try to get the Vista icon, else the biggest possible
					try {
						using (Image tmpImage = ExtractVistaIcon(imageFileStream)) {
							if (tmpImage != null) {
								fileBitmap = CloneImageToBitmap(tmpImage);
							}
						}
					} catch (Exception vistaIconException) {
						LOG.Warn("Can't read icon from " + filename, vistaIconException);
					}
					if (fileBitmap == null) {
						try {
							// No vista icon, try normal icon
							imageFileStream.Position = 0;
							// We create a copy of the bitmap, so everything else can be disposed
							using (Icon tmpIcon = new Icon(imageFileStream, new Size(1024,1024))) {
								using (Image tmpImage = tmpIcon.ToBitmap()) {
									fileBitmap = ImageHelper.CloneImageToBitmap(tmpImage);
								}
							}
						} catch (Exception iconException) {
							LOG.Warn("Can't read icon from " + filename, iconException);
						}
					}
				}
				if (fileBitmap == null) {
					// We create a copy of the bitmap, so everything else can be disposed
					imageFileStream.Position = 0;
					using (Image tmpImage = Image.FromStream(imageFileStream, true, true)) {
						fileBitmap = ImageHelper.CloneImageToBitmap(tmpImage);
					}
				}
			}
			return fileBitmap;
		}
		
		/// <summary>
		/// Clone the image to a bitmap
		/// </summary>
		/// <param name="srcImage">Image to clone</param>
		/// <returns>Bitmap</returns>
		public static Bitmap CloneImageToBitmap(Image srcImage) {
			Bitmap returnImage;
			int width = srcImage.Width;
			int height = srcImage.Height;
			float horizontalResolution = srcImage.HorizontalResolution;
			float verticalResolution = srcImage.VerticalResolution;
			PixelFormat pixelFormat = srcImage.PixelFormat;
			if (srcImage is Metafile) {
				pixelFormat = PixelFormat.Format32bppArgb;
			}
			// Make sure Greenshot supports the pixelformat, if not convert to one we support
			if (!isSupported(pixelFormat)) {
				pixelFormat = PixelFormat.Format24bppRgb;
			}
			returnImage = new Bitmap(width, height, pixelFormat);
			returnImage.SetResolution(horizontalResolution, verticalResolution);
			using (Graphics graphics = Graphics.FromImage(returnImage)) {
				if (Image.IsAlphaPixelFormat(pixelFormat)) {
					graphics.Clear(Color.Transparent);
				} else {
					graphics.Clear(Color.White);
				}
				graphics.DrawImageUnscaled(srcImage, 0, 0);
			}
			return returnImage;
		}

		/**
		 * Checks if we support the supplied PixelFormat
		 */
		private static bool isSupported(PixelFormat pixelformat) {
			return (PixelFormat.Format32bppArgb.Equals(pixelformat)||
					PixelFormat.Format32bppRgb.Equals(pixelformat) || 
					PixelFormat.Format24bppRgb.Equals(pixelformat));
		}
		
		// Based on: http://www.codeproject.com/KB/cs/IconExtractor.aspx
		// And a hint from: http://www.codeproject.com/KB/cs/IconLib.aspx
		public static Bitmap ExtractVistaIcon(Stream iconStream) {
			const int SizeICONDIR = 6;
			const int SizeICONDIRENTRY = 16;
			Bitmap bmpPngExtracted = null;
			try {
				byte[] srcBuf = new byte[iconStream.Length];
				iconStream.Read(srcBuf, 0, (int)iconStream.Length);
				int iCount = BitConverter.ToInt16(srcBuf, 4);
				for (int iIndex=0; iIndex<iCount; iIndex++) {
					int iWidth  = srcBuf[SizeICONDIR + SizeICONDIRENTRY * iIndex];
					int iHeight = srcBuf[SizeICONDIR + SizeICONDIRENTRY * iIndex + 1];
					int iBitCount   = BitConverter.ToInt16(srcBuf, SizeICONDIR + SizeICONDIRENTRY * iIndex + 6);
					if (iWidth == 0 && iHeight == 0) {
						int iImageSize   = BitConverter.ToInt32(srcBuf, SizeICONDIR + SizeICONDIRENTRY * iIndex + 8);
						int iImageOffset = BitConverter.ToInt32(srcBuf, SizeICONDIR + SizeICONDIRENTRY * iIndex + 12);
						using (MemoryStream destStream = new MemoryStream()) {
							using (BinaryWriter writer = new BinaryWriter(destStream)) {
								writer.Write(srcBuf, iImageOffset, iImageSize);
								destStream.Seek(0, System.IO.SeekOrigin.Begin);
								bmpPngExtracted = new Bitmap(destStream); // This is PNG! :)
							}
						}
						break;
					}
				}
			} catch {
				return null;
			}
			return bmpPngExtracted;
		}

		public static Icon ExtractAssociatedIcon(this Icon icon, string location) {
			IntPtr large;
			IntPtr small;
			Shell32.ExtractIconEx(location, 0, out large, out small, 1);
			Icon returnIcon = Icon.FromHandle(small);
			if (!IntPtr.Zero.Equals(small)){
				User32.DestroyIcon(small);
			}
			if (!IntPtr.Zero.Equals(large)){
				User32.DestroyIcon(large);
			}
			return returnIcon;
		}
	}
}
