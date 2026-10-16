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
using System.Linq;
using System.Runtime.InteropServices;
using Greenshot.Base.Native;
using Greenshot.Base.Native.DirectX;
using Xunit;

namespace Greenshot.Tests.Core;

/// <summary>
/// The HDR tone mapping (Direct2D HdrToneMap and WhiteLevelAdjustment) with known scRGB values on the WARP device:
/// no GPU or HDR monitor needed. scRGB 1.0 is 80 nits, with an SDR white level of 200 nits SDR white is 2.5.
/// </summary>
public class HdrToneMapperTests
{
    private const float SdrWhiteNits = 200f;
    private const float SdrWhite = SdrWhiteNits / 80f;

    [Fact]
    public void HdrHighlights_KeepTheirHue()
    {
        using var bitmap = ToneMapOnWarp(SdrWhiteNits, 1000f,
            Rgba(SdrWhite, SdrWhite, SdrWhite),
            Rgba(4 * SdrWhite, 2 * SdrWhite, 0));

        var white = bitmap.GetPixel(0, 0);
        var highlight = bitmap.GetPixel(1, 0);
        Assert.InRange(white.R, 200, 255);
        // Orange stays orange: not cut to yellow per channel
        Assert.InRange(highlight.R, 230, 255);
        Assert.InRange(highlight.G, 100, highlight.R - 20);
        Assert.InRange(highlight.B, 0, 20);
    }

    [Fact]
    public void BrighterInput_IsNeverDarker()
    {
        var values = new[] { 0f, 0.1f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f, 5f };
        using var bitmap = ToneMapOnWarp(SdrWhiteNits, 1000f, values.Select(value => Rgba(value * SdrWhite, value * SdrWhite, value * SdrWhite)).ToArray());

        Assert.Equal(0, bitmap.GetPixel(0, 0).R);
        for (int x = 1; x < values.Length; x++)
        {
            Assert.True(bitmap.GetPixel(x, 0).R >= bitmap.GetPixel(x - 1, 0).R, $"{values[x]} x SDR white became darker than {values[x - 1]} x SDR white");
        }
    }

    [Fact]
    public void UnknownDisplayPeak_StillToneMaps()
    {
        using var bitmap = ToneMapOnWarp(SdrWhiteNits, 0, Rgba(SdrWhite, SdrWhite, SdrWhite), Rgba(10 * SdrWhite, 10 * SdrWhite, 10 * SdrWhite));

        Assert.True(bitmap.GetPixel(1, 0).R >= bitmap.GetPixel(0, 0).R);
    }

    /// <summary>
    /// The way a captured frame goes: FP16 texture, Direct2D effects, 8-bit texture, bitmap
    /// </summary>
    private static Bitmap ToneMapOnWarp(float sdrWhiteLevelInNits, float displayPeakInNits, params float[][] pixels)
    {
        int hr = WindowsGraphicsCaptureInterop.CreateD3D11Device(WindowsGraphicsCaptureInterop.WarpDriverType, out var device, out var context);
        Assert.Equal(0, hr);
        var halfs = ToHalfs(pixels);
        var handle = GCHandle.Alloc(halfs, GCHandleType.Pinned);
        var initialData = new D3D11_SUBRESOURCE_DATA { SysMem = handle.AddrOfPinnedObject(), SysMemPitch = pixels.Length * 8 };
        var initialDataHandle = GCHandle.Alloc(initialData, GCHandleType.Pinned);
        ID3D11Texture2D texture = null;
        try
        {
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = pixels.Length,
                Height = 1,
                MipLevels = 1,
                ArraySize = 1,
                Format = 10, // DXGI_FORMAT_R16G16B16A16_FLOAT
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = 0x8, // D3D11_BIND_SHADER_RESOURCE
                CPUAccessFlags = 0,
                MiscFlags = 0
            };
            device.CreateTexture2D(ref desc, initialDataHandle.AddrOfPinnedObject(), out texture);
            return WindowsGraphicsCaptureInterop.ToneMapHdrTextureToBitmap(texture, device, context, sdrWhiteLevelInNits, displayPeakInNits);
        }
        finally
        {
            if (texture != null) Marshal.ReleaseComObject(texture);
            initialDataHandle.Free();
            handle.Free();
            Marshal.ReleaseComObject(context);
            Marshal.ReleaseComObject(device);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11_SUBRESOURCE_DATA
    {
        public IntPtr SysMem;
        public int SysMemPitch;
        public int SysMemSlicePitch;
    }

    private static float[] Rgba(float r, float g, float b, float a = 1) => new[] { r, g, b, a };

    private static ushort[] ToHalfs(params float[][] pixels)
    {
        var halfs = new ushort[pixels.Length * 4];
        for (int i = 0; i < pixels.Length; i++)
        {
            for (int channel = 0; channel < 4; channel++)
            {
                halfs[i * 4 + channel] = ToHalf(pixels[i][channel]);
            }
        }
        return halfs;
    }

    /// <summary>
    /// Float to IEEE 754 half, rounded, good enough for the test values (no subnormals)
    /// </summary>
    private static ushort ToHalf(float value)
    {
        uint bits = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
        uint sign = (bits >> 16) & 0x8000u;
        if (float.IsNaN(value))
        {
            return (ushort)(sign | 0x7E00u);
        }
        int exponent = (int)((bits >> 23) & 0xFF) - 127 + 15;
        uint mantissa = bits & 0x7FFFFFu;
        if (exponent <= 0)
        {
            return (ushort)sign;
        }
        if (exponent >= 31)
        {
            return (ushort)(sign | 0x7C00u);
        }
        uint half = sign | ((uint)exponent << 10) | (mantissa >> 13);
        if ((mantissa & 0x1000u) != 0)
        {
            half++;
        }
        return (ushort)half;
    }
}
