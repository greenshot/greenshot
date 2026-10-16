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
using System.Threading.Tasks;
using log4net;

namespace Greenshot.Base.Native;

/// <summary>
/// Tone maps FP16 HDR pixels (scRGB) to an 8-bit sRGB bitmap.
/// </summary>
/// <remarks>
/// The values are scaled with the display's SDR white level, so SDR content (the desktop, most windows) sits at 1.0.
/// A capture without anything brighter than that keeps every color as it is on the screen.
/// When the capture shows brighter (HDR) content, everything above the knee is compressed so the brightest pixel
/// becomes white (or what the display can show at most, if that is less): highlights keep their detail and hue instead of being cut off, at the cost of SDR white becoming a little darker.
/// The color of a pixel is scaled as a whole (by its largest channel), so the hue stays.
/// </remarks>
internal static class HdrCpuToneMapper
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(HdrCpuToneMapper));

    /// <summary>
    /// Brightness (1.0 = SDR white) up to which colors stay as they are when the capture has a few HDR highlights:
    /// SDR white stays almost white
    /// </summary>
    internal const float Knee = 0.8f;

    /// <summary>
    /// The knee when much of the capture is brighter than SDR white (an HDR video or game): the highlights get more of the 8-bit steps,
    /// at the cost of SDR white becoming darker
    /// </summary>
    internal const float HighlightKnee = 0.5f;

    /// <summary>
    /// Share of pixels brighter than SDR white from which the knee moves from <see cref="Knee"/> towards <see cref="HighlightKnee"/>, and where it reaches it
    /// </summary>
    private const float FewHighlights = 0.05f;
    private const float MostlyHighlights = 0.25f;

    /// <summary>
    /// A capture whose brightest pixel stays below this is SDR only, the small margin covers FP16 rounding
    /// </summary>
    private const float SdrPeak = 1.01f;

    private const int SrgbTableSize = 16384;
    private static readonly byte[] SrgbTable = CreateSrgbTable();

    /// <summary>
    /// Every half-float as float: a lookup is much faster than converting the bits, for 4 channels of a screen full of pixels
    /// </summary>
    private static readonly float[] HalfTable = CreateHalfTable();

    /// <summary>
    /// Tone-maps an FP16 (R16G16B16A16_FLOAT, premultiplied scRGB) mapped texture to a 32bpp ARGB bitmap.
    /// </summary>
    /// <param name="mappedData">Pointer to the mapped staging texture data.</param>
    /// <param name="rowPitch">The row pitch (stride) of the mapped data in bytes.</param>
    /// <param name="width">Texture width in pixels.</param>
    /// <param name="height">Texture height in pixels.</param>
    /// <param name="sdrWhiteLevelInNits">The SDR white level in nits for this display.</param>
    /// <param name="displayPeakInNits">The brightest the display can show, 0 if unknown: content brighter than this looks the same on the screen, so it becomes white too.</param>
    /// <returns>A new Bitmap in Format32bppArgb with tone-mapped pixel data.</returns>
    public static unsafe Bitmap ToneMapFp16ToBitmap(IntPtr mappedData, int rowPitch, int width, int height, float sdrWhiteLevelInNits, float displayPeakInNits = 0)
    {
        // scRGB 1.0 is 80 nits, SDR white is at sdrWhiteLevelInNits
        float scale = sdrWhiteLevelInNits > 0 ? 80.0f / sdrWhiteLevelInNits : 1.0f;

        var (rawPeak, brighterThanSdrWhite) = FindPeak(mappedData, rowPitch, width, height, SdrPeak / scale);
        float pixelPeak = rawPeak * scale;
        // A display peak below SDR white is not plausible, 10000 nits is the limit of HDR10
        float displayPeak = displayPeakInNits > sdrWhiteLevelInNits && displayPeakInNits <= 10000 ? displayPeakInNits / sdrWhiteLevelInNits : float.MaxValue;
        float peak = Math.Min(pixelPeak, displayPeak);
        bool hasHdrContent = peak > SdrPeak;
        float highlightShare = (float)brighterThanSdrWhite / ((long)width * height);
        float knee = Knee - (Knee - HighlightKnee) * Math.Min(Math.Max((highlightShare - FewHighlights) / (MostlyHighlights - FewHighlights), 0), 1);
        // Above the knee the curve runs from the knee (slope 1) to 1.0 at the peak, see Compress
        float whitePoint = (peak - knee) / (1.0f - knee);
        float inverseWhitePointSquared = hasHdrContent ? 1.0f / (whitePoint * whitePoint) : 0;
        Log.Info($"HDR tone mapping {width}x{height}: SDR white {sdrWhiteLevelInNits} nits, brightest pixel {pixelPeak * sdrWhiteLevelInNits:F0} nits, display peak {displayPeakInNits:F0} nits{(hasHdrContent ? $", highlights compressed above {knee:F2} ({highlightShare:P0} brighter than SDR white)" : ", SDR only")}.");

        // SDR only: values above 1.0 are FP16 rounding, cut them
        float compressAbove = hasHdrContent ? knee : 1.0f;

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
        try
        {
            IntPtr destination = bmpData.Scan0;
            int stride = bmpData.Stride;
            // PARALLEL: rows are independent, a monitor full of pixels takes noticeably long on one core
            Parallel.For(0, height, y =>
            {
                ushort* src = (ushort*)((byte*)mappedData + (long)y * rowPitch);
                byte* dst = (byte*)destination + (long)y * stride;
                for (int x = 0; x < width; x++)
                {
                    float r = HalfTable[src[0]];
                    float g = HalfTable[src[1]];
                    float b = HalfTable[src[2]];
                    float a = HalfTable[src[3]];
                    // NaN fails the comparison and becomes transparent
                    a = a > 0 ? Math.Min(a, 1f) : 0f;

                    // Un-premultiply and scale, negative (out of gamut) values are clamped
                    float factor = a > 0.0001f ? scale / a : scale;
                    r = Math.Max(r * factor, 0f);
                    g = Math.Max(g * factor, 0f);
                    b = Math.Max(b * factor, 0f);

                    float max = Math.Max(r, Math.Max(g, b));
                    if (max > compressAbove)
                    {
                        float target = hasHdrContent ? Compress(max, knee, inverseWhitePointSquared) : Math.Min(max, 1.0f);
                        float toTarget = target / max;
                        r *= toTarget;
                        g *= toTarget;
                        b *= toTarget;
                    }

                    // Format32bppArgb is BGRA in memory
                    dst[0] = ToSrgb(b);
                    dst[1] = ToSrgb(g);
                    dst[2] = ToSrgb(r);
                    dst[3] = (byte)(a * 255f + 0.5f);

                    src += 4;
                    dst += 4;
                }
            });
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
        return bitmap;
    }

    /// <summary>
    /// Extended Reinhard above the knee: slope 1 at the knee (no visible edge), 1.0 at the peak
    /// </summary>
    internal static float Compress(float value, float knee, float inverseWhitePointSquared)
    {
        float x = (value - knee) / (1.0f - knee);
        return knee + (1.0f - knee) * Math.Min(x * (1.0f + x * inverseWhitePointSquared) / (1.0f + x), 1.0f);
    }

    /// <summary>
    /// The largest color channel (un-premultiplied) in the texture, NaN and infinity are ignored,
    /// and the number of pixels whose largest channel is above the threshold
    /// </summary>
    private static unsafe (float Peak, long AboveThreshold) FindPeak(IntPtr data, int rowPitch, int width, int height, float threshold)
    {
        float peak = 0;
        long aboveThreshold = 0;
        object resultLock = new object();
        // PARALLEL: rows are independent, each worker keeps its own result and merges it once
        Parallel.For(0, height, () => (Peak: 0f, AboveThreshold: 0L), (y, _, local) =>
        {
            ushort* src = (ushort*)((byte*)data + (long)y * rowPitch);
            for (int x = 0; x < width; x++, src += 4)
            {
                float max = Math.Max(HalfTable[src[0]], Math.Max(HalfTable[src[1]], HalfTable[src[2]]));
                float a = HalfTable[src[3]];
                if (a > 0.0001f && a < 1.0f)
                {
                    max /= a;
                }
                if (float.IsInfinity(max))
                {
                    continue;
                }
                if (max > local.Peak)
                {
                    local.Peak = max;
                }
                if (max > threshold)
                {
                    local.AboveThreshold++;
                }
            }
            return local;
        }, local =>
        {
            lock (resultLock)
            {
                peak = Math.Max(peak, local.Peak);
                aboveThreshold += local.AboveThreshold;
            }
        });
        return (peak, aboveThreshold);
    }

    private static byte ToSrgb(float linear)
    {
        // NaN fails the comparison and becomes black
        if (!(linear > 0))
        {
            return 0;
        }
        return SrgbTable[(int)(Math.Min(linear, 1.0f) * (SrgbTableSize - 1) + 0.5f)];
    }

    /// <summary>
    /// IEC 61966-2-1 sRGB transfer function, as a table: Math.Pow per channel is too slow for a screen full of pixels
    /// </summary>
    private static byte[] CreateSrgbTable()
    {
        var table = new byte[SrgbTableSize];
        for (int i = 0; i < SrgbTableSize; i++)
        {
            double c = (double)i / (SrgbTableSize - 1);
            double srgb = c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1.0 / 2.4) - 0.055;
            table[i] = (byte)(srgb * 255.0 + 0.5);
        }
        return table;
    }

    private static float[] CreateHalfTable()
    {
        var table = new float[ushort.MaxValue + 1];
        for (int i = 0; i <= ushort.MaxValue; i++)
        {
            table[i] = HalfHelper.HalfToFloat((ushort)i);
        }
        return table;
    }
}
