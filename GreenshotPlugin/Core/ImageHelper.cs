/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
					returnImage = CloneArea(image, cropRectangle, PixelFormat.DontCare);
					image.Dispose();
					image = returnImage;
					return true;
				}
			}
			LOG.Warn("Can't crop a null/zero size image!");
			return false;
		}
		
		/// <summary>
		/// Helper method for the FindAutoCropRectangle
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="colorPoint"></param>
		/// <returns></returns>
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
		
		/// <summary>
		/// Load an image from file
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
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
								fileBitmap = Clone(tmpImage);
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
									fileBitmap = Clone(tmpImage);
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
						fileBitmap = Clone(tmpImage);
					}
				}
			}
			return fileBitmap;
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
			returnImage.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
			using (GraphicsPath path = new GraphicsPath()) {
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

				// Draw the created figure with the original image by using a TextureBrush so we have anti-aliasing
				using (Graphics graphics = Graphics.FromImage(returnImage)) {
					graphics.SmoothingMode = SmoothingMode.HighQuality;
					graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
					graphics.CompositingQuality = CompositingQuality.HighQuality;
					graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
					using (Brush brush = new TextureBrush(sourceBitmap)) {
						// Imporant note: If the target wouldn't be at 0,0 we need to translate-transform!!
						graphics.FillPath(brush, path);
					}
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
		/// Create a new bitmap with the sourceBitmap blurred
		/// </summary>
		/// <param name="sourceBitmap">Source to blur</param>
		/// <param name="applyRect">Area to blur</param>
		/// <param name="useExportQuality">Use best quality</param>
		/// <param name="blurRadius">Radius of the blur</param>
		/// <param name="previewQuality">Quality, use 1d for normal anything less skipps calculations</param>
		/// <param name="invert">true if the blur needs to occur outside of the area</param>
		/// <param name="parentBounds">Rectangle limiting the area when using invert</param>
		public static unsafe Bitmap CreateBlur(Bitmap sourceBitmap, Rectangle applyRect, bool useExportQuality, int blurRadius, double previewQuality, bool invert, Rectangle parentBounds) {
			if (applyRect.Height <= 0 || applyRect.Width <= 0) {
				return null;
			}
			// do nothing when nothing can be done!
			if (blurRadius < 1) {
				return null;
			}

			byte[] nullColor = new byte[] { 255, 255, 255, 255 };
			if (sourceBitmap.PixelFormat == PixelFormat.Format32bppArgb) {
				nullColor = new byte[] { 0, 0, 0, 0 };
			}
			byte[] settingColor = new byte[4];
			byte[] readingColor = new byte[4];

			using (BitmapBuffer bbbDest = new BitmapBuffer(sourceBitmap, applyRect, true)) {
				bbbDest.Lock();
				using (BitmapBuffer bbbSrc = new BitmapBuffer(sourceBitmap, applyRect, false)) {
					bbbSrc.Lock();
					Random rand = new Random();
					unchecked {
						int r = blurRadius;
						int[] w = CreateGaussianBlurRow(r);
						int wlen = w.Length;
						long* waSums = stackalloc long[wlen];
						long* wcSums = stackalloc long[wlen];
						long* aSums = stackalloc long[wlen];
						long* rSums = stackalloc long[wlen];
						long* gSums = stackalloc long[wlen];
						long* bSums = stackalloc long[wlen];
						for (int y = 0; y < applyRect.Height; ++y) {
							long waSum = 0;
							long wcSum = 0;
							long aSum = 0;
							long rSum = 0;
							long gSum = 0;
							long bSum = 0;

							for (int wx = 0; wx < wlen; ++wx) {
								int srcX = wx - r;
								waSums[wx] = 0;
								wcSums[wx] = 0;
								aSums[wx] = 0;
								rSums[wx] = 0;
								gSums[wx] = 0;
								bSums[wx] = 0;

								if (srcX >= 0 && srcX < bbbDest.Width) {
									for (int wy = 0; wy < wlen; ++wy) {
										int srcY = y + wy - r;

										if (srcY >= 0 && srcY < bbbDest.Height) {
											bbbSrc.GetColorIn(srcX, srcY, readingColor);
											int wp = w[wy];

											waSums[wx] += wp;
											wp *= readingColor[0] + (readingColor[0] >> 7);
											wcSums[wx] += wp;
											wp >>= 8;

											aSums[wx] += wp * readingColor[0];
											rSums[wx] += wp * readingColor[1];
											gSums[wx] += wp * readingColor[2];
											bSums[wx] += wp * readingColor[3];
										}
									}

									int wwx = w[wx];
									waSum += wwx * waSums[wx];
									wcSum += wwx * wcSums[wx];
									aSum += wwx * aSums[wx];
									rSum += wwx * rSums[wx];
									gSum += wwx * gSums[wx];
									bSum += wwx * bSums[wx];
								}
							}

							wcSum >>= 8;

							if (parentBounds.Contains(applyRect.Left, applyRect.Top + y) ^ invert) {
								if (waSum == 0 || wcSum == 0) {
									bbbDest.SetUncheckedColorArrayAt(0, y, nullColor);
								} else {
									settingColor[0] = (byte)(aSum / waSum);
									settingColor[1] = (byte)(rSum / wcSum);
									settingColor[2] = (byte)(gSum / wcSum);
									settingColor[3] = (byte)(bSum / wcSum);
									bbbDest.SetUncheckedColorArrayAt(0, y, settingColor);
								}
							}

							for (int x = 1; x < applyRect.Width; ++x) {
								for (int i = 0; i < wlen - 1; ++i) {
									waSums[i] = waSums[i + 1];
									wcSums[i] = wcSums[i + 1];
									aSums[i] = aSums[i + 1];
									rSums[i] = rSums[i + 1];
									gSums[i] = gSums[i + 1];
									bSums[i] = bSums[i + 1];
								}

								waSum = 0;
								wcSum = 0;
								aSum = 0;
								rSum = 0;
								gSum = 0;
								bSum = 0;

								int wx;
								for (wx = 0; wx < wlen - 1; ++wx) {
									long wwx = (long)w[wx];
									waSum += wwx * waSums[wx];
									wcSum += wwx * wcSums[wx];
									aSum += wwx * aSums[wx];
									rSum += wwx * rSums[wx];
									gSum += wwx * gSums[wx];
									bSum += wwx * bSums[wx];
								}

								wx = wlen - 1;

								waSums[wx] = 0;
								wcSums[wx] = 0;
								aSums[wx] = 0;
								rSums[wx] = 0;
								gSums[wx] = 0;
								bSums[wx] = 0;

								int srcX = x + wx - r;

								if (srcX >= 0 && srcX < applyRect.Width) {
									for (int wy = 0; wy < wlen; ++wy) {
										int srcY = y + wy - r;
										// only when in EDIT mode, ignore some pixels depending on preview quality
										if ((useExportQuality || rand.NextDouble() < previewQuality) && srcY >= 0 && srcY < applyRect.Height) {
											int wp = w[wy];
											waSums[wx] += wp;
											bbbSrc.GetColorIn(srcX, srcY, readingColor);
											wp *= readingColor[0] + (readingColor[0] >> 7);
											wcSums[wx] += wp;
											wp >>= 8;

											aSums[wx] += wp * readingColor[0];
											rSums[wx] += wp * readingColor[1];
											gSums[wx] += wp * readingColor[2];
											bSums[wx] += wp * readingColor[3];
										}
									}

									int wr = w[wx];
									waSum += wr * waSums[wx];
									wcSum += wr * wcSums[wx];
									aSum += wr * aSums[wx];
									rSum += wr * rSums[wx];
									gSum += wr * gSums[wx];
									bSum += wr * bSums[wx];
								}

								wcSum >>= 8;
								if (parentBounds.Contains(applyRect.Left + x, applyRect.Top + y) ^ invert) {
									if (waSum == 0 || wcSum == 0) {
										bbbDest.SetUncheckedColorArrayAt(x, y, nullColor);
									} else {
										settingColor[0] = (byte)(aSum / waSum);
										settingColor[1] = (byte)(rSum / wcSum);
										settingColor[2] = (byte)(gSum / wcSum);
										settingColor[3] = (byte)(bSum / wcSum);
										bbbDest.SetUncheckedColorArrayAt(x, y, settingColor);
									}
								}
							}
						}
					}
				}
				bbbDest.Unlock();
				return bbbDest.Bitmap;
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
		public static Bitmap CreateShadow(Image sourceBitmap, float darkness, int shadowSize, Point offset, PixelFormat targetPixelformat) {
			// Create a new "clean" image
			Bitmap newImage = new Bitmap(sourceBitmap.Width + (shadowSize * 2), sourceBitmap.Height + (shadowSize * 2), targetPixelformat);
			newImage.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

			using (Graphics graphics = Graphics.FromImage(newImage)) {
				// Make sure the background color is what we want (transparent or white, depending on the pixel format)
				if (Image.IsAlphaPixelFormat(targetPixelformat)) {
					graphics.Clear(Color.Transparent);
				} else {
					graphics.Clear(Color.White);
				}
				// Make sure we draw with the best quality!
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

				// Draw "shadow" offsetted
				ImageAttributes ia = new ImageAttributes();
				ColorMatrix cm = new ColorMatrix();
				cm.Matrix00 = 0;
				cm.Matrix11 = 0;
				cm.Matrix22 = 0;
				cm.Matrix33 = darkness;
				ia.SetColorMatrix(cm);
				Rectangle shadowRectangle = new Rectangle(new Point(shadowSize, shadowSize), sourceBitmap.Size);
				graphics.DrawImage(sourceBitmap, shadowRectangle, 0, 0, sourceBitmap.Width, sourceBitmap.Height, GraphicsUnit.Pixel, ia);

				
				// Only do the blur on the edges
				//Rectangle blurRectangle = new Rectangle(shadowSize + 30, shadowSize + 30, sourceBitmap.Width - 60, sourceBitmap.Height - 60);
				//Rectangle applyRect = ImageHelper.CreateIntersectRectangle(newImage.Size, blurRectangle, true);
				//LOG.DebugFormat("blurRect = {0} - applyRect = {1}", blurRectangle, applyRect);
				//using (Bitmap blurImage = ImageHelper.CreateBlur(newImage, applyRect, true, shadowSize, 1d, true, blurRectangle)) {
				//    if (blurImage != null) {
				//        graphics.DrawImageUnscaled(blurImage, applyRect.Location);
				//    }
				//}
				// blur "shadow", apply to whole new image
				Rectangle newImageRectangle = new Rectangle(0, 0, newImage.Width, newImage.Height);
				using (Bitmap blurImage = CreateBlur(newImage, newImageRectangle, true, shadowSize, 1d, false, newImageRectangle)) {
					graphics.DrawImageUnscaled(blurImage, newImageRectangle.Location);
				}

				// draw original with a TextureBrush so we have nice antialiasing!
				using (Brush textureBrush = new TextureBrush(sourceBitmap, WrapMode.Clamp)) {
					// We need to do a translate-tranform otherwise the image is wrapped
					graphics.TranslateTransform(offset.X, offset.Y);
					graphics.FillRectangle(textureBrush, 0, 0, sourceBitmap.Width, sourceBitmap.Height);
				}
			}
			return newImage;
		}

		/// <summary>
		/// Create a new bitmap where the sourceBitmap has a Simple border around it
		/// </summary>
		/// <param name="sourceBitmap">Bitmap to make a border on</param>
		/// <param name="borderSize">Size of the border</param>
		/// <param name="borderColor">Color of the border</param>
		/// <param name="targetPixelformat">What pixel format must the returning bitmap have</param>
		/// <param name="offset">How many pixels is the original image moved?</param>
		/// <returns>Bitmap with the shadow, is bigger than the sourceBitmap!!</returns>
		public static Bitmap CreateBorder(Bitmap sourceBitmap, int borderSize, Color borderColor, PixelFormat targetPixelformat, out Point offset) {
			// "return" the shifted offset, so the caller can e.g. move elements
			offset = new Point(borderSize, borderSize);

			// Create a new "clean" image
			Bitmap newImage = new Bitmap(sourceBitmap.Width + (borderSize * 2), sourceBitmap.Height + (borderSize * 2), targetPixelformat);
			newImage.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
			using (Graphics graphics = Graphics.FromImage(newImage)) {
				// Make sure the background color is what we want (transparent or white, depending on the pixel format)
				if (Image.IsAlphaPixelFormat(targetPixelformat)) {
					graphics.Clear(Color.Transparent);
				} else {
					graphics.Clear(Color.White);
				}
				// Make sure we draw with the best quality!
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				using (GraphicsPath path = new GraphicsPath()) {
					path.AddRectangle(new Rectangle(borderSize >> 1, borderSize >> 1, newImage.Width - (borderSize), newImage.Height - (borderSize)));
					using (Pen pen = new Pen(borderColor, borderSize)) {
						pen.LineJoin = LineJoin.Round;
						pen.StartCap = LineCap.Round;
						pen.EndCap = LineCap.Round;
						graphics.DrawPath(pen, path);
					}
				}
				// draw original with a TextureBrush so we have nice antialiasing!
				using (Brush textureBrush = new TextureBrush(sourceBitmap, WrapMode.Clamp)) {
					// We need to do a translate-tranform otherwise the image is wrapped
					graphics.TranslateTransform(offset.X, offset.Y);
					graphics.FillRectangle(textureBrush, 0, 0, sourceBitmap.Width, sourceBitmap.Height);
				}
			}
			return newImage;
		}

		/// <summary>
		/// Create a new bitmap where the sourceBitmap is in grayscale
		/// </summary>
		/// <param name="sourceBitmap">Original bitmap</param>
		/// <returns>Bitmap with grayscale</returns>
		public static Bitmap CreateGrayscale(Bitmap sourceBitmap) {
			//create a blank bitmap the same size as original
			Bitmap newBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);

			//get a graphics object from the new image
			using (Graphics graphics = Graphics.FromImage(newBitmap)) {
				// create the grayscale ColorMatrix
				ColorMatrix colorMatrix = new ColorMatrix(
				   new float[][] {
					   new float[] {.3f, .3f, .3f, 0, 0},
						new float[] {.59f, .59f, .59f, 0, 0},
						new float[] {.11f, .11f, .11f, 0, 0},
						new float[] {0, 0, 0, 1, 0},
						new float[] {0, 0, 0, 0, 1}
					});

				//create some image attributes
				ImageAttributes attributes = new ImageAttributes();

				//set the color matrix attribute
				attributes.SetColorMatrix(colorMatrix);

				//draw the original image on the new image using the grayscale color matrix
				graphics.DrawImage(sourceBitmap, new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), 0, 0, sourceBitmap.Width, sourceBitmap.Height, GraphicsUnit.Pixel, attributes);
			}

			return newBitmap;
		}

		/// <summary>
		/// Checks if the supplied Bitmap has a PixelFormat we support
		/// </summary>
		/// <param name="bitmap">bitmap to check</param>
		/// <returns>bool if we support it</returns>
		public static bool SupportsPixelFormat(Bitmap bitmap) {
			return SupportsPixelFormat(bitmap.PixelFormat);
		}

		/// <summary>
		/// Checks if we support the pixel format
		/// </summary>
		/// <param name="pixelformat">PixelFormat to check</param>
		/// <returns>bool if we support it</returns>
		public static bool SupportsPixelFormat(PixelFormat pixelformat) {
			return (pixelformat.Equals(PixelFormat.Format32bppArgb) ||
					pixelformat.Equals(PixelFormat.Format32bppRgb) ||
					pixelformat.Equals(PixelFormat.Format24bppRgb));
		}

		/// <summary>
		/// Wrapper for just cloning which calls the CloneArea
		/// </summary>
		/// <param name="sourceBitmap">Image to clone</param>
		/// <returns>Bitmap with clone image data</returns>
		public static Bitmap Clone(Image sourceBitmap) {
			return CloneArea(sourceBitmap, Rectangle.Empty, PixelFormat.DontCare);
		}

		/// <summary>
		/// Clone an image, taking some rules into account:
		/// 1) When sourceRect is the whole bitmap there is a GDI+ bug in Clone
		///		Clone will than return the same PixelFormat as the source
		///		a quick workaround is using new Bitmap which uses a default of Format32bppArgb
		///	2) When going from a transparent to a non transparent bitmap, we draw the background white!
		/// </summary>
		/// <param name="sourceBitmap">Source bitmap to clone</param>
		/// <param name="sourceRect">Rectangle to copy from the source, use Rectangle.Empty for all</param>
		/// <param name="targetFormat">Target Format, use PixelFormat.DontCare if you want the original (or a default if the source PixelFormat is not supported)</param>
		/// <returns></returns>
		public static Bitmap CloneArea(Image sourceBitmap, Rectangle sourceRect, PixelFormat targetFormat) {
			Bitmap newImage = null;
			Rectangle bitmapRect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);

			// Make sure the source is not Rectangle.Empty
			if (Rectangle.Empty.Equals(sourceRect)) {
				sourceRect = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);
			}

			// If no pixelformat is supplied 
			if (PixelFormat.DontCare == targetFormat || PixelFormat.Undefined == targetFormat) {
				if (SupportsPixelFormat(sourceBitmap.PixelFormat)) {
					targetFormat = sourceBitmap.PixelFormat;
				} else if (Image.IsAlphaPixelFormat(sourceBitmap.PixelFormat)) {
					targetFormat = PixelFormat.Format32bppArgb;
				} else {
					targetFormat = PixelFormat.Format24bppRgb;
				}
			}

			// check the target format
			if (!SupportsPixelFormat(targetFormat)) {
				if (Image.IsAlphaPixelFormat(targetFormat)) {
					targetFormat = PixelFormat.Format32bppArgb;
				} else {
					targetFormat = PixelFormat.Format24bppRgb;
				}
			}

			bool destinationIsTransparent = Image.IsAlphaPixelFormat(targetFormat);
			bool sourceIsTransparent = Image.IsAlphaPixelFormat(sourceBitmap.PixelFormat);
			bool fromTransparentToNon = !destinationIsTransparent && sourceIsTransparent;
			bool isBitmap = sourceBitmap is Bitmap;
			bool isAreaEqual = sourceRect.Equals(bitmapRect);
			if (isAreaEqual || fromTransparentToNon || !isBitmap) {
				// Rule 1: if the areas are equal, always copy ourselves
				newImage = new Bitmap(bitmapRect.Width, bitmapRect.Height, targetFormat);
				using (Graphics graphics = Graphics.FromImage(newImage)) {
					if (fromTransparentToNon) {
						// Rule 2: Make sure the background color is white
						graphics.Clear(Color.White);
					}
					// decide fastest copy method
					if (isAreaEqual) {
						graphics.DrawImageUnscaled(sourceBitmap, 0, 0);
					} else {
						graphics.DrawImage(sourceBitmap, 0, 0, sourceRect, GraphicsUnit.Pixel);
					}
				}
			} else {
				// Let GDI+ decide how to convert, need to test what is quicker...
				newImage = (sourceBitmap as Bitmap).Clone(sourceRect, targetFormat);
			}
			// Make sure both images have the same resolution
			newImage.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
			return newImage;
		}
	}
}
