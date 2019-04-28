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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Greenshot.Gfx.Effects;
using Greenshot.Gfx.Extensions;
using Greenshot.Gfx.FastBitmap;
using Greenshot.Gfx.Structs;

namespace Greenshot.Gfx
{
    /// <summary>
    ///     The BitmapHelper contains extensions for Bitmaps
    /// </summary>
    public static class BitmapHelper
	{
		private const int ExifOrientationId = 0x0112;
		private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// A function which usage image.fromstream
        /// </summary>
        public static readonly Func<Stream, string, IBitmapWithNativeSupport> FromStreamReader = (stream, s) =>
	    {
	        using (var tmpImage = Image.FromStream(stream, true, true))
	        {
	            if (!(tmpImage is Bitmap bitmap))
	            {
	                return null;
	            }
	            Log.Debug().WriteLine("Loaded bitmap with Size {0}x{1} and PixelFormat {2}", bitmap.Width, bitmap.Height, bitmap.PixelFormat);
	            return bitmap.CloneBitmap(PixelFormat.Format32bppArgb);
	        }
	    };

        static BitmapHelper()
		{
			// Fallback
			StreamConverters[""] = FromStreamReader;

			StreamConverters["gif"] = FromStreamReader;
			StreamConverters["bmp"] = FromStreamReader;
			StreamConverters["jpg"] = FromStreamReader;
			StreamConverters["jpeg"] = FromStreamReader;
			StreamConverters["png"] = FromStreamReader;
			StreamConverters["wmf"] = FromStreamReader;
			StreamConverters["svg"] = (stream, s) =>
			{
				stream.Position = 0;
				try
				{
					return SvgBitmap.FromStream(stream);
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Can't load SVG");
				}
				return null;
			};

			StreamConverters["ico"] = (stream, extension) =>
			{
				// Icon logic, try to get the Vista icon, else the biggest possible
				try
				{
					using (var tmpBitmap = stream.ExtractVistaIcon())
					{
						if (tmpBitmap != null)
						{
							return tmpBitmap.CloneBitmap(PixelFormat.Format32bppArgb);
						}
					}
				}
				catch (Exception vistaIconException)
				{
					Log.Warn().WriteLine(vistaIconException, "Can't read icon");
				}
				try
				{
					// No vista icon, try normal icon
					stream.Position = 0;
					// We create a copy of the bitmap, so everything else can be disposed
					using (var tmpIcon = new Icon(stream, new Size(1024, 1024)))
					{
						using (var tmpImage = tmpIcon.ToBitmap())
						{
							return tmpImage.CloneBitmap(PixelFormat.Format32bppArgb);
						}
					}
				}
				catch (Exception iconException)
				{
					Log.Warn().WriteLine(iconException, "Can't read icon");
				}

				stream.Position = 0;
				return FromStreamReader(stream, extension);
			};
		}

        /// <summary>
        /// This defines all available bitmap reader functions, registered to an "extension" is called with a stream to a IBitmap. 
        /// </summary>
		public static IDictionary<string, Func<Stream, string, IBitmapWithNativeSupport>> StreamConverters { get; } = new Dictionary<string, Func<Stream, string, IBitmapWithNativeSupport>>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Make sure the image is orientated correctly
		/// </summary>
		/// <param name="image">Image</param>
		public static void Orientate(this Image image)
		{
			try
			{
				// Get the index of the orientation property.
				var orientationIndex = Array.IndexOf(image.PropertyIdList, ExifOrientationId);
				// If there is no such property, return Unknown.
				if (orientationIndex < 0)
				{
					return;
				}
				var item = image.GetPropertyItem(ExifOrientationId);

				var orientation = (ExifOrientations) item.Value[0];
				// Orient the image.
				switch (orientation)
				{
					case ExifOrientations.Unknown:
					case ExifOrientations.TopLeft:
						break;
					case ExifOrientations.TopRight:
						image.RotateFlip(RotateFlipType.RotateNoneFlipX);
						break;
					case ExifOrientations.BottomRight:
						image.RotateFlip(RotateFlipType.Rotate180FlipNone);
						break;
					case ExifOrientations.BottomLeft:
						image.RotateFlip(RotateFlipType.RotateNoneFlipY);
						break;
					case ExifOrientations.LeftTop:
						image.RotateFlip(RotateFlipType.Rotate90FlipX);
						break;
					case ExifOrientations.RightTop:
						image.RotateFlip(RotateFlipType.Rotate90FlipNone);
						break;
					case ExifOrientations.RightBottom:
						image.RotateFlip(RotateFlipType.Rotate90FlipY);
						break;
					case ExifOrientations.LeftBottom:
						image.RotateFlip(RotateFlipType.Rotate270FlipNone);
						break;
				}
				// Set the orientation to be normal, as we rotated the image.
				item.Value[0] = (byte) ExifOrientations.TopLeft;
				image.SetPropertyItem(item);
			}
			catch (Exception orientEx)
			{
				Log.Warn().WriteLine(orientEx, "Problem orientating the image: ");
			}
		}

