/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Effects;
using Dapplo.Ini;
using log4net;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using Matrix = System.Drawing.Drawing2D.Matrix;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of ImageHelper.
    /// </summary>
    public static class ImageHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageHelper));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private const int ExifOrientationId = 0x0112;


        /// <summary>
        /// Make sure the image is orientated correctly
        /// </summary>
        /// <param name="image"></param>
        public static void Orientate(Image image)
        {
            if (!CoreConfig.ProcessEXIFOrientation)
            {
                return;
            }

            try
            {
                // Get the index of the orientation property.
                int orientationIndex = Array.IndexOf(image.PropertyIdList, ExifOrientationId);
                // If there is no such property, return Unknown.
                if (orientationIndex < 0)
                {
                    return;
                }

                PropertyItem item = image.GetPropertyItem(ExifOrientationId);
                if (item.Value == null || item.Value.Length == 0)
                {
                    return;
                }

                ExifOrientations orientation = (ExifOrientations) item.Value[0];
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
                Log.Warn("Problem orientating the image: ", orientEx);
            }
        }

        /// <summary>
        /// Create a Thumbnail
        /// </summary>
        /// <param name="image"></param>
        /// <param name="thumbWidth"></param>
        /// <param name="thumbHeight"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        public static Image CreateThumbnail(Image image, int thumbWidth, int thumbHeight, int maxWidth = -1, int maxHeight = -1)
        {
            int srcWidth = image.Width;
            int srcHeight = image.Height;
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

            Bitmap bmp = new Bitmap(thumbWidth, thumbHeight);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                NativeRect rectDestination = new NativeRect(0, 0, thumbWidth, thumbHeight);
                graphics.DrawImage(image, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);
            }

            return bmp;
        }

        /// <summary>
        /// Crops the image to the specified NativeRect
        /// </summary>
        /// <param name="image">Image to crop</param>
        /// <param name="cropNativeRect">NativeRect with bitmap coordinates, will be "intersected" to the bitmap</param>
        public static bool Crop(ref Image image, ref NativeRect cropNativeRect)
        {
            if (image is Bitmap && (image.Width * image.Height > 0))
            {
                cropNativeRect = cropNativeRect.Intersect(new NativeRect(0, 0, image.Width, image.Height));
                if (cropNativeRect.Width != 0 || cropNativeRect.Height != 0)
                {
                    Image returnImage = CloneArea(image, cropNativeRect, PixelFormat.DontCare);
                    image.Dispose();
                    image = returnImage;
                    return true;
                }
            }

            Log.Warn("Can't crop a null/zero size image!");
            return false;
        }

        /// <summary>
        /// Private helper method for the FindAutoCropNativeRect
        /// </summary>
        /// <param name="fastBitmap">IFastBitmap</param>
        /// <param name="colorPoint">NativePoint</param>
        /// <param name="cropDifference">int</param>
        /// <param name="area">NativeRect with optional area to scan in</param>
        /// <returns>NativeRect</returns>
        private static NativeRect FindAutoCropNativeRect(IFastBitmap fastBitmap, NativePoint colorPoint, int cropDifference, NativeRect? area = null)
        {
            area ??= new NativeRect(0, 0, fastBitmap.Width, fastBitmap.Height);
            NativeRect cropNativeRect = NativeRect.Empty;
            Color referenceColor = fastBitmap.GetColorAt(colorPoint.X, colorPoint.Y);
            NativePoint min = new NativePoint(int.MaxValue, int.MaxValue);
            NativePoint max = new NativePoint(int.MinValue, int.MinValue);

            if (cropDifference > 0)
            {
                for (int y = area.Value.Top; y < area.Value.Bottom; y++)
                {
                    for (int x = area.Value.Left; x < area.Value.Right; x++)
                    {
                        Color currentColor = fastBitmap.GetColorAt(x, y);
                        int diffR = Math.Abs(currentColor.R - referenceColor.R);
                        int diffG = Math.Abs(currentColor.G - referenceColor.G);
                        int diffB = Math.Abs(currentColor.B - referenceColor.B);
                        if ((diffR + diffG + diffB) / 3 <= cropDifference)
                        {
                            continue;
                        }

                        if (x < min.X) min = min.ChangeX(x);
                        if (y < min.Y) min = min.ChangeY(y);
                        if (x > max.X) max = max.ChangeX(x);
                        if (y > max.Y) max = max.ChangeY(y);
                    }
                }
            }
            else
            {
                for (int y = area.Value.Top; y < area.Value.Bottom; y++)
                {
                    for (int x = area.Value.Left; x < area.Value.Right; x++)
                    {
                        Color currentColor = fastBitmap.GetColorAt(x, y);
                        if (!referenceColor.Equals(currentColor))
                        {
                            continue;
                        }

                        if (x < min.X) min = min.ChangeX(x);
                        if (y < min.Y) min = min.ChangeY(y);
                        if (x > max.X) max = max.ChangeX(x);
                        if (y > max.Y) max = max.ChangeY(y);
                    }
                }
            }

            if (!(NativePoint.Empty.Equals(min) && max.Equals(new NativePoint(area.Value.Width - 1, area.Value.Height - 1))))
            {
                if (!(min.X == int.MaxValue || min.Y == int.MaxValue || max.X == int.MinValue || min.X == int.MinValue))
                {
                    cropNativeRect = new NativeRect(min.X, min.Y, max.X - min.X + 1, max.Y - min.Y + 1);
                }
            }

            return cropNativeRect;
        }

        /// <summary>
        /// Get a NativeRect for the image which crops the image of all colors equal to that on 0,0
        /// </summary>
        /// <param name="image">Image</param>
        /// <param name="cropDifference">int</param>
        /// <param name="area">NativeRect with optional area</param>
        /// <returns>NativeRect</returns>
        public static NativeRect FindAutoCropRectangle(Image image, int cropDifference, NativeRect? area = null)
        {
            area ??= new NativeRect(0, 0, image.Width, image.Height);
            NativeRect cropNativeRect = NativeRect.Empty;
            var checkPoints = new List<NativePoint>
            {
                new(area.Value.Left, area.Value.Top),
                new(area.Value.Left, area.Value.Bottom - 1),
                new(area.Value.Right - 1, area.Value.Top),
                new(area.Value.Right - 1, area.Value.Bottom - 1)
            };

            // Top Left
            // Bottom Left
            // Top Right
            // Bottom Right
            using (IFastBitmap fastBitmap = FastBitmap.Create((Bitmap) image))
            {
                // find biggest area
                foreach (var checkPoint in checkPoints)
                {
                    var currentNativeRect = FindAutoCropNativeRect(fastBitmap, checkPoint, cropDifference, area);
                    if (currentNativeRect.Width * currentNativeRect.Height > cropNativeRect.Width * cropNativeRect.Height)
                    {
                        cropNativeRect = currentNativeRect;
                    }
                }
            }

            return cropNativeRect;
        }

        /// <summary>
        /// Apply the effect to the bitmap
        /// </summary>
        /// <param name="sourceImage">Bitmap</param>
        /// <param name="effect">IEffect</param>
        /// <param name="matrix">Matrix</param>
        /// <returns>Bitmap</returns>
        public static Image ApplyEffect(Image sourceImage, IEffect effect, Matrix matrix)
        {
            var effects = new List<IEffect>
            {
                effect
            };
            return ApplyEffects(sourceImage, effects, matrix);
        }

        /// <summary>
        /// Apply the effects in the supplied order to the bitmap
        /// </summary>
        /// <param name="sourceImage">Bitmap</param>
        /// <param name="effects">List of IEffect</param>
        /// <param name="matrix"></param>
        /// <returns>Bitmap</returns>
        public static Image ApplyEffects(Image sourceImage, IEnumerable<IEffect> effects, Matrix matrix)
        {
            var currentImage = sourceImage;
            bool disposeImage = false;
            foreach (var effect in effects)
            {
                var tmpImage = effect.Apply(currentImage, matrix);
                if (tmpImage != null)
                {
                    if (disposeImage)
                    {
                        currentImage.Dispose();
                    }

                    currentImage = tmpImage;
                    // Make sure the "new" image is disposed
                    disposeImage = true;
                }
            }

            return currentImage;
        }

        /// <summary>
        /// Helper method for the tornedge
        /// </summary>
        /// <param name="path">Path to draw to</param>
        /// <param name="points">Points for the lines to draw</param>
        private static void DrawLines(GraphicsPath path, List<NativePoint> points)
        {
            path.AddLine(points[0], points[1]);
            for (int i = 0; i < points.Count - 1; i++)
            {
                path.AddLine(points[i], points[i + 1]);
            }
        }

        /// <summary>
        /// Make the picture look like it's torn
        /// </summary>
        /// <param name="sourceImage">Bitmap to make torn edge off</param>
        /// <param name="toothHeight">How large (height) is each tooth</param>
        /// <param name="horizontalToothRange">How wide is a horizontal tooth</param>
        /// <param name="verticalToothRange">How wide is a vertical tooth</param>
        /// <param name="edges">bool[] with information on if the edge needs torn or not. Order is clockwise: 0=top,1=right,2=bottom,3=left</param>
        /// <returns>Changed bitmap</returns>
        public static Image CreateTornEdge(Image sourceImage, int toothHeight, int horizontalToothRange, int verticalToothRange, bool[] edges)
        {
            Image returnImage = CreateEmpty(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb, Color.Empty, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            using (var path = new GraphicsPath())
            {
                Random random = new Random();
                int horizontalRegions = (int) Math.Round((float) sourceImage.Width / horizontalToothRange);
                int verticalRegions = (int) Math.Round((float) sourceImage.Height / verticalToothRange);

                var topLeft = new NativePoint(0, 0);
                var topRight = new NativePoint(sourceImage.Width, 0);
                var bottomLeft = new NativePoint(0, sourceImage.Height);
                var bottomRight = new NativePoint(sourceImage.Width, sourceImage.Height);

                var points = new List<NativePoint>();

                if (edges[0])
                {
                    // calculate starting point only if the left edge is torn
                    if (!edges[3])
                    {
                        points.Add(topLeft);
                    }
                    else
                    {
                        points.Add(new NativePoint(random.Next(1, toothHeight), random.Next(1, toothHeight)));
                    }

                    for (int i = 1; i < horizontalRegions - 1; i++)
                    {
                        points.Add(new NativePoint(i * horizontalToothRange, random.Next(1, toothHeight)));
                    }

                    points.Add(new NativePoint(sourceImage.Width - random.Next(1, toothHeight), random.Next(1, toothHeight)));
                }
                else
                {
                    // set start & endpoint to be the default "whole-line"
                    points.Add(topLeft);
                    points.Add(topRight);
                }

                // Right
                if (edges[1])
                {
                    for (int i = 1; i < verticalRegions - 1; i++)
                    {
                        points.Add(new NativePoint(sourceImage.Width - random.Next(1, toothHeight), i * verticalToothRange));
                    }

                    points.Add(new NativePoint(sourceImage.Width - random.Next(1, toothHeight), sourceImage.Height - random.Next(1, toothHeight)));
                }
                else
                {
                    // correct previous ending point
                    points[points.Count - 1] = topRight;
                    // set endpoint to be the default "whole-line"
                    points.Add(bottomRight);
                }

                // Bottom
                if (edges[2])
                {
                    for (int i = 1; i < horizontalRegions - 1; i++)
                    {
                        points.Add(new NativePoint(sourceImage.Width - i * horizontalToothRange, sourceImage.Height - random.Next(1, toothHeight)));
                    }

                    points.Add(new NativePoint(random.Next(1, toothHeight), sourceImage.Height - random.Next(1, toothHeight)));
                }
                else
                {
                    // correct previous ending point
                    points[points.Count - 1] = bottomRight;
                    // set endpoint to be the default "whole-line"
                    points.Add(bottomLeft);
                }

                // Left
                if (edges[3])
                {
                    // One fewer as the end point is the starting point
                    for (int i = 1; i < verticalRegions - 1; i++)
                    {
                        points.Add(new NativePoint(random.Next(1, toothHeight), points[points.Count - 1].Y - verticalToothRange));
                    }
                }
                else
                {
                    // correct previous ending point
                    points[points.Count - 1] = bottomLeft;
                    // set endpoint to be the default "whole-line"
                    points.Add(topLeft);
                }

                // End point always is the starting point
                points[points.Count - 1] = points[0];

                DrawLines(path, points);

                path.CloseFigure();

                // Draw the created figure with the original image by using a TextureBrush so we have anti-aliasing
                using Graphics graphics = Graphics.FromImage(returnImage);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using Brush brush = new TextureBrush(sourceImage);
                // Important note: If the target wouldn't be at 0,0 we need to translate-transform!!
                graphics.FillPath(brush, path);
            }

            return returnImage;
        }

        /// <summary>
        /// Apply BoxBlur to the destinationBitmap
        /// </summary>
        /// <param name="destinationBitmap">Bitmap to blur</param>
        /// <param name="range">Must be ODD!</param>
        public static void ApplyBoxBlur(Bitmap destinationBitmap, int range)
        {
            // We only need one fastbitmap as we use it as source and target (the reading is done for one line H/V, writing after "parsing" one line H/V)
            using IFastBitmap fastBitmap = FastBitmap.Create(destinationBitmap);
            ApplyBoxBlur(fastBitmap, range);
        }

        /// <summary>
        /// Apply BoxBlur to the fastBitmap
        /// </summary>
        /// <param name="fastBitmap">IFastBitmap to blur</param>
        /// <param name="range">Must be ODD!</param>
        public static void ApplyBoxBlur(IFastBitmap fastBitmap, int range)
        {
            // Range must be odd!
            if ((range & 1) == 0)
            {
                range++;
            }

            if (range <= 1)
            {
                return;
            }

            // Box blurs are frequently used to approximate a Gaussian blur.
            // By the central limit theorem, if applied 3 times on the same image, a box blur approximates the Gaussian kernel to within about 3%, yielding the same result as a quadratic convolution kernel.
            // This might be true, but the GDI+ BlurEffect doesn't look the same, a 2x blur is more similar and we only make 2x Box-Blur.
            // (Might also be a mistake in our blur, but for now it looks great)
            int channelCount = fastBitmap.HasAlphaChannel ? 4 : 3;
            BoxBlurHorizontalCore(fastBitmap, range, channelCount);
            BoxBlurVerticalCore(fastBitmap, range, channelCount);
            BoxBlurHorizontalCore(fastBitmap, range, channelCount);
            BoxBlurVerticalCore(fastBitmap, range, channelCount);
        }

        /// <summary>
        /// BoxBlurHorizontalCore is the shared horizontal-pass implementation for BoxBlur.
        /// It processes <paramref name="channelCount"/> consecutive bytes per pixel (e.g. 3 for RGB, 4 for ARGB)
        /// using ArrayPool buffers to minimise GC pressure.
        /// The channelCount branch is hoisted outside all loops; pixel access uses pointer advancement
        /// (no per-pixel multiplications, no per-pixel branches).
        /// </summary>
        /// <param name="targetFastBitmap">Target BitmapBuffer</param>
        /// <param name="range">Range must be odd!</param>
        /// <param name="channelCount">Number of byte channels to blur (3 for RGB, 4 for ARGB)</param>
        private static unsafe void BoxBlurHorizontalCore(IFastBitmap targetFastBitmap, int range, int channelCount)
        {
            int halfRange = range / 2;
            int width = targetFastBitmap.Width;
            int bytesPerPixel = targetFastBitmap.BytesPerPixel;
            int left = targetFastBitmap.Left;
            int right = targetFastBitmap.Right;
            int top = targetFastBitmap.Top;
            int bottom = targetFastBitmap.Bottom;

            // Channels are laid out sequentially in memory: [0]=B, [1]=G, [2]=R, [3]=A (BGRA order).
            // Rent one output buffer per channel; these are returned even if an exception is thrown.
            byte[] buf0 = ArrayPool<byte>.Shared.Rent(width);
            byte[] buf1 = ArrayPool<byte>.Shared.Rent(width);
            byte[] buf2 = ArrayPool<byte>.Shared.Rent(width);
            byte[] buf3 = channelCount == 4 ? ArrayPool<byte>.Shared.Rent(width) : null;
            try
            {
                // The channelCount check is hoisted here so the inner loops contain no conditional branching.
                // oldPtr / newPtr advance by bytesPerPixel on every access, avoiding any per-pixel multiplication.
                if (channelCount == 4)
                {
                    for (int y = top; y < bottom; y++)
                    {
                        byte* rowPtr = (byte*)targetFastBitmap.GetRowPointer(y);
                        // Both pointers start at the first pixel of the clipped area.
                        // They advance by bytesPerPixel each time the sliding window adds/removes a pixel.
                        byte* oldPtr = rowPtr + left * bytesPerPixel;
                        byte* newPtr = rowPtr + left * bytesPerPixel;
                        int hits = 0;
                        int ch0 = 0, ch1 = 0, ch2 = 0, ch3 = 0;
                        for (int x = left - halfRange; x < right; x++)
                        {
                            int oldPixel = x - halfRange - 1;
                            if (oldPixel >= left)
                            {
                                ch0 -= oldPtr[0]; ch1 -= oldPtr[1]; ch2 -= oldPtr[2]; ch3 -= oldPtr[3];
                                oldPtr += bytesPerPixel;
                                hits--;
                            }

                            int newPixel = x + halfRange;
                            if (newPixel < right)
                            {
                                ch0 += newPtr[0]; ch1 += newPtr[1]; ch2 += newPtr[2]; ch3 += newPtr[3];
                                newPtr += bytesPerPixel;
                                hits++;
                            }

                            if (x >= left)
                            {
                                int idx = x - left;
                                buf0[idx] = (byte)(ch0 / hits);
                                buf1[idx] = (byte)(ch1 / hits);
                                buf2[idx] = (byte)(ch2 / hits);
                                buf3[idx] = (byte)(ch3 / hits);
                            }
                        }

                        byte* writePtr = rowPtr + left * bytesPerPixel;
                        for (int i = 0; i < width; i++)
                        {
                            writePtr[0] = buf0[i]; writePtr[1] = buf1[i];
                            writePtr[2] = buf2[i]; writePtr[3] = buf3[i];
                            writePtr += bytesPerPixel;
                        }
                    }
                }
                else // channelCount == 3
                {
                    for (int y = top; y < bottom; y++)
                    {
                        byte* rowPtr = (byte*)targetFastBitmap.GetRowPointer(y);
                        byte* oldPtr = rowPtr + left * bytesPerPixel;
                        byte* newPtr = rowPtr + left * bytesPerPixel;
                        int hits = 0;
                        int ch0 = 0, ch1 = 0, ch2 = 0;
                        for (int x = left - halfRange; x < right; x++)
                        {
                            int oldPixel = x - halfRange - 1;
                            if (oldPixel >= left)
                            {
                                ch0 -= oldPtr[0]; ch1 -= oldPtr[1]; ch2 -= oldPtr[2];
                                oldPtr += bytesPerPixel;
                                hits--;
                            }

                            int newPixel = x + halfRange;
                            if (newPixel < right)
                            {
                                ch0 += newPtr[0]; ch1 += newPtr[1]; ch2 += newPtr[2];
                                newPtr += bytesPerPixel;
                                hits++;
                            }

                            if (x >= left)
                            {
                                int idx = x - left;
                                buf0[idx] = (byte)(ch0 / hits);
                                buf1[idx] = (byte)(ch1 / hits);
                                buf2[idx] = (byte)(ch2 / hits);
                            }
                        }

                        byte* writePtr = rowPtr + left * bytesPerPixel;
                        for (int i = 0; i < width; i++)
                        {
                            writePtr[0] = buf0[i]; writePtr[1] = buf1[i]; writePtr[2] = buf2[i];
                            writePtr += bytesPerPixel;
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf0);
                ArrayPool<byte>.Shared.Return(buf1);
                ArrayPool<byte>.Shared.Return(buf2);
                if (buf3 != null) ArrayPool<byte>.Shared.Return(buf3);
            }
        }

        /// <summary>
        /// BoxBlurVerticalCore is the shared vertical-pass implementation for BoxBlur.
        /// It processes <paramref name="channelCount"/> consecutive bytes per pixel (e.g. 3 for RGB, 4 for ARGB)
        /// using ArrayPool buffers to minimise GC pressure.
        /// The channelCount branch is hoisted outside all loops; column access uses stride-based pointer
        /// advancement rather than per-row GetRowPointer calls.
        /// </summary>
        /// <param name="targetFastBitmap">BitmapBuffer which previously was created with BoxBlurHorizontalCore</param>
        /// <param name="range">Range must be odd!</param>
        /// <param name="channelCount">Number of byte channels to blur (3 for RGB, 4 for ARGB)</param>
        private static unsafe void BoxBlurVerticalCore(IFastBitmap targetFastBitmap, int range, int channelCount)
        {
            int halfRange = range / 2;
            int height = targetFastBitmap.Height;
            int bytesPerPixel = targetFastBitmap.BytesPerPixel;
            int stride = targetFastBitmap.Stride;
            int left = targetFastBitmap.Left;
            int right = targetFastBitmap.Right;
            int top = targetFastBitmap.Top;
            int bottom = targetFastBitmap.Bottom;

            // Channels are laid out sequentially in memory: [0]=B, [1]=G, [2]=R, [3]=A (BGRA order).
            byte[] buf0 = ArrayPool<byte>.Shared.Rent(height);
            byte[] buf1 = ArrayPool<byte>.Shared.Rent(height);
            byte[] buf2 = ArrayPool<byte>.Shared.Rent(height);
            byte[] buf3 = channelCount == 4 ? ArrayPool<byte>.Shared.Rent(height) : null;

            // Base pointer to row `top`; xOffset added per column so each column walk uses stride advancement.
            byte* topRowPtr = (byte*)targetFastBitmap.GetRowPointer(top);
            try
            {
                // The channelCount check is hoisted here so the inner loops contain no conditional branching.
                // oldColPtr / newColPtr advance by `stride` on every access, replacing per-row GetRowPointer calls.
                if (channelCount == 4)
                {
                    for (int x = left; x < right; x++)
                    {
                        int xOffset = x * bytesPerPixel;
                        // Column pointers start at the first valid row (top) and advance by stride each step.
                        byte* oldColPtr = topRowPtr + xOffset;
                        byte* newColPtr = topRowPtr + xOffset;
                        int hits = 0;
                        int ch0 = 0, ch1 = 0, ch2 = 0, ch3 = 0;
                        for (int y = top - halfRange; y < bottom; y++)
                        {
                            int oldPixel = y - halfRange - 1;
                            if (oldPixel >= top)
                            {
                                ch0 -= oldColPtr[0]; ch1 -= oldColPtr[1]; ch2 -= oldColPtr[2]; ch3 -= oldColPtr[3];
                                oldColPtr += stride;
                                hits--;
                            }

                            int newPixel = y + halfRange;
                            if (newPixel < bottom)
                            {
                                ch0 += newColPtr[0]; ch1 += newColPtr[1]; ch2 += newColPtr[2]; ch3 += newColPtr[3];
                                newColPtr += stride;
                                hits++;
                            }

                            if (y >= top)
                            {
                                int idx = y - top;
                                buf0[idx] = (byte)(ch0 / hits);
                                buf1[idx] = (byte)(ch1 / hits);
                                buf2[idx] = (byte)(ch2 / hits);
                                buf3[idx] = (byte)(ch3 / hits);
                            }
                        }

                        byte* writeColPtr = topRowPtr + xOffset;
                        for (int i = 0; i < height; i++)
                        {
                            writeColPtr[0] = buf0[i]; writeColPtr[1] = buf1[i];
                            writeColPtr[2] = buf2[i]; writeColPtr[3] = buf3[i];
                            writeColPtr += stride;
                        }
                    }
                }
                else // channelCount == 3
                {
                    for (int x = left; x < right; x++)
                    {
                        int xOffset = x * bytesPerPixel;
                        byte* oldColPtr = topRowPtr + xOffset;
                        byte* newColPtr = topRowPtr + xOffset;
                        int hits = 0;
                        int ch0 = 0, ch1 = 0, ch2 = 0;
                        for (int y = top - halfRange; y < bottom; y++)
                        {
                            int oldPixel = y - halfRange - 1;
                            if (oldPixel >= top)
                            {
                                ch0 -= oldColPtr[0]; ch1 -= oldColPtr[1]; ch2 -= oldColPtr[2];
                                oldColPtr += stride;
                                hits--;
                            }

                            int newPixel = y + halfRange;
                            if (newPixel < bottom)
                            {
                                ch0 += newColPtr[0]; ch1 += newColPtr[1]; ch2 += newColPtr[2];
                                newColPtr += stride;
                                hits++;
                            }

                            if (y >= top)
                            {
                                int idx = y - top;
                                buf0[idx] = (byte)(ch0 / hits);
                                buf1[idx] = (byte)(ch1 / hits);
                                buf2[idx] = (byte)(ch2 / hits);
                            }
                        }

                        byte* writeColPtr = topRowPtr + xOffset;
                        for (int i = 0; i < height; i++)
                        {
                            writeColPtr[0] = buf0[i]; writeColPtr[1] = buf1[i]; writeColPtr[2] = buf2[i];
                            writeColPtr += stride;
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf0);
                ArrayPool<byte>.Shared.Return(buf1);
                ArrayPool<byte>.Shared.Return(buf2);
                if (buf3 != null) ArrayPool<byte>.Shared.Return(buf3);
            }
        }

        /// <summary>
        /// This method fixes the problem that we can't apply a filter outside the target bitmap,
        /// therefore the filtered-bitmap will be shifted if we try to draw it outside the target bitmap.
        /// It will also account for the Invert flag.
        /// </summary>
        /// <param name="applySize"></param>
        /// <param name="rect"></param>
        /// <param name="invert"></param>
        /// <returns>NativeRect</returns>
        public static NativeRect CreateIntersectRectangle(NativeSize applySize, NativeRect rect, bool invert)
        {
            NativeRect myRect;
            if (invert)
            {
                myRect = new NativeRect(0, 0, applySize.Width, applySize.Height);
            }
            else
            {
                NativeRect applyRect = new NativeRect(0, 0, applySize.Width, applySize.Height);
                myRect = new NativeRect(rect.X, rect.Y, rect.Width, rect.Height).Intersect(applyRect);
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
        /// <param name="shadowOffset"></param>
        /// <param name="matrix">The transform matrix which describes how the elements need to be transformed to stay at the same location</param>
        /// <returns>Bitmap with the shadow, is bigger than the sourceBitmap!!</returns>
        public static Bitmap CreateShadow(Image sourceBitmap, float darkness, int shadowSize, NativePoint shadowOffset, Matrix matrix, PixelFormat targetPixelformat)
        {
            NativePoint offset = shadowOffset.Offset(shadowSize - 1, shadowSize - 1);
            matrix.Translate(offset.X, offset.Y, MatrixOrder.Append);
            // Create a new "clean" image
            Bitmap returnImage = CreateEmpty(sourceBitmap.Width + shadowSize * 2, sourceBitmap.Height + shadowSize * 2, targetPixelformat, Color.Empty,
                sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
            // Make sure the shadow is odd, there is no reason for an even blur!
            if ((shadowSize & 1) == 0)
            {
                shadowSize++;
            }

            bool useGdiBlur = GdiPlusApi.IsBlurPossible(shadowSize);
            // Create "mask" for the shadow
            ColorMatrix maskMatrix = new ColorMatrix
            {
                Matrix00 = 0,
                Matrix11 = 0,
                Matrix22 = 0
            };
            if (useGdiBlur)
            {
                maskMatrix.Matrix33 = darkness + 0.1f;
            }
            else
            {
                maskMatrix.Matrix33 = darkness;
            }

            NativeRect shadowNativeRect = new NativeRect(new NativePoint(shadowSize, shadowSize), sourceBitmap.Size);
            ApplyColorMatrix((Bitmap) sourceBitmap, NativeRect.Empty, returnImage, shadowNativeRect, maskMatrix);

            // blur "shadow", apply to whole new image
            if (useGdiBlur)
            {
                // Use GDI Blur
                NativeRect newImageNativeRect = new NativeRect(0, 0, returnImage.Width, returnImage.Height);
                GdiPlusApi.ApplyBlur(returnImage, newImageNativeRect, shadowSize + 1, false);
            }
            else
            {
                // try normal software blur
                //returnImage = CreateBlur(returnImage, newImageNativeRect, true, shadowSize, 1d, false, newImageNativeRect);
                ApplyBoxBlur(returnImage, shadowSize);
            }

            // Draw the original image over the shadow
            using (Graphics graphics = Graphics.FromImage(returnImage))
            {
                // Make sure we draw with the best quality!
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                // draw original with a TextureBrush so we have nice antialiasing!
                using Brush textureBrush = new TextureBrush(sourceBitmap, WrapMode.Clamp);
                // We need to do a translate-transform otherwise the image is wrapped
                graphics.TranslateTransform(offset.X, offset.Y);
                graphics.FillRectangle(textureBrush, 0, 0, sourceBitmap.Width, sourceBitmap.Height);
            }

            return returnImage;
        }

        /// <summary>
        /// Return negative of Bitmap
        /// </summary>
        /// <param name="sourceImage">Bitmap to create a negative off</param>
        /// <returns>Negative bitmap</returns>
        public static Bitmap CreateNegative(Image sourceImage)
        {
            Bitmap clone = (Bitmap) Clone(sourceImage);
            ColorMatrix invertMatrix = new ColorMatrix(new[]
            {
                new float[]
                {
                    -1, 0, 0, 0, 0
                },
                new float[]
                {
                    0, -1, 0, 0, 0
                },
                new float[]
                {
                    0, 0, -1, 0, 0
                },
                new float[]
                {
                    0, 0, 0, 1, 0
                },
                new float[]
                {
                    1, 1, 1, 1, 1
                }
            });
            ApplyColorMatrix(clone, invertMatrix);
            return clone;
        }

        /// <summary>
        /// Apply a color matrix to the image
        /// </summary>
        /// <param name="source">Image to apply matrix to</param>
        /// <param name="colorMatrix">ColorMatrix to apply</param>
        public static void ApplyColorMatrix(Bitmap source, ColorMatrix colorMatrix)
        {
            ApplyColorMatrix(source, NativeRect.Empty, source, NativeRect.Empty, colorMatrix);
        }

        /// <summary>
        /// Apply a color matrix by copying from the source to the destination
        /// </summary>
        /// <param name="source">Image to copy from</param>
        /// <param name="sourceRect">NativeRect to copy from</param>
        /// <param name="destRect">NativeRect to copy to</param>
        /// <param name="dest">Image to copy to</param>
        /// <param name="colorMatrix">ColorMatrix to apply</param>
        public static void ApplyColorMatrix(Bitmap source, NativeRect sourceRect, Bitmap dest, NativeRect destRect, ColorMatrix colorMatrix)
        {
            using ImageAttributes imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(colorMatrix);
            ApplyImageAttributes(source, sourceRect, dest, destRect, imageAttributes);
        }

        /// <summary>
        /// Apply a color matrix by copying from the source to the destination
        /// </summary>
        /// <param name="source">Image to copy from</param>
        /// <param name="sourceRect">NativeRect to copy from</param>
        /// <param name="destRect">NativeRect to copy to</param>
        /// <param name="dest">Image to copy to</param>
        /// <param name="imageAttributes">ImageAttributes to apply</param>
        public static void ApplyImageAttributes(Bitmap source, NativeRect sourceRect, Bitmap dest, NativeRect destRect, ImageAttributes imageAttributes)
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

            using Graphics graphics = Graphics.FromImage(dest);
            // Make sure we draw with the best quality!
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingMode = CompositingMode.SourceCopy;

            graphics.DrawImage(source, destRect, sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height, GraphicsUnit.Pixel, imageAttributes);
        }

        /// <summary>
        /// Returns a b/w of Bitmap
        /// </summary>
        /// <param name="sourceImage">Bitmap to create a b/w of</param>
        /// <param name="threshold">Threshold for monochrome filter (0 - 255), lower value means less black</param>
        /// <returns>b/w bitmap</returns>
        public static Bitmap CreateMonochrome(Image sourceImage, byte threshold)
        {
            using IFastBitmap fastBitmap = FastBitmap.CreateCloneOf(sourceImage, sourceImage.PixelFormat);
            for (int y = 0; y < fastBitmap.Height; y++)
            {
                for (int x = 0; x < fastBitmap.Width; x++)
                {
                    Color color = fastBitmap.GetColorAt(x, y);
                    int colorBrightness = (color.R + color.G + color.B) / 3 > threshold ? 255 : 0;
                    Color monoColor = Color.FromArgb(color.A, colorBrightness, colorBrightness, colorBrightness);
                    fastBitmap.SetColorAt(x, y, monoColor);
                }
            }

            return fastBitmap.UnlockAndReturnBitmap();
        }

        /// <summary>
        /// Create a new bitmap where the sourceBitmap has a Simple border around it
        /// </summary>
        /// <param name="sourceImage">Bitmap to make a border on</param>
        /// <param name="borderSize">Size of the border</param>
        /// <param name="borderColor">Color of the border</param>
        /// <param name="targetPixelformat">What pixel format must the returning bitmap have</param>
        /// <param name="matrix">The transform matrix which describes how the elements need to be transformed to stay at the same location</param>
        /// <returns>Bitmap with the shadow, is bigger than the sourceBitmap!!</returns>
        public static Image CreateBorder(Image sourceImage, int borderSize, Color borderColor, PixelFormat targetPixelformat, Matrix matrix)
        {
            // "return" the shifted offset, so the caller can e.g. move elements
            NativePoint offset = new NativePoint(borderSize, borderSize);
            matrix.Translate(offset.X, offset.Y, MatrixOrder.Append);

            // Create a new "clean" image
            Bitmap newImage = CreateEmpty(sourceImage.Width + borderSize * 2, sourceImage.Height + borderSize * 2, targetPixelformat, Color.Empty, sourceImage.HorizontalResolution,
                sourceImage.VerticalResolution);
            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                // Make sure we draw with the best quality!
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddRectangle(new NativeRect(borderSize >> 1, borderSize >> 1, newImage.Width - borderSize, newImage.Height - borderSize));
                    using Pen pen = new Pen(borderColor, borderSize)
                    {
                        LineJoin = LineJoin.Round,
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    graphics.DrawPath(pen, path);
                }

                // draw original with a TextureBrush so we have nice antialiasing!
                using Brush textureBrush = new TextureBrush(sourceImage, WrapMode.Clamp);
                // We need to do a translate-tranform otherwise the image is wrapped
                graphics.TranslateTransform(offset.X, offset.Y);
                graphics.FillRectangle(textureBrush, 0, 0, sourceImage.Width, sourceImage.Height);
            }

            return newImage;
        }

        /// <summary>
        /// Create ImageAttributes to modify
        /// </summary>
        /// <param name="brightness"></param>
        /// <param name="contrast"></param>
        /// <param name="gamma"></param>
        /// <returns>ImageAttributes</returns>
        public static ImageAttributes CreateAdjustAttributes(float brightness, float contrast, float gamma)
        {
            float adjustedBrightness = brightness - 1.0f;
            ColorMatrix applyColorMatrix = new ColorMatrix(
                new[]
                {
                    new[]
                    {
                        contrast, 0, 0, 0, 0
                    }, // scale red
                    new[]
                    {
                        0, contrast, 0, 0, 0
                    }, // scale green
                    new[]
                    {
                        0, 0, contrast, 0, 0
                    }, // scale blue
                    new[]
                    {
                        0, 0, 0, 1.0f, 0
                    }, // don't scale alpha
                    new[]
                    {
                        adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1
                    }
                });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();
            attributes.ClearColorMatrix();
            attributes.SetColorMatrix(applyColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            attributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            return attributes;
        }

        /// <summary>
        /// Adjust the brightness, contract or gamma of an image.
        /// Use the value "1.0f" for no changes.
        /// </summary>
        /// <param name="sourceImage">Original bitmap</param>
        /// <param name="brightness"></param>
        /// <param name="contrast"></param>
        /// <param name="gamma"></param>
        /// <returns>Bitmap with grayscale</returns>
        public static Image Adjust(Image sourceImage, float brightness, float contrast, float gamma)
        {
            //create a blank bitmap the same size as original
            // If using 8bpp than the following exception comes: A Graphics object cannot be created from an image that has an indexed pixel format.
            Bitmap newBitmap = CreateEmpty(sourceImage.Width, sourceImage.Height, PixelFormat.Format24bppRgb, Color.Empty, sourceImage.HorizontalResolution,
                sourceImage.VerticalResolution);
            using (ImageAttributes adjustAttributes = CreateAdjustAttributes(brightness, contrast, gamma))
            {
                ApplyImageAttributes((Bitmap) sourceImage, NativeRect.Empty, newBitmap, NativeRect.Empty, adjustAttributes);
            }

            return newBitmap;
        }

        /// <summary>
        /// Create a new bitmap where the sourceBitmap is in grayscale
        /// </summary>
        /// <param name="sourceImage">Original bitmap</param>
        /// <returns>Bitmap with grayscale</returns>
        public static Image CreateGrayscale(Image sourceImage)
        {
            Bitmap clone = (Bitmap) Clone(sourceImage);
            ColorMatrix grayscaleMatrix = new ColorMatrix(new[]
            {
                new[]
                {
                    .3f, .3f, .3f, 0, 0
                },
                new[]
                {
                    .59f, .59f, .59f, 0, 0
                },
                new[]
                {
                    .11f, .11f, .11f, 0, 0
                },
                new float[]
                {
                    0, 0, 0, 1, 0
                },
                new float[]
                {
                    0, 0, 0, 0, 1
                }
            });
            ApplyColorMatrix(clone, grayscaleMatrix);
            return clone;
        }

        /// <summary>
        /// Checks if we support the pixel format
        /// </summary>
        /// <param name="pixelformat">PixelFormat to check</param>
        /// <returns>bool if we support it</returns>
        public static bool SupportsPixelFormat(PixelFormat pixelformat)
        {
            return pixelformat.Equals(PixelFormat.Format32bppArgb) ||
                   pixelformat.Equals(PixelFormat.Format32bppPArgb) ||
                   pixelformat.Equals(PixelFormat.Format32bppRgb) ||
                   pixelformat.Equals(PixelFormat.Format24bppRgb);
        }

        /// <summary>
        /// Wrapper for just cloning which calls the CloneArea
        /// </summary>
        /// <param name="sourceImage">Image to clone</param>
        /// <returns>Bitmap with clone image data</returns>
        public static Image Clone(Image sourceImage)
        {
            if (sourceImage is Metafile)
            {
                return (Image) sourceImage.Clone();
            }

            return CloneArea(sourceImage, NativeRect.Empty, PixelFormat.DontCare);
        }

        /// <summary>
        /// Wrapper for just cloning & TargetFormat which calls the CloneArea
        /// </summary>
        /// <param name="sourceBitmap">Image to clone</param>
        /// <param name="targetFormat">Target Format, use PixelFormat.DontCare if you want the original (or a default if the source PixelFormat is not supported)</param>
        /// <returns>Bitmap with clone image data</returns>
        public static Bitmap Clone(Image sourceBitmap, PixelFormat targetFormat)
        {
            return CloneArea(sourceBitmap, NativeRect.Empty, targetFormat);
        }

        /// <summary>
        /// Clone an image, taking some rules into account:
        /// 1) When sourceRect is the whole bitmap there is a GDI+ bug in Clone
        ///    Clone will than return the same PixelFormat as the source
        ///    a quick workaround is using new Bitmap which uses a default of Format32bppArgb
        /// 2) When going from a transparent to a non transparent bitmap, we draw the background white!
        /// </summary>
        /// <param name="sourceImage">Source bitmap to clone</param>
        /// <param name="sourceRect">NativeRect to copy from the source, use NativeRect.Empty for all</param>
        /// <param name="targetFormat">Target Format, use PixelFormat.DontCare if you want the original (or a default if the source PixelFormat is not supported)</param>
        /// <returns></returns>
        public static Bitmap CloneArea(Image sourceImage, NativeRect sourceRect, PixelFormat targetFormat)
        {
            Bitmap newImage;
            NativeRect bitmapRect = new NativeRect(0, 0, sourceImage.Width, sourceImage.Height);

            // Make sure the source is not NativeRect.Empty
            if (NativeRect.Empty.Equals(sourceRect))
            {
                sourceRect = new NativeRect(0, 0, sourceImage.Width, sourceImage.Height);
            }
            else
            {
                sourceRect = sourceRect.Intersect(bitmapRect);
            }

            // If no pixelformat is supplied
            if (targetFormat is PixelFormat.DontCare or PixelFormat.Undefined)
            {
                if (SupportsPixelFormat(sourceImage.PixelFormat))
                {
                    targetFormat = sourceImage.PixelFormat;
                }
                else if (Image.IsAlphaPixelFormat(sourceImage.PixelFormat))
                {
                    targetFormat = PixelFormat.Format32bppArgb;
                }
                else
                {
                    targetFormat = PixelFormat.Format24bppRgb;
                }
            }

            // check the target format
            if (!SupportsPixelFormat(targetFormat))
            {
                targetFormat = Image.IsAlphaPixelFormat(targetFormat) ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
            }

            bool destinationIsTransparent = Image.IsAlphaPixelFormat(targetFormat);
            bool sourceIsTransparent = Image.IsAlphaPixelFormat(sourceImage.PixelFormat);
            bool fromTransparentToNon = !destinationIsTransparent && sourceIsTransparent;
            bool isBitmap = sourceImage is Bitmap;
            bool isAreaEqual = sourceRect.Equals(bitmapRect);
            if (isAreaEqual || fromTransparentToNon || !isBitmap)
            {
                // Rule 1: if the areas are equal, always copy ourselves
                newImage = new Bitmap(bitmapRect.Width, bitmapRect.Height, targetFormat);
                // Make sure both images have the same resolution
                newImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);

                using Graphics graphics = Graphics.FromImage(newImage);
                if (fromTransparentToNon)
                {
                    // Rule 2: Make sure the background color is white
                    graphics.Clear(Color.White);
                }

                // decide fastest copy method
                if (isAreaEqual)
                {
                    graphics.DrawImageUnscaled(sourceImage, 0, 0);
                }
                else
                {
                    graphics.DrawImage(sourceImage, 0, 0, sourceRect, GraphicsUnit.Pixel);
                }
            }
            else
            {
                // Let GDI+ decide how to convert, need to test what is quicker...
                var sourceBitmap = sourceImage as Bitmap;
                if (sourceBitmap == null)
                {
                    throw new InvalidOperationException($"Expected Bitmap but got {sourceImage?.GetType().Name}");
                }
                newImage = sourceBitmap.Clone(sourceRect, targetFormat);
                // Make sure both images have the same resolution
                newImage.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            }

            // In WINE someone getting the PropertyItems doesn't work
            try
            {
                // Clone property items (EXIF information etc)
                foreach (var propertyItem in sourceImage.PropertyItems)
                {
                    try
                    {
                        newImage.SetPropertyItem(propertyItem);
                    }
                    catch (Exception innerEx)
                    {
                        Log.Warn("Problem cloning a propertyItem.", innerEx);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Problem cloning a propertyItem.", ex);
            }

            return newImage;
        }

        /// <summary>
        /// Rotate the bitmap
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="rotateFlipType"></param>
        /// <returns></returns>
        public static Image RotateFlip(Image sourceImage, RotateFlipType rotateFlipType)
        {
            Image returnImage = Clone(sourceImage);
            returnImage.RotateFlip(rotateFlipType);
            return returnImage;
        }

        /// <summary>
        /// A generic way to create an empty image
        /// </summary>
        /// <param name="sourceImage">the source bitmap as the specifications for the new bitmap</param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <param name="pixelFormat">PixelFormat</param>
        /// <returns>Bitmap</returns>
        public static Bitmap CreateEmptyLike(Image sourceImage, Color backgroundColor, PixelFormat? pixelFormat = null)
        {
            pixelFormat ??= sourceImage.PixelFormat;
            if (backgroundColor.A < 255)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }

            return CreateEmpty(sourceImage.Width, sourceImage.Height, pixelFormat.Value, backgroundColor, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
        }

        /// <summary>
        /// A generic way to create an empty image
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="format"></param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <param name="horizontalResolution">float</param>
        /// <param name="verticalResolution">float</param>
        /// <returns>Bitmap</returns>
        public static Bitmap CreateEmpty(int width, int height, PixelFormat format, Color backgroundColor, float horizontalResolution = 96f, float verticalResolution = 96f)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException($"Cannot create a bitmap with non-positive dimensions ({width}x{height}).");
            }
            // Create a new "clean" image
            Bitmap newImage = new Bitmap(width, height, format);
            newImage.SetResolution(horizontalResolution, verticalResolution);
            if (format != PixelFormat.Format8bppIndexed)
            {
                using Graphics graphics = Graphics.FromImage(newImage);
                // Make sure the background color is what we want (transparent or white, depending on the pixel format)
                if (!Color.Empty.Equals(backgroundColor))
                {
                    graphics.Clear(backgroundColor);
                }
                else if (Image.IsAlphaPixelFormat(format))
                {
                    graphics.Clear(Color.Transparent);
                }
                else
                {
                    graphics.Clear(Color.White);
                }
            }

            return newImage;
        }

        /// <summary>
        /// Resize canvas with pixel to the left, right, top and bottom
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="matrix"></param>
        /// <returns>a new bitmap with the source copied on it</returns>
        public static Image ResizeCanvas(Image sourceImage, Color backgroundColor, int left, int right, int top, int bottom, Matrix matrix)
        {
            matrix.Translate(left, top, MatrixOrder.Append);
            Bitmap newBitmap = CreateEmpty(sourceImage.Width + left + right, sourceImage.Height + top + bottom, sourceImage.PixelFormat, backgroundColor,
                sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
            using (Graphics graphics = Graphics.FromImage(newBitmap))
            {
                graphics.DrawImageUnscaled(sourceImage, left, top);
            }

            return newBitmap;
        }

        /// <summary>
        /// Wrapper for the more complex Resize, this resize could be used for e.g. Thumbnails
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="maintainAspectRatio">true to maintain the aspect ratio</param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Image ResizeImage(Image sourceImage, bool maintainAspectRatio, int newWidth, int newHeight, Matrix matrix)
        {
            return ResizeImage(sourceImage, maintainAspectRatio, false, Color.Empty, newWidth, newHeight, matrix);
        }

        /// <summary>
        /// Count how many times the supplied color exists
        /// </summary>
        /// <param name="sourceImage">Image to count the pixels of</param>
        /// <param name="colorToCount">Color to count</param>
        /// <param name="includeAlpha">true if Alpha needs to be checked</param>
        /// <returns>int with the number of pixels which have colorToCount</returns>
        public static int CountColor(Image sourceImage, Color colorToCount, bool includeAlpha)
        {
            int colors = 0;
            int toCount = colorToCount.ToArgb();
            if (!includeAlpha)
            {
                toCount &= 0xffffff;
            }

            using IFastBitmap bb = FastBitmap.Create((Bitmap) sourceImage);
            for (int y = 0; y < bb.Height; y++)
            {
                for (int x = 0; x < bb.Width; x++)
                {
                    int bitmapcolor = bb.GetColorAt(x, y).ToArgb();
                    if (!includeAlpha)
                    {
                        bitmapcolor &= 0xffffff;
                    }

                    if (bitmapcolor == toCount)
                    {
                        colors++;
                    }
                }
            }

            return colors;
        }

        /// <summary>
        /// Scale the bitmap, keeping aspect ratio, but the canvas will always have the specified size.
        /// </summary>
        /// <param name="sourceImage">Image to scale</param>
        /// <param name="maintainAspectRatio">true to maintain the aspect ratio</param>
        /// <param name="canvasUseNewSize">Makes the image maintain aspect ratio, but the canvas gets the specified size</param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <param name="newWidth">new width</param>
        /// <param name="newHeight">new height</param>
        /// <param name="matrix"></param>
        /// <returns>a new bitmap with the specified size, the source-Image scaled to fit with aspect ratio locked</returns>
        public static Image ResizeImage(Image sourceImage, bool maintainAspectRatio, bool canvasUseNewSize, Color backgroundColor, int newWidth, int newHeight, Matrix matrix)
        {
            int destX = 0;
            int destY = 0;

            var nPercentW = newWidth / (float) sourceImage.Width;
            var nPercentH = newHeight / (float) sourceImage.Height;
            if (maintainAspectRatio)
            {
                if ((int) nPercentW == 1)
                {
                    nPercentW = nPercentH;
                    if (canvasUseNewSize)
                    {
                        destX = Math.Max(0, Convert.ToInt32((newWidth - sourceImage.Width * nPercentW) / 2));
                    }
                }
                else if ((int) nPercentH == 1)
                {
                    nPercentH = nPercentW;
                    if (canvasUseNewSize)
                    {
                        destY = Math.Max(0, Convert.ToInt32((newHeight - sourceImage.Height * nPercentH) / 2));
                    }
                }
                else if ((int) nPercentH != 0 && nPercentH < nPercentW)
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

            int destWidth = (int) (sourceImage.Width * nPercentW);
            int destHeight = (int) (sourceImage.Height * nPercentH);
            if (newWidth == 0)
            {
                newWidth = destWidth;
            }

            if (newHeight == 0)
            {
                newHeight = destHeight;
            }

            Image newImage;
            if (maintainAspectRatio && canvasUseNewSize)
            {
                newImage = CreateEmpty(newWidth, newHeight, sourceImage.PixelFormat, backgroundColor, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
                matrix?.Scale((float) newWidth / sourceImage.Width, (float) newHeight / sourceImage.Height, MatrixOrder.Append);
            }
            else
            {
                newImage = CreateEmpty(destWidth, destHeight, sourceImage.PixelFormat, backgroundColor, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
                matrix?.Scale((float) destWidth / sourceImage.Width, (float) destHeight / sourceImage.Height, MatrixOrder.Append);
            }

            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using ImageAttributes wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(sourceImage, new NativeRect(destX, destY, destWidth, destHeight), 0, 0, sourceImage.Width, sourceImage.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return newImage;
        }
        
        /// <summary>
        /// Rotate the image
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="rotationAngle">Angle in degrees</param>
        /// <returns>Rotated image</returns>
        public static Image Rotate(this Image image, float rotationAngle)
        {
            var bitmap = CreateEmptyLike(image, Color.Transparent);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            graphics.TranslateTransform((float)bitmap.Width / 2, (float)bitmap.Height / 2);
            graphics.RotateTransform(rotationAngle);
            graphics.TranslateTransform(-(float)bitmap.Width / 2, -(float)bitmap.Height / 2);

            graphics.DrawImage(image, new NativePoint(0, 0));

            return bitmap;
        }

        /// <summary>
        /// Map a System.Drawing.Imaging.PixelFormat to a System.Windows.Media.PixelFormat
        /// </summary>
        /// <param name="pixelFormat">System.Drawing.Imaging.PixelFormat</param>
        /// <returns>System.Windows.Media.PixelFormat</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static System.Windows.Media.PixelFormat Map(this PixelFormat pixelFormat) =>
            pixelFormat switch
            {
                PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
                PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
                PixelFormat.Format32bppRgb => PixelFormats.Bgr32,
                _ => throw new NotSupportedException($"Can't map {pixelFormat}.")
            };

        /// <summary>
        /// Map a System.Windows.Media.PixelFormat to a System.Drawing.Imaging.PixelFormat
        /// </summary>
        /// <param name="pixelFormat">System.Windows.Media.PixelFormat</param>
        /// <returns>System.Drawing.Imaging.PixelFormat</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static PixelFormat Map(this System.Windows.Media.PixelFormat pixelFormat)
        {
            if (pixelFormat == PixelFormats.Bgra32)
            {
                return PixelFormat.Format32bppArgb;
            }
            if (pixelFormat == PixelFormats.Bgr24)
            {
                return PixelFormat.Format24bppRgb;
            }
            if (pixelFormat == PixelFormats.Bgr32)
            {
                return PixelFormat.Format32bppRgb;
            }

            throw new NotSupportedException($"Can't map {pixelFormat}.");
        }

        /// <summary>
        /// Convert a Bitmap to a BitmapSource
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <returns>BitmapSource</returns>
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(new NativeRect(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

            BitmapSource bitmapSource;
            try
            {
                bitmapSource = BitmapSource.Create(
                    bitmapData.Width, bitmapData.Height,
                    bitmap.HorizontalResolution, bitmap.VerticalResolution,
                    bitmap.PixelFormat.Map(), null,
                    bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmapSource;
        }

        /// <summary>
        /// Convert a BitmapSource to a Bitmap
        /// </summary>
        /// <param name="bitmapSource">BitmapSource</param>
        /// <returns>Bitmap</returns>
        public static Bitmap ToBitmap(this BitmapSource bitmapSource)
        {
            var pixelFormat = bitmapSource.Format.Map();

            Bitmap bitmap = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, pixelFormat);
            BitmapData data = bitmap.LockBits(new NativeRect(NativePoint.Empty, bitmap.Size), ImageLockMode.WriteOnly, pixelFormat);
            bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bitmap.UnlockBits(data);
            return bitmap;
        }
    }
}