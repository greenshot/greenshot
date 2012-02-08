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
			using (Graphics graphics = System.Drawing.Graphics.FromImage(bmp)) {
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				Rectangle rectDestination = new Rectangle(0, 0, thumbWidth, thumbHeight);
				graphics.DrawImage(image, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);  
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

		/// <summary>
		/// Make the picture look like it's torn
		/// </summary>
		/// <param name="sourceBitmap">Bitmap to make torn edge off</param>
		/// <returns>Changed bitmap</returns>
		public static Bitmap CreateTornEdge(Bitmap sourceBitmap) {
			Bitmap returnImage = new Bitmap(sourceBitmap.Width, sourceBitmap.Height, PixelFormat.Format32bppArgb);
			try {
				returnImage.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
			} catch (Exception ex) {
				LOG.Warn("An exception occured while setting the resolution.", ex);
			}
			GraphicsPath path = new GraphicsPath();
			Random random = new Random();
			int regionWidth = 20;
			int regionHeight = 20;
			int HorizontalRegions = (int)(sourceBitmap.Width / regionWidth);
			int VerticalRegions = (int)(sourceBitmap.Height / regionHeight);
			int distance = 12;

			// Start
			Point previousEndingPoint = new Point(regionWidth, random.Next(1, distance));
			Point newEndingPoint;
			// Top
			for (int i = 0; i < HorizontalRegions; i++) {
				int x = (int)previousEndingPoint.X + regionWidth;
				int y = random.Next(1, distance);
				newEndingPoint = new Point(x, y);
				path.AddLine(previousEndingPoint, newEndingPoint);
				previousEndingPoint = newEndingPoint;
			}

			// Right
			for (int i = 0; i < VerticalRegions; i++) {
				int x = sourceBitmap.Width - random.Next(1, distance);
				int y = (int)previousEndingPoint.Y + regionHeight;
				newEndingPoint = new Point(x, y);
				path.AddLine(previousEndingPoint, newEndingPoint);
				previousEndingPoint = newEndingPoint;
			}

			// Bottom
			for (int i = 0; i < HorizontalRegions; i++) {
				int x = (int)previousEndingPoint.X - regionWidth;
				int y = sourceBitmap.Height - random.Next(1, distance);
				newEndingPoint = new Point(x, y);
				path.AddLine(previousEndingPoint, newEndingPoint);
				previousEndingPoint = newEndingPoint;
			}

			// Left
			for (int i = 0; i < VerticalRegions; i++) {
				int x = random.Next(1, distance);
				int y = (int)previousEndingPoint.Y - regionHeight;
				newEndingPoint = new Point(x, y);
				path.AddLine(previousEndingPoint, newEndingPoint);
				previousEndingPoint = newEndingPoint;
			}
			path.CloseFigure();

			// Draw
			using (Graphics graphics = Graphics.FromImage(returnImage)) {
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				using (Brush brush = new TextureBrush(sourceBitmap)) {
					graphics.FillPath(brush, path);
				}
			}
			return returnImage;
		}

		/// <summary>
		/// Helper Method for the ApplyBlur
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		private static int[] CreateGaussianBlurRow(int amount) {
			int size = 1 + (amount * 2);
			int[] weights = new int[size];

			for (int i = 0; i <= amount; ++i) {
				// 1 + aa - aa + 2ai - ii
				weights[i] = 16 * (i + 1);
				weights[weights.Length - i - 1] = weights[i];
			}

			return weights;
		}

		/// <summary>
		/// Apply sourceBitmap with a blur to the targetGraphics
		/// </summary>
		/// <param name="targetGraphics">Target to draw to</param>
		/// <param name="sourceBitmap">Source to blur</param>
		/// <param name="rect">Area to blur</param>
		/// <param name="export">Use full quality</param>
		/// <param name="blurRadius">Radius of the blur</param>
		/// <param name="previewQuality">Quality, use 1d for normal anything less skipps calculations</param>
		/// <param name="invert">true if the blur needs to occur outside of the area</param>
		/// <param name="parentBounds">Rectangle limiting the area when using invert</param>
		public static void ApplyBlur(Graphics targetGraphics, Bitmap sourceBitmap, Rectangle rect, bool export, int blurRadius, double previewQuality, bool invert, Rectangle parentBounds) {
			Rectangle applyRect = CreateIntersectRectangle(sourceBitmap.Size, rect, invert);

			if (applyRect.Height <= 0 || applyRect.Width <= 0) {
				return;
			}
			// do nothing when nothing can be done!
			if (blurRadius < 1) {
				return;
			}
			Color nullColor = Color.White;
			if (sourceBitmap.PixelFormat == PixelFormat.Format32bppArgb) {
				nullColor = Color.Transparent;
			}

			using (BitmapBuffer bbbDest = new BitmapBuffer(sourceBitmap, applyRect)) {
				bbbDest.Lock();
				using (BitmapBuffer bbbSrc = new BitmapBuffer(sourceBitmap, applyRect)) {
					bbbSrc.Lock();
					Random rand = new Random();

					int r = blurRadius;
					int[] w = CreateGaussianBlurRow(r);
					int wlen = w.Length;
					long[] waSums = new long[wlen];
					long[] wcSums = new long[wlen];
					long[] aSums = new long[wlen];
					long[] bSums = new long[wlen];
					long[] gSums = new long[wlen];
					long[] rSums = new long[wlen];
					for (int y = 0; y < applyRect.Height; ++y) {
						long waSum = 0;
						long wcSum = 0;
						long aSum = 0;
						long bSum = 0;
						long gSum = 0;
						long rSum = 0;

						for (int wx = 0; wx < wlen; ++wx) {
							int srcX = wx - r;
							waSums[wx] = 0;
							wcSums[wx] = 0;
							aSums[wx] = 0;
							bSums[wx] = 0;
							gSums[wx] = 0;
							rSums[wx] = 0;

							if (srcX >= 0 && srcX < bbbDest.Width) {
								for (int wy = 0; wy < wlen; ++wy) {
									int srcY = y + wy - r;

									if (srcY >= 0 && srcY < bbbDest.Height) {
										int[] colors = bbbSrc.GetColorArrayAt(srcX, srcY);
										int wp = w[wy];

										waSums[wx] += wp;
										wp *= colors[0] + (colors[0] >> 7);
										wcSums[wx] += wp;
										wp >>= 8;

										aSums[wx] += wp * colors[0];
										bSums[wx] += wp * colors[3];
										gSums[wx] += wp * colors[2];
										rSums[wx] += wp * colors[1];
									}
								}

								int wwx = w[wx];
								waSum += wwx * waSums[wx];
								wcSum += wwx * wcSums[wx];
								aSum += wwx * aSums[wx];
								bSum += wwx * bSums[wx];
								gSum += wwx * gSums[wx];
								rSum += wwx * rSums[wx];
							}
						}

						wcSum >>= 8;

						if (waSum == 0 || wcSum == 0) {
							if (parentBounds.Contains(applyRect.Left, applyRect.Top + y) ^ invert) {
								bbbDest.SetColorAt(0, y, nullColor);
							}
						} else {
							int alpha = (int)(aSum / waSum);
							int blue = (int)(bSum / wcSum);
							int green = (int)(gSum / wcSum);
							int red = (int)(rSum / wcSum);
							if (parentBounds.Contains(applyRect.Left, applyRect.Top + y) ^ invert) {
								bbbDest.SetColorAt(0, y, Color.FromArgb(alpha, red, green, blue));
							}
						}

						for (int x = 1; x < applyRect.Width; ++x) {
							for (int i = 0; i < wlen - 1; ++i) {
								waSums[i] = waSums[i + 1];
								wcSums[i] = wcSums[i + 1];
								aSums[i] = aSums[i + 1];
								bSums[i] = bSums[i + 1];
								gSums[i] = gSums[i + 1];
								rSums[i] = rSums[i + 1];
							}

							waSum = 0;
							wcSum = 0;
							aSum = 0;
							bSum = 0;
							gSum = 0;
							rSum = 0;

							int wx;
							for (wx = 0; wx < wlen - 1; ++wx) {
								long wwx = (long)w[wx];
								waSum += wwx * waSums[wx];
								wcSum += wwx * wcSums[wx];
								aSum += wwx * aSums[wx];
								bSum += wwx * bSums[wx];
								gSum += wwx * gSums[wx];
								rSum += wwx * rSums[wx];
							}

							wx = wlen - 1;

							waSums[wx] = 0;
							wcSums[wx] = 0;
							aSums[wx] = 0;
							bSums[wx] = 0;
							gSums[wx] = 0;
							rSums[wx] = 0;

							int srcX = x + wx - r;

							if (srcX >= 0 && srcX < applyRect.Width) {
								for (int wy = 0; wy < wlen; ++wy) {
									int srcY = y + wy - r;
									// only when in EDIT mode, ignore some pixels depending on preview quality
									if ((export || rand.NextDouble() < previewQuality) && srcY >= 0 && srcY < applyRect.Height) {
										int[] colors = bbbSrc.GetColorArrayAt(srcX, srcY);
										int wp = w[wy];

										waSums[wx] += wp;
										wp *= colors[0] + (colors[0] >> 7);
										wcSums[wx] += wp;
										wp >>= 8;

										aSums[wx] += wp * (long)colors[0];
										bSums[wx] += wp * (long)colors[3];
										gSums[wx] += wp * (long)colors[2];
										rSums[wx] += wp * (long)colors[1];
									}
								}

								int wr = w[wx];
								waSum += (long)wr * waSums[wx];
								wcSum += (long)wr * wcSums[wx];
								aSum += (long)wr * aSums[wx];
								bSum += (long)wr * bSums[wx];
								gSum += (long)wr * gSums[wx];
								rSum += (long)wr * rSums[wx];
							}

							wcSum >>= 8;

							if (waSum == 0 || wcSum == 0) {
								if (parentBounds.Contains(applyRect.Left + x, applyRect.Top + y) ^ invert) {
									bbbDest.SetColorAt(x, y, nullColor);
								}
							} else {
								int alpha = (int)(aSum / waSum);
								int blue = (int)(bSum / wcSum);
								int green = (int)(gSum / wcSum);
								int red = (int)(rSum / wcSum);
								if (parentBounds.Contains(applyRect.Left + x, applyRect.Top + y) ^ invert) {
									bbbDest.SetColorAt(x, y, Color.FromArgb(alpha, red, green, blue));
								}
							}
						}
					}
				}
				bbbDest.DrawTo(targetGraphics, applyRect.Location);
			}
		}

		/**
		 * This method fixes the problem that we can't apply a filter outside the target bitmap,
		 * therefor the filtered-bitmap will be shifted if we try to draw it outside the target bitmap.
		 * It will also account for the Invert flag.
		 */
		public static Rectangle CreateIntersectRectangle(Size applySize, Rectangle rect, bool invert) {
			Rectangle myRect;
			if (invert) {
				myRect = new Rectangle(0, 0, applySize.Width, applySize.Height);
			} else {
				Rectangle applyRect = new Rectangle(0, 0, applySize.Width, applySize.Height);
				myRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
				myRect.Intersect(applyRect);
			}
			return myRect;
		}

		/// <summary>
		/// Create a new bitmap where the sourceBitmap has a shadow
		/// </summary>
		/// <param name="sourceBitmap">Bitmap to make a shadow on</param>
		/// <param name="darkness">How dark is the shadow</param>
		/// <param name="shadowSize">Size of the shadow</param>
		/// <param name="targetPixelformat">What pixel format must the returning bitmap have</param>
		/// <param name="offset">How many pixels is the original image moved?</param>
		/// <returns>Bitmap with the shadow, is bigger than the sourceBitmap!!</returns>
		public static Bitmap CreateShadow(Bitmap sourceBitmap, float darkness, int shadowSize, PixelFormat targetPixelformat, out Point offset) {
			Bitmap newImage = new Bitmap(sourceBitmap.Width + (shadowSize * 2), sourceBitmap.Height + (shadowSize * 2), targetPixelformat);

			using (Graphics graphics = Graphics.FromImage(newImage)) {
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

				if (Image.IsAlphaPixelFormat(targetPixelformat)) {
					graphics.Clear(Color.Transparent);
				} else {
					graphics.Clear(Color.White);
				}
				ImageAttributes ia = new ImageAttributes();
				ColorMatrix cm = new ColorMatrix();
				cm.Matrix00 = 0;
				cm.Matrix11 = 0;
				cm.Matrix22 = 0;
				cm.Matrix33 = darkness;
				ia.SetColorMatrix(cm);
				// Draw "shadow" offsetted
				graphics.DrawImage(sourceBitmap, new Rectangle(new Point(shadowSize, shadowSize), sourceBitmap.Size), 0, 0, sourceBitmap.Width, sourceBitmap.Height, GraphicsUnit.Pixel, ia);
				// blur "shadow", apply to whole new image
				Rectangle applyRectangle = new Rectangle(Point.Empty, newImage.Size);
				ApplyBlur(graphics, newImage, applyRectangle, true, shadowSize, 1d, false, applyRectangle);
				// draw original
				offset = new Point(shadowSize - 2, shadowSize - 2);
				graphics.DrawImage(sourceBitmap, offset);
			}
			return newImage;
		}
	}
}
