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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Greenshot.Base.Native.DirectX;

namespace Greenshot.Base.Native;

/// <summary>
/// Decides which brightness becomes white in the 8-bit screenshot of an HDR display.
/// </summary>
/// <remarks>
/// Without HDR content (normal windows) SDR white stays white, so the colors are exactly as on the screen.
/// When much of the capture is brighter than SDR white (an HDR video or game) the whole capture is scaled so the display's
/// peak becomes white, like the Xbox Game Bar does: highlights keep their detail, SDR white becomes light grey for it.
/// In between it blends smoothly by the share of pixels brighter than SDR white.
/// </remarks>
internal static class HdrContent
{
    /// <summary>
    /// Share of pixels brighter than SDR white up to which SDR white stays white, and from which the display's peak is white
    /// </summary>
    private const float FewHighlights = 0.02f;
    private const float MostlyHighlights = 0.10f;

    /// <summary>
    /// A pixel within this factor of SDR white is SDR, the margin covers FP16 rounding
    /// </summary>
    private const float SdrMargin = 1.01f;

    private static readonly float[] HalfTable = CreateHalfTable();

    /// <summary>
    /// The brightness in nits which becomes white
    /// </summary>
    /// <param name="sdrWhiteLevelInNits">The display's SDR white level.</param>
    /// <param name="displayPeakInNits">The brightest the display can show, 0 if unknown.</param>
    /// <param name="peakInNits">The brightest pixel of the capture.</param>
    /// <param name="highlightShare">Share of pixels brighter than SDR white.</param>
    internal static float WhiteLevelInNits(float sdrWhiteLevelInNits, float displayPeakInNits, float peakInNits, float highlightShare)
    {
        // A display peak below SDR white is not plausible, 10000 nits is the limit of HDR10
        float peak = displayPeakInNits > sdrWhiteLevelInNits && displayPeakInNits <= 10000 ? Math.Min(peakInNits, displayPeakInNits) : peakInNits;
        if (peak <= sdrWhiteLevelInNits * SdrMargin)
        {
            return sdrWhiteLevelInNits;
        }
        float darken = Math.Min(Math.Max((highlightShare - FewHighlights) / (MostlyHighlights - FewHighlights), 0), 1);
        return sdrWhiteLevelInNits + (peak - sdrWhiteLevelInNits) * darken;
    }

    /// <summary>
    /// The brightest pixel in nits and the share of pixels brighter than SDR white, of every pixel:
    /// a single bright pixel (a thin line, a small light) must not be missed, it sets the input range of the tone mapping
    /// </summary>
    internal static unsafe (float PeakInNits, float HighlightShare) Measure(ID3D11Texture2D hdrTexture, ID3D11Device device, ID3D11DeviceContext context, float sdrWhiteLevelInNits)
    {
        hdrTexture.GetDesc(out D3D11_TEXTURE2D_DESC desc);
        int width = desc.Width;
        int height = desc.Height;
        var stagingDesc = new D3D11_TEXTURE2D_DESC
        {
            Width = width,
            Height = height,
            MipLevels = 1,
            ArraySize = 1,
            Format = desc.Format, // R16G16B16A16_FLOAT
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
            BindFlags = 0,
            CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
            MiscFlags = 0
        };
        device.CreateTexture2D(ref stagingDesc, IntPtr.Zero, out var stagingTexture);
        try
        {
            context.CopyResource(stagingTexture, hdrTexture);
            int hr = context.Map(stagingTexture, 0, D3D11_MAP.D3D11_MAP_READ, 0, out var mapped);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
            try
            {
                IntPtr data = mapped.pData;
                int rowPitch = mapped.RowPitch;
                // scRGB 1.0 is 80 nits
                float sdrWhite = sdrWhiteLevelInNits / 80f * SdrMargin;
                float peak = 0;
                long brighter = 0;
                long counted = 0;
                object resultLock = new object();
                // PARALLEL: rows are independent, each worker keeps its own result and merges it once
                Parallel.For(0, height, () => (Peak: 0f, Brighter: 0L, Counted: 0L), (row, _, local) =>
                {
                    ushort* src = (ushort*)((byte*)data + (long)row * rowPitch);
                    for (int x = 0; x < width; x++, src += 4)
                    {
                        float max = Math.Max(HalfTable[src[0]], Math.Max(HalfTable[src[1]], HalfTable[src[2]]));
                        float a = HalfTable[src[3]];
                        if (a > 0.0001f && a < 1.0f)
                        {
                            max /= a;
                        }
                        // NaN fails the comparisons
                        if (float.IsInfinity(max) || !(max >= 0))
                        {
                            continue;
                        }
                        local.Counted++;
                        if (max > local.Peak)
                        {
                            local.Peak = max;
                        }
                        if (max > sdrWhite)
                        {
                            local.Brighter++;
                        }
                    }
                    return local;
                }, local =>
                {
                    lock (resultLock)
                    {
                        peak = Math.Max(peak, local.Peak);
                        brighter += local.Brighter;
                        counted += local.Counted;
                    }
                });
                return (peak * 80f, counted > 0 ? (float)brighter / counted : 0);
            }
            finally
            {
                context.Unmap(stagingTexture, 0);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(stagingTexture);
        }
    }

    /// <summary>
    /// Every half-float as float: a lookup is much faster than converting the bits for a screen full of pixels
    /// </summary>
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
