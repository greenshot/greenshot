#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Gfx
{
    /// <summary>
    /// Create images
    /// </summary>
    public static class BitmapFactory
    {
        private static readonly LogSource Log = new LogSource();
        /// <summary>
        ///     A generic way to create an empty image
        /// </summary>
        /// <param name="sourceImage">the source bitmap as the specifications for the new bitmap</param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <returns>Bitmap</returns>
        public static Bitmap CreateEmptyLike(this Image sourceImage, Color? backgroundColor = null)
        {
            var pixelFormat = sourceImage.PixelFormat;
            if (backgroundColor.HasValue && backgroundColor.Value.A < 255)
            {
                pixelFormat = PixelFormat.Format32bppArgb;
            }
            return CreateEmpty(sourceImage.Width, sourceImage.Height, pixelFormat, backgroundColor, sourceImage.HorizontalResolution, sourceImage.VerticalResolution);
        }

        /// <summary>
        ///     A generic way to create an empty image
        /// </summary>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <param name="format">PixelFormat</param>
        /// <param name="backgroundColor">The color to fill with, or Color.Empty to take the default depending on the pixel format</param>
        /// <param name="horizontalResolution">float</param>
        /// <param name="verticalResolution">float</param>
        /// <returns>Bitmap</returns>
        public static Bitmap CreateEmpty(int width, int height, PixelFormat format = PixelFormat.Format32bppArgb, Color? backgroundColor = null, float horizontalResolution = 96f, float verticalResolution = 96f)
        {
            // Create a new "clean" image
            var newImage = new Bitmap(width, height, format);
            newImage.SetResolution(horizontalResolution, verticalResolution);
            if (format == PixelFormat.Format8bppIndexed)
            {
                return newImage;
            }
            using (var graphics = Graphics.FromImage(newImage))
            {
                // Make sure the background color is what we want (transparent or white, depending on the pixel format)
                if (backgroundColor.HasValue && !Color.Empty.Equals(backgroundColor))
                {
                    graphics.Clear(backgroundColor.Value);
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
        ///     Clone a bitmap, taking some rules into account:
        ///     1) When sourceRect is the whole bitmap there is a GDI+ bug in Clone
        ///     Clone will than return the same PixelFormat as the source
        ///     a quick workaround is using new Bitmap which uses a default of Format32bppArgb
        ///     2) When going from a transparent to a non transparent bitmap, we draw the background white!
        /// </summary>
        /// <param name="sourceBitmap">Source bitmap to clone</param>
        /// <param name="sourceRectangle">NativeRect to copy from the source, use NativeRect.Empty for all</param>
        /// <param name="targetFormat">
        ///     Target Format, use PixelFormat.DontCare if you want the original (or a default if the source
        ///     PixelFormat is not supported)
        /// </param>
        /// <returns>Bitmap</returns>
        public static Bitmap CloneBitmap(this Bitmap sourceBitmap, PixelFormat targetFormat = PixelFormat.DontCare, NativeRect? sourceRectangle = null)
        {
            Bitmap newImage;
            var sourceRect = sourceRectangle ?? NativeRect.Empty;

            var bitmapRect = new NativeRect(0, 0, sourceBitmap.Width, sourceBitmap.Height);

            // Make sure the source is not NativeRect.Empty
            if (NativeRect.Empty.Equals(sourceRect))
            {
                sourceRect = new NativeRect(0, 0, sourceBitmap.Width, sourceBitmap.Height);
            }
            else
            {
                sourceRect = sourceRect.Intersect(bitmapRect);
            }

            // If no pixelformat is supplied 
            if (PixelFormat.DontCare == targetFormat || PixelFormat.Undefined == targetFormat)
            {
                if (sourceBitmap.PixelFormat.IsPixelFormatSupported())
                {
                    targetFormat = sourceBitmap.PixelFormat;
                }
                else if (Image.IsAlphaPixelFormat(sourceBitmap.PixelFormat))
                {
                    targetFormat = PixelFormat.Format32bppArgb;
                }
                else
                {
                    targetFormat = PixelFormat.Format24bppRgb;
                }
            }

            // check the target format
            if (!targetFormat.IsPixelFormatSupported())
            {
                targetFormat = Image.IsAlphaPixelFormat(targetFormat) ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
            }

            var destinationIsTransparent = Image.IsAlphaPixelFormat(targetFormat);
            var sourceIsTransparent = Image.IsAlphaPixelFormat(sourceBitmap.PixelFormat);
            var fromTransparentToNon = !destinationIsTransparent && sourceIsTransparent;
            var isAreaEqual = sourceRect.Equals(bitmapRect);
            if (isAreaEqual || fromTransparentToNon)
            {
                // Rule 1: if the areas are equal, always copy ourselves
                newImage = new Bitmap(bitmapRect.Width, bitmapRect.Height, targetFormat);
                // Make sure both images have the same resolution
                newImage.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);

                using (var graphics = Graphics.FromImage(newImage))
                {
                    if (fromTransparentToNon)
                    {
                        // Rule 2: Make sure the background color is white
                        graphics.Clear(Color.White);
                    }
                    // decide fastest copy method
                    if (isAreaEqual)
                    {
                        graphics.DrawImageUnscaled(sourceBitmap, 0, 0);
                    }
                    else
                    {
                        graphics.DrawImage(sourceBitmap, 0, 0, sourceRect, GraphicsUnit.Pixel);
                    }
                }
            }
            else
            {
                // Let GDI+ decide how to convert, need to test what is quicker...
                newImage = sourceBitmap.Clone(sourceRect, targetFormat);
                // Make sure both images have the same resolution
                newImage.SetResolution(sourceBitmap.HorizontalResolution, sourceBitmap.VerticalResolution);
            }
            // Clone property items (EXIF information etc)
            foreach (var propertyItem in sourceBitmap.PropertyItems)
            {
                try
                {
                    newImage.SetPropertyItem(propertyItem);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Problem cloning a propertyItem.");
                }
            }
            return newImage;
        }

    }
}