        /// <summary>
        ///     Create a Thumbnail
        /// </summary>
        /// <param name="image">Image</param>
        /// <param name="thumbWidth">int</param>
        /// <param name="thumbHeight">int</param>
        /// <param name="maxWidth">int</param>
        /// <param name="maxHeight">int</param>
        /// <returns></returns>
        public static IBitmapWithNativeSupport CreateThumbnail(this IBitmapWithNativeSupport image, int thumbWidth, int thumbHeight, int maxWidth = -1, int maxHeight = -1)
		{
			var srcWidth = image.Width;
			var srcHeight = image.Height;
			if (thumbHeight < 0)
			{
				thumbHeight = (int) (thumbWidth * (srcHeight / (float) srcWidth));
			}
			if (thumbWidth < 0)
			{
				thumbWidth = (int) (thumbHeight * (srcWidth / (float) srcHeight));
			}
			if (maxWidth > 0 && thumbWidth > maxWidth)
			{
				thumbWidth = Math.Min(thumbWidth, maxWidth);
				thumbHeight = (int) (thumbWidth * (srcHeight / (float) srcWidth));
			}
			if (maxHeight > 0 && thumbHeight > maxHeight)
			{
				thumbHeight = Math.Min(thumbHeight, maxHeight);
				thumbWidth = (int) (thumbHeight * (srcWidth / (float) srcHeight));
			}

            IBitmapWithNativeSupport bmp = new UnmanagedBitmap<Bgra32>(thumbWidth, thumbHeight, image.HorizontalResolution, image.VerticalResolution);
			using (var graphics = Graphics.FromImage(bmp.NativeBitmap))
			{
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				var rectDestination = new NativeRect(0, 0, thumbWidth, thumbHeight);
				graphics.DrawImage(image.NativeBitmap, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);
			}
			return bmp;
		}

        /// <summary>
        ///     Crops the image to the specified rectangle
        /// </summary>
        /// <param name="bitmap">Bitmap to crop</param>
        /// <param name="cropRectangle">NativeRect with bitmap coordinates, will be "intersected" to the bitmap</param>
        public static bool Crop(ref IBitmapWithNativeSupport bitmap, ref NativeRect cropRectangle)
		{
			if (bitmap.Width * bitmap.Height > 0)
			{
			    cropRectangle = cropRectangle.Intersect(new NativeRect(0, 0, bitmap.Width, bitmap.Height));
				if (cropRectangle.Width != 0 || cropRectangle.Height != 0)
				{
					var returnImage = bitmap.CloneBitmap(PixelFormat.DontCare, cropRectangle);
					bitmap.Dispose();
					bitmap = returnImage;
					return true;
				}
			}
			Log.Warn().WriteLine("Can't crop a null/zero size image!");
			return false;
		}

        /// <summary>
        ///     Private helper method for the FindAutoCropRectangle
        /// </summary>
        /// <param name="fastBitmap">IFastBitmap</param>
        /// <param name="referenceColor">color for reference</param>
        /// <param name="cropDifference">int</param>
        /// <returns>NativeRect</returns>
        private static NativeRect FindAutoCropRectangle(this IFastBitmap fastBitmap, Color referenceColor, int cropDifference)
		{
			var cropRectangle = NativeRect.Empty;
			var min = new NativePoint(int.MaxValue, int.MaxValue);
			var max = new NativePoint(int.MinValue, int.MinValue);

			if (cropDifference > 0)
			{
				for (var y = 0; y < fastBitmap.Height; y++)
				{
					for (var x = 0; x < fastBitmap.Width; x++)
					{
						var currentColor = fastBitmap.GetColorAt(x, y);
						var diffR = Math.Abs(currentColor.R - referenceColor.R);
						var diffG = Math.Abs(currentColor.G - referenceColor.G);
						var diffB = Math.Abs(currentColor.B - referenceColor.B);
						if ((diffR + diffG + diffB) / 3 <= cropDifference)
						{
							continue;
						}
						if (x < min.X)
						{
							min = min.ChangeX(x);
						}
						if (y < min.Y)
						{
						    min = min.ChangeY(y);
                        }
						if (x > max.X)
						{
							max = max.ChangeX(x);
						}
						if (y > max.Y)
						{
						    max = max.ChangeY(y);
                        }
					}
				}
			}
			else
			{
				for (var y = 0; y < fastBitmap.Height; y++)
				{
					for (var x = 0; x < fastBitmap.Width; x++)
					{
						var currentColor = fastBitmap.GetColorAt(x, y);
						if (!referenceColor.Equals(currentColor))
						{
							continue;
						}
						if (x < min.X)
						{
						    min = min.ChangeX(x);
                        }
						if (y < min.Y)
						{
						    min = min.ChangeY(y);
                        }
						if (x > max.X)
						{
							max = max.ChangeX(x);
						}
						if (y > max.Y)
						{
						    max = max.ChangeY(y);
                        }
					}
				}
			}

		    if (!(NativePoint.Empty.Equals(min) && max.Equals(new NativePoint(fastBitmap.Width - 1, fastBitmap.Height - 1))) &&
		        !(min.X == int.MaxValue || min.Y == int.MaxValue || max.X == int.MinValue || min.X == int.MinValue))
		    {
		        cropRectangle = new NativeRect(min.X, min.Y, max.X - min.X + 1, max.Y - min.Y + 1);
		    }
		    return cropRectangle;
		}

