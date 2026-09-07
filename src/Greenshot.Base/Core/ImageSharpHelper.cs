/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Greenshot.Base.Core;

/// <summary>
/// Some methods to help with ImageSharp and GDI+ interop.
/// This is a critical component for Greenshot's image processing pipeline, as it allows us to leverage ImageSharp's powerful features while maintaining compatibility with GDI+-based APIs and components.
/// </summary>
public static class ImageSharpHelper
{
    /// <summary>
    /// Convert from Bitmap to ImageSharp Image. This method handles various pixel formats and ensures the resulting ImageSharp Image has the correct pixel format for efficient processing.
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <returns>Image</returns>
    public static Image ConvertToImageSharp(Bitmap bitmap)
    {
        if (bitmap == null) return null;

        switch (bitmap.PixelFormat)
        {
            // ----------------------------------------------------------------
            // CASE 1: 24-bit (No Alpha)
            // Use Bgr24. Direct copy. No conversion.
            // CASE 2: 32-bit (No Alpha)
            // Use Bgr24. Some conversion, but more efficient in the usage.
            // ----------------------------------------------------------------
            case PixelFormat.Format24bppRgb:
            case PixelFormat.Format32bppRgb:
                return ConvertFrom<Bgr24>(bitmap, PixelFormat.Format24bppRgb);

            // ----------------------------------------------------------------
            // CASE 3: 32-bit (Standard Alpha)
            // Use Bgra32. Direct copy. No conversion.
            // ----------------------------------------------------------------
            case PixelFormat.Format32bppArgb:
                return ConvertFrom<Bgra32>(bitmap, PixelFormat.Format32bppArgb);

            // ----------------------------------------------------------------
            // CASE 4: Weird Formats (Indexed, Premultiplied, 16-bit, etc.)
            // Force GDI+ to convert these to standard 32-bit BGRA.
            // This incurs a copy/convert cost, but it's required for correctness.
            // ----------------------------------------------------------------
            case PixelFormat.Format32bppPArgb: // Un-premultiply
            case PixelFormat.Format8bppIndexed: // Expand Palette
            default:
                return ConvertFrom<Bgra32>(bitmap, PixelFormat.Format32bppArgb);
        }
    }

    /// <summary>
    /// Loads an image from a GDI+ Bitmap into a new ImageSharp Image container of the specified pixel format.
    /// </summary>
    /// <remarks>This method locks the source Bitmap for read-only access, copies its pixel data
    /// directly into a new ImageSharp Image<TPixel> instance, and ensures the Bitmap is properly unlocked after the
    /// operation. The caller is responsible for disposing of the returned image when it is no longer
    /// needed.</remarks>
    /// <typeparam name="TPixel">The pixel type of the resulting image. Must be an unmanaged type that implements the IPixel<TPixel>
    /// interface.</typeparam>
    /// <param name="bitmap">The source GDI+ Bitmap from which to load pixel data. Cannot be null.</param>
    /// <param name="lockFormat">The pixel format to use when locking the Bitmap for reading. Determines how pixel data is accessed during
    /// the copy operation.</param>
    /// <returns>An Image<TPixel> instance containing the pixel data copied from the specified Bitmap.</returns>
    private static unsafe Image<TPixel> ConvertFrom<TPixel>(Bitmap bitmap, PixelFormat lockFormat) where TPixel : unmanaged, IPixel<TPixel>
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // 1. Create ImageSharp container
        var image = new Image<TPixel>(width, height);

