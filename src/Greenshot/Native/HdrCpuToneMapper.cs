/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Native;

/// <summary>
/// CPU-based fallback tone mapper for converting FP16 HDR textures to 8-bit SDR bitmaps.
/// </summary>
/// <remarks>
/// This is the fallback path used when GPU (Direct2D) tone mapping fails.
/// Applies white-level normalization, simple Reinhard tone mapping, and gamma 2.2 encoding.
/// </remarks>
internal static class HdrCpuToneMapper
{
    /// <summary>
    /// Tone-maps an FP16 (R16G16B16A16_FLOAT) mapped texture to a 32bpp ARGB bitmap.
    /// </summary>
    /// <param name="mappedData">Pointer to the mapped staging texture data.</param>
    /// <param name="rowPitch">The row pitch (stride) of the mapped data in bytes.</param>
    /// <param name="width">Texture width in pixels.</param>
    /// <param name="height">Texture height in pixels.</param>
    /// <param name="sdrWhiteLevelInNits">The SDR white level in nits for this display.</param>
    /// <returns>A new Bitmap in Format32bppArgb with tone-mapped pixel data.</returns>
    public static unsafe Bitmap ToneMapFp16ToBitmap(
        IntPtr mappedData, int rowPitch,
        int width, int height, float sdrWhiteLevelInNits)
    {
        // whiteScale = sdrWhiteLevelInNits / 80.0f (the scRGB value that represents SDR white)
        float whiteScale = sdrWhiteLevelInNits / 80.0f;
        float invScale = whiteScale > 0.0001f ? 1.0f / whiteScale : 1.0f;

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            bitmap.PixelFormat);

        try
        {
            byte* srcRow = (byte*)mappedData;
            byte* dstRow = (byte*)bmpData.Scan0;

            for (int y = 0; y < height; y++)
            {
                ushort* src = (ushort*)srcRow;
                byte* dst = dstRow;

                for (int x = 0; x < width; x++)
                {
                    // Read RGBA as 4 half-floats (R16G16B16A16_FLOAT)
                    float r = HalfHelper.HalfToFloat(src[0]);
                    float g = HalfHelper.HalfToFloat(src[1]);
                    float b = HalfHelper.HalfToFloat(src[2]);
                    float a = HalfHelper.HalfToFloat(src[3]);

                    // Un-premultiply alpha (WGC provides premultiplied scRGB)
                    if (a > 0.0001f)
                    {
                        r /= a;
                        g /= a;
                        b /= a;
                    }

                    // Normalize by SDR white level
                    r *= invScale;
                    g *= invScale;
                    b *= invScale;

                    // Clamp negative values (out-of-gamut)
                    r = Math.Max(r, 0f);
                    g = Math.Max(g, 0f);
                    b = Math.Max(b, 0f);

                    // Simple Reinhard tone mapping: v / (1 + v)
                    // Compresses the entire HDR range into [0, 1)
                    r = r / (1.0f + r);
                    g = g / (1.0f + g);
                    b = b / (1.0f + b);

                    // Gamma 2.2 encoding
                    r = (float)Math.Pow(r, 1.0 / 2.2);
                    g = (float)Math.Pow(g, 1.0 / 2.2);
                    b = (float)Math.Pow(b, 1.0 / 2.2);

                    // Clamp alpha to [0, 1]
                    a = Math.Min(Math.Max(a, 0f), 1f);

                    // Write as BGRA (Format32bppArgb stores pixels as BGRA in memory)
                    dst[0] = (byte)(Math.Min(b, 1f) * 255f + 0.5f); // B
                    dst[1] = (byte)(Math.Min(g, 1f) * 255f + 0.5f); // G
                    dst[2] = (byte)(Math.Min(r, 1f) * 255f + 0.5f); // R
                    dst[3] = (byte)(a * 255f + 0.5f);               // A

                    src += 4; // Next pixel (4 half-floats = 8 bytes)
                    dst += 4; // Next pixel (4 bytes)
                }

                srcRow += rowPitch;
                dstRow += bmpData.Stride;
            }
        }
        catch
        {
            bitmap.UnlockBits(bmpData);
            bitmap.Dispose();
            throw;
        }

        bitmap.UnlockBits(bmpData);
        return bitmap;
    }
}