		/// <summary>
		///     Get a rectangle for the image which crops the image of all colors equal to that on 0,0
		/// </summary>
		/// <param name="image"></param>
		/// <param name="cropDifference"></param>
		/// <returns>NativeRect</returns>
		public static NativeRect FindAutoCropRectangle(this IBitmapWithNativeSupport image, int cropDifference)
		{
			var cropRectangle = NativeRect.Empty;
			var checkPoints = new List<NativePoint>
			{
				new NativePoint(0, 0),
				new NativePoint(0, image.Height - 1),
				new NativePoint(image.Width - 1, 0),
				new NativePoint(image.Width - 1, image.Height - 1)
			};
			// Top Left
			// Bottom Left
			// Top Right
			// Bottom Right
			using (var fastBitmap = FastBitmapFactory.Create(image))
			{
				// find biggest area
				foreach (var checkPoint in checkPoints)
				{
					var currentRectangle = fastBitmap.FindAutoCropRectangle(fastBitmap.GetColorAt(checkPoint.X, checkPoint.Y), cropDifference);
					if (currentRectangle.Width * currentRectangle.Height > cropRectangle.Width * cropRectangle.Height)
					{
						cropRectangle = currentRectangle;
					}
				}
			}
			return cropRectangle;
		}

		/// <summary>
		///     Load an image from file
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static IBitmapWithNativeSupport LoadBitmap(string filename)
		{
			if (string.IsNullOrEmpty(filename))
			{
				return null;
			}
			if (!File.Exists(filename))
			{
				return null;
			}
            IBitmapWithNativeSupport fileBitmap;
			Log.Info().WriteLine("Loading image from file {0}", filename);
			// Fixed lock problem Bug #3431881
			using (Stream imageFileStream = File.OpenRead(filename))
			{
				fileBitmap = FromStream(imageFileStream, Path.GetExtension(filename));
			}
			if (fileBitmap != null)
			{
				Log.Info().WriteLine("Information about file {0}: {1}x{2}-{3} Resolution {4}x{5}", filename, fileBitmap.Width, fileBitmap.Height, fileBitmap.PixelFormat,
					fileBitmap.HorizontalResolution, fileBitmap.VerticalResolution);
			}
			return fileBitmap;
		}