        // 2. Lock GDI+ Bitmap
        var rect = new NativeRect(0, 0, width, height);
        BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, lockFormat);

        try
        {
            // 3. Unsafe Direct Copy
            image.ProcessPixelRows(accessor =>
            {
                byte* sourcePtr = (byte*)bmpData.Scan0;
                int sourceStride = bmpData.Stride;

                for (int y = 0; y < height; y++)
                {
                    var sourceRow = new ReadOnlySpan<TPixel>(
                        sourcePtr + (y * sourceStride),
                        width);

                    var targetRow = accessor.GetRowSpan(y);
                    sourceRow.CopyTo(targetRow);
                }
            });
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }

        return image;
    }

    /// <summary>
    /// Converts the specified ImageSharp Image to a Bitmap that is compatible with GDI+ operations.
    /// </summary>
    /// <remarks>This method supports multiple image pixel formats, including Bgra32, Bgr24, and
    /// Rgba32. If the image format is not directly supported, the method converts the image to Bgra32 before
    /// creating the Bitmap. The returned Bitmap can be used with GDI+ APIs that require a System.Drawing.Bitmap
    /// instance.</remarks>
    /// <param name="image">The Image to convert. This parameter must not be null.</param>
    /// <returns>A Bitmap representation of the specified Image, or null if the input image is null.</returns>
    public static Bitmap ToBitmap(Image image)
    {
        if (image == null) return null;

        // ---------------------------------------------------------------------
        // Case 1: Image is Bgra32 (Native GDI+ format)
        // This is the fastest path. Direct Block Copy.
        // ---------------------------------------------------------------------
        if (image is Image<Bgra32> imgBgra32)
        {
            return ConvertToBitmap<Bgra32>(imgBgra32, PixelFormat.Format32bppArgb);
        }

        // ---------------------------------------------------------------------
        // Case 2: Image is Bgr24 (Native GDI+ 24-bit format)
        // ---------------------------------------------------------------------
        if (image is Image<Bgr24> imgBgr24)
        {
            return ConvertToBitmap<Bgr24>(imgBgr24, PixelFormat.Format24bppRgb);
        }

        // ---------------------------------------------------------------------
        // Case 3: Image is Rgba32 (Default ImageSharp format)
        // Problem: GDI+ expects BGRA. 
        // Solution: We clone/convert it to Bgra32 first.
        // ---------------------------------------------------------------------
        if (image is Image<Rgba32> imgRgba32)
        {
            // We create a temporary Bgra32 copy. 
            // This handles the R <-> B swap automatically and efficiently.
            using (var tempImg = imgRgba32.CloneAs<Bgra32>())
            {
                return ConvertToBitmap<Bgra32>(tempImg, PixelFormat.Format32bppArgb);
            }
        }

        // ---------------------------------------------------------------------
        // Case 4: Any other format (L8, Rgb24, etc.)
        // Fallback: Convert to Bgra32 universal format
        // ---------------------------------------------------------------------
        using (var tempImg = image.CloneAs<Bgra32>())
        {
            return ConvertToBitmap<Bgra32>(tempImg, PixelFormat.Format32bppArgb);
        }
    }

    /// <summary>
    /// Convert the ImageSharp Image<TPixel> to a GDI+ Bitmap. This is done by locking the Bitmap and copying pixel data row-by-row.
    /// </summary>
    /// <typeparam name="TPixel">The pixel-format-type for the ImageSharp Image</typeparam>
    /// <param name="sourceImage">Image from ImageSharp</param>
    /// <param name="targetFormat">Formal used for the System.Drawing.Bitmap</param>
    /// <returns>Bitmap</returns>
    private static unsafe Bitmap ConvertToBitmap<TPixel>(this Image<TPixel> sourceImage, PixelFormat targetFormat) where TPixel : unmanaged, IPixel<TPixel>
    {
        if (sourceImage == null) return null;

        int width = sourceImage.Width;
        int height = sourceImage.Height;

        // 1. Create the GDI+ Bitmap (Allocates unmanaged memory)
        Bitmap bmp = new Bitmap(width, height, targetFormat);

        // 2. Lock the Bitmap to get the pointer
        BitmapData bmpData = bmp.LockBits(
            new NativeRect(0, 0, width, height),
            ImageLockMode.WriteOnly,
            targetFormat);

        try
        {
            // 3. Copy row-by-row from ImageSharp to GDI+
            sourceImage.ProcessPixelRows(accessor =>
            {
                byte* targetPtr = (byte*)bmpData.Scan0;
                int targetStride = bmpData.Stride;

                for (int y = 0; y < height; y++)
                {
                    // Get the row from ImageSharp
                    var sourceRow = accessor.GetRowSpan(y);
                    // Calculate target address for this row
                    // We must use a Span to copy safely into unmanaged memory
                    var targetRow = new Span<TPixel>(targetPtr + (y * targetStride), width);
                    // Copy!
                    sourceRow.CopyTo(targetRow);
                }
            });
        }
        finally
        {
            // 4. Unlock
            bmp.UnlockBits(bmpData);
        }

        return bmp;
    }
}