		/// <summary>
		///     Based on: http://www.codeproject.com/KB/cs/IconExtractor.aspx
		///     And a hint from: http://www.codeproject.com/KB/cs/IconLib.aspx
		/// </summary>
		/// <param name="iconStream">Stream with the icon information</param>
		/// <returns>Bitmap with the Vista Icon (256x256)</returns>
		private static IBitmapWithNativeSupport ExtractVistaIcon(this Stream iconStream)
		{
			const int sizeIconDir = 6;
			const int sizeIconDirEntry = 16;
            IBitmapWithNativeSupport bmpPngExtracted = null;
			try
			{
				var srcBuf = new byte[iconStream.Length];
				iconStream.Read(srcBuf, 0, (int) iconStream.Length);
				int iCount = BitConverter.ToInt16(srcBuf, 4);
				for (var iIndex = 0; iIndex < iCount; iIndex++)
				{
					int iWidth = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex];
					int iHeight = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex + 1];
				    if (iWidth != 0 || iHeight != 0)
				    {
				        continue;
				    }
				    var iImageSize = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 8);
				    var iImageOffset = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 12);
				    using (var destStream = new MemoryStream())
				    {
				        destStream.Write(srcBuf, iImageOffset, iImageSize);
				        destStream.Seek(0, SeekOrigin.Begin);
				        bmpPngExtracted = BitmapWrapper.FromBitmap(new Bitmap(destStream)); // This is PNG! :)
				    }
				    break;
				}
			}
			catch
			{
				return null;
			}
			return bmpPngExtracted;
		}

		/// <summary>
		///     Apply the effect to the bitmap
		/// </summary>
		/// <param name="sourceBitmap">Bitmap</param>
		/// <param name="effect">IEffect</param>
		/// <param name="matrix">Matrix</param>
		/// <returns>Bitmap</returns>
		public static IBitmapWithNativeSupport ApplyEffect(this IBitmapWithNativeSupport sourceBitmap, IEffect effect, Matrix matrix)
		{
			var effects = new List<IEffect> {effect};
			return sourceBitmap.ApplyEffects(effects, matrix);
		}

	    /// <summary>
	    ///     Apply the effects in the supplied order to the bitmap
	    /// </summary>
	    /// <param name="sourceBitmap"></param>
	    /// <param name="effects">List of IEffect</param>
	    /// <param name="matrix">Matrix</param>
	    /// <returns>Bitmap</returns>
	    public static IBitmapWithNativeSupport ApplyEffects(this IBitmapWithNativeSupport sourceBitmap, IEnumerable<IEffect> effects, Matrix matrix)
		{
			var currentImage = sourceBitmap;
			var disposeImage = false;
			foreach (var effect in effects)
			{
				var tmpImage = effect.Apply(currentImage, matrix);
			    if (tmpImage == null)
			    {
			        continue;
			    }
			    if (disposeImage)
			    {
			        currentImage.Dispose();
			    }
			    currentImage = tmpImage;
			    // Make sure the "new" image is disposed
			    disposeImage = true;
			}
			return currentImage;
		}

		/// <summary>
		///     This method fixes the problem that we can't apply a filter outside the target bitmap,
		///     therefor the filtered-bitmap will be shifted if we try to draw it outside the target bitmap.
		///     It will also account for the Invert flag.
		/// </summary>
		/// <param name="applySize"></param>
		/// <param name="rect"></param>
		/// <param name="invert"></param>
		/// <returns></returns>
		public static NativeRect CreateIntersectRectangle(Size applySize, NativeRect rect, bool invert)
		{
			NativeRect myRect;
			if (invert)
			{
				myRect = new NativeRect(0, 0, applySize.Width, applySize.Height);
			}
			else
			{
				var applyRect = new NativeRect(0, 0, applySize.Width, applySize.Height);
				myRect = new NativeRect(rect.X, rect.Y, rect.Width, rect.Height).Intersect(applyRect);
			}
			return myRect;
		}

		/// <summary>
		///     Create a new bitmap where the sourceBitmap has a shadow
		/// </summary>
		/// <param name="sourceBitmap">Bitmap to make a shadow on</param>
		/// <param name="darkness">How dark is the shadow</param>
		/// <param name="shadowSize">Size of the shadow</param>
		/// <param name="targetPixelformat">What pixel format must the returning bitmap have</param>
		/// <param name="shadowOffset"></param>
		/// <param name="matrix">
		///     The transform matrix which describes how the elements need to be transformed to stay at the same
		///     location
		/// </param>
		/// <returns>Bitmap with the shadow, is bigger than the sourceBitmap!!</returns>
		public static IBitmapWithNativeSupport CreateShadow(this IBitmapWithNativeSupport sourceBitmap, float darkness, int shadowSize, NativePoint shadowOffset, Matrix matrix, PixelFormat targetPixelformat)
		{
		    var offset = shadowOffset.Offset(shadowSize - 1, shadowSize - 1);
			matrix.Translate(offset.X, offset.Y, MatrixOrder.Append);
			// Create a new "clean" image
			var returnImage = BitmapFactory.CreateEmpty(sourceBitmap.Width + shadowSize * 2, sourceBitmap.Height + shadowSize * 2, targetPixelformat, Color.Empty,
				sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
			// Make sure the shadow is odd, there is no reason for an even blur!
			if ((shadowSize & 1) == 0)
			{
				shadowSize++;
			}
			// Create "mask" for the shadow
			var maskMatrix = new ColorMatrix
			{
				Matrix00 = 0,
				Matrix11 = 0,
				Matrix22 = 0,
                Matrix33 = darkness
			};
			
			var shadowRectangle = new NativeRect(new NativePoint(shadowSize, shadowSize), sourceBitmap.Size);
			ApplyColorMatrix(sourceBitmap, NativeRect.Empty, returnImage, shadowRectangle, maskMatrix);

			// blur "shadow", apply to whole new image

			// try normal software blur
			returnImage.ApplyBoxBlur(shadowSize);

			// Draw the original image over the shadow
			using (var graphics = Graphics.FromImage(sourceBitmap.NativeBitmap))
			{
				// Make sure we draw with the best quality!
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				// draw original with a TextureBrush so we have nice anti-aliasing!
				using (Brush textureBrush = new TextureBrush(sourceBitmap.NativeBitmap, WrapMode.Clamp))
				{
					// We need to do a translate-transform otherwise the image is wrapped
					graphics.TranslateTransform(offset.X, offset.Y);
					graphics.FillRectangle(textureBrush, 0, 0, sourceBitmap.Width, sourceBitmap.Height);
				}
			}
			return returnImage;
		}

		/// <summary>
		///     Apply a color matrix to the image
		/// </summary>
		/// <param name="source">Image to apply matrix to</param>
		/// <param name="colorMatrix">ColorMatrix to apply</param>
		public static void ApplyColorMatrix(this IBitmapWithNativeSupport source, ColorMatrix colorMatrix)
		{
			source.ApplyColorMatrix(NativeRect.Empty, source, NativeRect.Empty, colorMatrix);
		}

		/// <summary>
		///     Apply a color matrix by copying from the source to the destination
		/// </summary>
		/// <param name="source">Image to copy from</param>
		/// <param name="sourceRect">NativeRect to copy from</param>
		/// <param name="destRect">NativeRect to copy to</param>
		/// <param name="dest">Image to copy to</param>
		/// <param name="colorMatrix">ColorMatrix to apply</param>
		public static void ApplyColorMatrix(this IBitmapWithNativeSupport source, NativeRect sourceRect, IBitmapWithNativeSupport dest, NativeRect destRect, ColorMatrix colorMatrix)
		{
			using (var imageAttributes = new ImageAttributes())
			{
				imageAttributes.ClearColorMatrix();
				imageAttributes.SetColorMatrix(colorMatrix);
                source.ApplyImageAttributes(sourceRect, dest, destRect, imageAttributes);
			}
		}

		/// <summary>
		///     Apply image attributes to the image
		/// </summary>
		/// <param name="source">Image to apply matrix to</param>
		/// <param name="imageAttributes">ImageAttributes to apply</param>
		public static void ApplyColorMatrix(this IBitmapWithNativeSupport source, ImageAttributes imageAttributes)
		{
            source.ApplyImageAttributes(NativeRect.Empty, source, NativeRect.Empty, imageAttributes);
		}

		/// <summary>
		///     Apply a color matrix by copying from the source to the destination
		/// </summary>
		/// <param name="source">Image to copy from</param>
		/// <param name="sourceRect">NativeRect to copy from</param>
		/// <param name="destRect">NativeRect to copy to</param>
		/// <param name="dest">Image to copy to</param>
		/// <param name="imageAttributes">ImageAttributes to apply</param>
		public static void ApplyImageAttributes(this IBitmapWithNativeSupport source, NativeRect sourceRect, IBitmapWithNativeSupport dest, NativeRect destRect, ImageAttributes imageAttributes)
		{
			if (sourceRect == NativeRect.Empty)
			{
				sourceRect = new NativeRect(0, 0, source.Width, source.Height);
			}
			if (dest == null)
			{
				dest = source;
			}
			if (destRect == NativeRect.Empty)
			{
				destRect = new NativeRect(0, 0, dest.Width, dest.Height);
			}
			using (var graphics = Graphics.FromImage(dest.NativeBitmap))
			{
				// Make sure we draw with the best quality!
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.CompositingMode = CompositingMode.SourceCopy;

				graphics.DrawImage(source.NativeBitmap, destRect, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, GraphicsUnit.Pixel, imageAttributes);
			}
		}

		/// <summary>
		///     Checks if the supplied Bitmap has a PixelFormat we support
		/// </summary>
		/// <param name="image">bitmap to check</param>
		/// <returns>bool if we support it</returns>
		public static bool IsPixelFormatSupported(this IBitmapWithNativeSupport image)
		{
			return image.PixelFormat.IsPixelFormatSupported();
		}

		/// <summary>
		///     Checks if we support the pixel format
		/// </summary>
		/// <param name="pixelformat">PixelFormat to check</param>
		/// <returns>bool if we support it</returns>
		public static bool IsPixelFormatSupported(this PixelFormat pixelformat)
		{
			return pixelformat.Equals(PixelFormat.Format32bppArgb) ||
			       pixelformat.Equals(PixelFormat.Format32bppPArgb) ||
			       pixelformat.Equals(PixelFormat.Format32bppRgb) ||
			       pixelformat.Equals(PixelFormat.Format24bppRgb);
		}


		/// <summary>
		///     Rotate the bitmap
		/// </summary>
		/// <param name="sourceBitmap">Image</param>
		/// <param name="rotateFlipType">RotateFlipType</param>
		/// <returns>Image</returns>
		public static IBitmapWithNativeSupport ApplyRotateFlip(this IBitmapWithNativeSupport sourceBitmap, RotateFlipType rotateFlipType)
		{
			var returnImage = sourceBitmap.CloneBitmap();
			returnImage.NativeBitmap.RotateFlip(rotateFlipType);
			return returnImage;
		}


        /// <summary>
        ///     Get a scaled version of the sourceBitmap
        /// </summary>
        /// <param name="sourceImage">Image</param>
        /// <param name="percent">1-99 to make smaller, use 101 and more to make the picture bigger</param>
        /// <returns>Bitmap</returns>
        public static IBitmapWithNativeSupport ScaleByPercent(this IBitmapWithNativeSupport sourceImage, int percent)
		{
			var nPercent = (float) percent / 100;

			var sourceWidth = sourceImage.Width;
			var sourceHeight = sourceImage.Height;
			var destWidth = (int) (sourceWidth * nPercent);
			var destHeight = (int) (sourceHeight * nPercent);

			var scaledBitmap = BitmapFactory.CreateEmpty(destWidth, destHeight, sourceImage.PixelFormat, Color.Empty, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
			using (var graphics = Graphics.FromImage(scaledBitmap.NativeBitmap))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(sourceImage.NativeBitmap, new NativeRect(0, 0, destWidth, destHeight), new NativeRect(0, 0, sourceWidth, sourceHeight), GraphicsUnit.Pixel);
			}
			return scaledBitmap;
		}

        /// <summary>
        ///     Resize canvas with pixel to the left, right, top and bottom
        /// </summary>
        /// <param name="sourceImage">Image</param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <param name="left">int</param>
        /// <param name="right">int</param>
        /// <param name="top">int</param>
        /// <param name="bottom">int</param>
        /// <param name="matrix">Matrix</param>
        /// <returns>a new bitmap with the source copied on it</returns>
        public static IBitmapWithNativeSupport ResizeCanvas(this IBitmapWithNativeSupport sourceImage, Color backgroundColor, int left, int right, int top, int bottom, Matrix matrix)
		{
			matrix.Translate(left, top, MatrixOrder.Append);
			var newBitmap = BitmapFactory.CreateEmpty(sourceImage.Width + left + right, sourceImage.Height + top + bottom, sourceImage.PixelFormat, backgroundColor,
				sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
			using (var graphics = Graphics.FromImage(newBitmap.NativeBitmap))
			{
				graphics.DrawImageUnscaled(sourceImage.NativeBitmap, left, top);
			}
			return newBitmap;
		}

        /// <summary>
        ///     Wrapper for the more complex Resize, this resize could be used for e.g. Thumbnails
        /// </summary>
        /// <param name="sourceImage">Image</param>
        /// <param name="maintainAspectRatio">true to maintain the aspect ratio</param>
        /// <param name="newWidth">int</param>
        /// <param name="newHeight">int</param>
        /// <param name="matrix">Matrix</param>
        /// <param name="interpolationMode">InterpolationMode</param>
        /// <returns>Image</returns>
        public static IBitmapWithNativeSupport Resize(this IBitmapWithNativeSupport sourceImage, bool maintainAspectRatio, int newWidth, int newHeight, Matrix matrix = null, InterpolationMode interpolationMode = InterpolationMode.HighQualityBicubic)
		{
			return Resize(sourceImage, maintainAspectRatio, false, Color.Empty, newWidth, newHeight, matrix, interpolationMode);
		}

        /// <summary>
        ///     Scale the bitmap, keeping aspect ratio, but the canvas will always have the specified size.
        /// </summary>
        /// <param name="sourceImage">Image to scale</param>
        /// <param name="maintainAspectRatio">true to maintain the aspect ratio</param>
        /// <param name="canvasUseNewSize">Makes the image maintain aspect ratio, but the canvas get's the specified size</param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <param name="newWidth">new width</param>
        /// <param name="newHeight">new height</param>
        /// <param name="matrix">Matrix</param>
        /// <param name="interpolationMode">InterpolationMode</param>
        /// <returns>a new bitmap with the specified size, the source-Image scaled to fit with aspect ratio locked</returns>
        public static IBitmapWithNativeSupport Resize(this IBitmapWithNativeSupport sourceImage, bool maintainAspectRatio, bool canvasUseNewSize, Color backgroundColor, int newWidth, int newHeight, Matrix matrix, InterpolationMode interpolationMode = InterpolationMode.HighQualityBicubic)
		{
			var destX = 0;
			var destY = 0;

			var nPercentW = newWidth / (float) sourceImage.Width;
			var nPercentH = newHeight / (float) sourceImage.Height;
			if (maintainAspectRatio)
			{
				if ((int) nPercentW == 1 || (int)nPercentH != 0 && nPercentH < nPercentW)
				{
					nPercentW = nPercentH;
					if (canvasUseNewSize)
					{
						destX = Math.Max(0, Convert.ToInt32((newWidth - sourceImage.Width * nPercentW) / 2));
					}
				}
				else
				{
					nPercentH = nPercentW;
					if (canvasUseNewSize)
					{
						destY = Math.Max(0, Convert.ToInt32((newHeight - sourceImage.Height * nPercentH) / 2));
					}
				}
			}

			var destWidth = (int) (sourceImage.Width * nPercentW);
			var destHeight = (int) (sourceImage.Height * nPercentH);
			if (newWidth == 0)
			{
				newWidth = destWidth;
			}
			if (newHeight == 0)
			{
				newHeight = destHeight;
			}
            IBitmapWithNativeSupport newBitmap;
			if (maintainAspectRatio && canvasUseNewSize)
			{
				newBitmap = BitmapFactory.CreateEmpty(newWidth, newHeight, sourceImage.PixelFormat, backgroundColor, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
				matrix?.Scale((float) newWidth / sourceImage.Width, (float) newHeight / sourceImage.Height, MatrixOrder.Append);
			}
			else
			{
				newBitmap = BitmapFactory.CreateEmpty(destWidth, destHeight, sourceImage.PixelFormat, backgroundColor, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
				matrix?.Scale((float) destWidth / sourceImage.Width, (float) destHeight / sourceImage.Height, MatrixOrder.Append);
			}

			using (var graphics = Graphics.FromImage(newBitmap.NativeBitmap))
			{
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.InterpolationMode = interpolationMode;
				using (var wrapMode = new ImageAttributes())
				{
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(sourceImage.NativeBitmap, new NativeRect(destX, destY, destWidth, destHeight), 0, 0, sourceImage.Width, sourceImage.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}
			return newBitmap;
		}

	    /// <summary>
	    ///     Count how many times the supplied color exists
	    /// </summary>
	    /// <param name="sourceImage">Image to count the pixels of</param>
	    /// <param name="colorToCount">Color to count</param>
	    /// <param name="includeAlpha">true if Alpha needs to be checked</param>
	    /// <returns>int with the number of pixels which have colorToCount</returns>
	    public static int CountColor(this IBitmapWithNativeSupport sourceImage, Color colorToCount, bool includeAlpha = true)
	    {
            var lockObject = new object();
	        var colors = 0;
	        var toCount = colorToCount.ToArgb();
	        if (!includeAlpha)
	        {
	            toCount = toCount & 0xffffff;
	        }
	        using (var bb = FastBitmapFactory.Create(sourceImage))
	        {
	            Parallel.For(0, bb.Height, () => 0, (y, state, initialColorCount) =>
	            {
	                var currentColors = initialColorCount;
	                for (var x = 0; x < bb.Width; x++)
	                {
	                    var bitmapcolor = bb.GetColorAt(x, y).ToArgb();
	                    if (!includeAlpha)
	                    {
	                        bitmapcolor = bitmapcolor & 0xffffff;
	                    }
	                    if (bitmapcolor == toCount)
	                    {
	                        currentColors++;
	                    }
	                }
                    return currentColors;
	            }, lineColorCount =>
	            {
	                lock (lockObject)
	                {
	                    colors += lineColorCount;
	                }
	            });
                
	            return colors;
	        }
	    }

        /// <summary>
        ///     Create an image from a stream, if an extension is supplied more formats are supported.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="extension"></param>
        /// <returns>Image</returns>
        public static IBitmapWithNativeSupport FromStream(Stream stream, string extension = null)
		{
			if (stream == null)
			{
				return null;
			}
			if (!string.IsNullOrEmpty(extension))
			{
				extension = extension.Replace(".", "");
			}

			// Make sure we can try multiple times
			if (!stream.CanSeek)
			{
				var memoryStream = new MemoryStream();
				stream.CopyTo(memoryStream);
				stream = memoryStream;
			}

            IBitmapWithNativeSupport returnBitmap = null;
		    if (StreamConverters.TryGetValue(extension ?? "", out var converter))
			{
				returnBitmap = converter(stream, extension);
			}
		    if (returnBitmap != null || converter == FromStreamReader)
		    {
		        return returnBitmap;
		    }
		    // Fallback to default converter
		    stream.Position = 0;
		    returnBitmap = FromStreamReader(stream, extension);
		    return returnBitmap;
		}

        /// <summary>
        ///     Prepare an "icon" to be displayed correctly scaled
        /// </summary>
        /// <param name="original">original icon Bitmap</param>
        /// <param name="dpi">double with the dpi value</param>
        /// <param name="baseSize">the base size of the icon, default is 16</param>
        /// <returns>Bitmap</returns>
        public static IBitmapWithNativeSupport ScaleIconForDisplaying(this IBitmapWithNativeSupport original, uint dpi, int baseSize = 16)
		{
			if (original == null)
			{
				return null;
			}
			if (dpi < DpiHandler.DefaultScreenDpi)
			{
				dpi = DpiHandler.DefaultScreenDpi;
			}
			var width = DpiHandler.ScaleWithDpi(baseSize, dpi);
			if (original.Width == width)
			{
				return original;
			}

            
			if (width == original.Width * 2)
			{
				return original.Scale2X();
			}
			if (width == original.Width * 3)
			{
				return original.Scale3X();
			}
			if (width == original.Width * 4)
			{
				using (var scale2X = original.Scale2X())
				{
					return scale2X.Scale2X();
				}
			}
			return original.Resize(true, width, width, interpolationMode: InterpolationMode.NearestNeighbor);
		}

	    /// <summary>
	    /// Check if the bitmaps are equal
	    /// </summary>
	    /// <param name="bitmap1">Bitmap</param>
	    /// <param name="bitmap2">Bitmap</param>
	    /// <returns>bool true if they are equal</returns>
	    public static bool IsEqualTo(this IBitmapWithNativeSupport bitmap1, IBitmapWithNativeSupport bitmap2)
	    {
	        if (bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height)
	        {
                Log.Debug().WriteLine("Different sizes 1={0}, 2={1}", bitmap1.Size, bitmap2.Size);
	            // Different sizes
	            return false;
	        }

	        if (bitmap1.PixelFormat != bitmap2.PixelFormat)
	        {
                // Different pixel formats
	            Log.Debug().WriteLine("Different pixel formats 1={0}, 2={1}", bitmap1.PixelFormat, bitmap2.PixelFormat);
	            return false;
	        }
	        bool result = true;
	        using (var fastBitmap1 = FastBitmapFactory.Create(bitmap1))
	        using (var fastBitmap2 = FastBitmapFactory.Create(bitmap2))
	        {
	            Parallel.For(0, fastBitmap1.Height, (y, state) =>
	            {
	                unsafe
	                {
	                    var tmpColor1 = stackalloc byte[4];
	                    var tmpColor2 = stackalloc byte[4];
	                    for (int x = 0; x < fastBitmap1.Width; x++)
	                    {
	                        fastBitmap1.GetColorAt(x, y, tmpColor1);
	                        fastBitmap2.GetColorAt(x, y, tmpColor2);
	                        if (AreColorsSame(tmpColor1, tmpColor2, fastBitmap1.HasAlphaChannel))
	                        {
	                            continue;
	                        }
	                        Log.Debug().WriteLine("Different colors at {0},{1}", x, y);
                            result = false;
	                        state.Break();
	                    }
	                }
	            });
	        }
	        return result;
	    }

	    /// <summary>
	    ///     Checks if the colors are the same.
	    /// </summary>
	    /// <param name="aColor">Color first</param>
	    /// <param name="bColor">Color second</param>
        /// <param name="hasAlpha">bool hasAlpha</param>
	    /// <returns>True if they are; otherwise false</returns>
	    [MethodImpl(MethodImplOptions.AggressiveInlining)]
	    private static unsafe bool AreColorsSame(byte* aColor, byte* bColor, bool hasAlpha = false)
	    {
	        return aColor[0] == bColor[0] && aColor[1] == bColor[1] && aColor[2] == bColor[2] && (hasAlpha ? aColor[3] == bColor[3] : true);
	    }
    }
}