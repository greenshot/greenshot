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
using System.Runtime.InteropServices;
using Greenshot.Base.Native;
using Greenshot.Base.Native.DirectX;
using Xunit;

namespace Greenshot.Tests.Core;

/// <summary>
/// The HDR tone mapping with known scRGB values, no HDR monitor needed.
/// scRGB 1.0 is 80 nits, with an SDR white level of 200 nits SDR white is 2.5.
/// </summary>
public class HdrCpuToneMapperTests
{
    private const float SdrWhiteNits = 200f;
    private const float SdrWhite = SdrWhiteNits / 80f;

    // The linear value of sRGB 128
    private static readonly float Grey128 = (float)Math.Pow((128 / 255.0 + 0.055) / 1.055, 2.4);

    [Fact]
    public void SdrOnly_KeepsEveryColor()
    {
        using var bitmap = ToneMap(SdrWhiteNits,
            Rgba(SdrWhite, SdrWhite, SdrWhite),
            Rgba(Grey128 * SdrWhite, Grey128 * SdrWhite, Grey128 * SdrWhite),
            Rgba(0, 0, 0),
            Rgba(SdrWhite, 0, 0));

        Assert.Equal(Color.FromArgb(255, 255, 255, 255), bitmap.GetPixel(0, 0));
        Assert.Equal(Color.FromArgb(255, 128, 128, 128), bitmap.GetPixel(1, 0));
        Assert.Equal(Color.FromArgb(255, 0, 0, 0), bitmap.GetPixel(2, 0));
        Assert.Equal(Color.FromArgb(255, 255, 0, 0), bitmap.GetPixel(3, 0));
    }

    [Fact]
    public void SdrWhiteLevel80_IsScRgbAsIs()
    {
        using var bitmap = ToneMap(80f, Rgba(1, 1, 1), Rgba(0.5f, 0.5f, 0.5f));

        Assert.Equal(Color.FromArgb(255, 255, 255, 255), bitmap.GetPixel(0, 0));
        // sRGB of linear 0.5
        Assert.Equal(Color.FromArgb(255, 188, 188, 188), bitmap.GetPixel(1, 0));
    }

    [Fact]
    public void HdrHighlights_AreCompressedWithTheirHue()
    {
        using var bitmap = ToneMap(SdrWhiteNits,
            Rgba(SdrWhite, SdrWhite, SdrWhite),
            Rgba(10 * SdrWhite, 5 * SdrWhite, 0),
            Rgba(Grey128 * SdrWhite, Grey128 * SdrWhite, Grey128 * SdrWhite));

        var white = bitmap.GetPixel(0, 0);
        var highlight = bitmap.GetPixel(1, 0);
        var grey = bitmap.GetPixel(2, 0);

        // The brightest pixel becomes the brightest color, keeping its orange: not cut to yellow or white
        Assert.Equal(Color.FromArgb(255, 255, 188, 0), highlight);
        // SDR white makes room for the highlight, but stays close to white
        Assert.InRange(white.R, 235, 254);
        Assert.Equal(white.R, white.G);
        Assert.Equal(white.R, white.B);
        // Below the knee nothing changes
        Assert.Equal(Color.FromArgb(255, 128, 128, 128), grey);
    }

    [Fact]
    public void BrighterInput_IsNeverDarker()
    {
        float previous = 0;
        for (float value = HdrCpuToneMapper.Knee; value <= 10; value += 0.05f)
        {
            float compressed = HdrCpuToneMapper.Compress(value, 1 / (46f * 46f));
            Assert.True(compressed >= previous, $"{value} became {compressed}, below {previous}");
            Assert.True(compressed <= 1.0f);
            previous = compressed;
        }
        Assert.Equal(HdrCpuToneMapper.Knee, HdrCpuToneMapper.Compress(HdrCpuToneMapper.Knee, 0.01f), 5);
        // The peak (here 10, white point (10 - 0.8) / 0.2 = 46) becomes white
        Assert.Equal(1.0f, HdrCpuToneMapper.Compress(10, 1 / (46f * 46f)), 4);
    }

    [Fact]
    public void PremultipliedAlpha_IsUndone()
    {
        using var bitmap = ToneMap(SdrWhiteNits, Rgba(SdrWhite / 2, SdrWhite / 2, SdrWhite / 2, 0.5f), Rgba(0, 0, 0, 0));

        Assert.Equal(Color.FromArgb(128, 255, 255, 255), bitmap.GetPixel(0, 0));
        Assert.Equal(0, bitmap.GetPixel(1, 0).A);
    }

    [Fact]
    public void NaNAndNegativeValues_BecomeBlack()
    {
        using var bitmap = ToneMap(SdrWhiteNits, Rgba(float.NaN, -1, 0), Rgba(SdrWhite, SdrWhite, SdrWhite, float.NaN));

        Assert.Equal(Color.FromArgb(255, 0, 0, 0), bitmap.GetPixel(0, 0));
        Assert.Equal(0, bitmap.GetPixel(1, 0).A);
    }

    [Fact]
    public void Fp16Texture_IsToneMappedOnTheWarpDevice()
    {
        // The way a captured frame goes: texture, staging copy, tone mapping. WARP works without GPU or HDR monitor.
        int hr = WindowsGraphicsCaptureInterop.CreateD3D11Device(WindowsGraphicsCaptureInterop.WarpDriverType, out var device, out var context);
        Assert.Equal(0, hr);
        var pixels = ToHalfs(Rgba(SdrWhite, SdrWhite, SdrWhite), Rgba(10 * SdrWhite, 5 * SdrWhite, 0));
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        var initialData = new D3D11_SUBRESOURCE_DATA { SysMem = handle.AddrOfPinnedObject(), SysMemPitch = 2 * 8 };
        var initialDataHandle = GCHandle.Alloc(initialData, GCHandleType.Pinned);
        ID3D11Texture2D texture = null;
        try
        {
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = 2,
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

            using var bitmap = WindowsGraphicsCaptureInterop.ToneMapHdrTextureToBitmap(texture, device, context, SdrWhiteNits);

            Assert.InRange(bitmap.GetPixel(0, 0).R, 235, 254);
            Assert.Equal(Color.FromArgb(255, 255, 188, 0), bitmap.GetPixel(1, 0));
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

    /// <summary>
    /// Tone map one row of pixels
    /// </summary>
    private static Bitmap ToneMap(float sdrWhiteLevelInNits, params float[][] pixels)
    {
        var halfs = ToHalfs(pixels);
        var handle = GCHandle.Alloc(halfs, GCHandleType.Pinned);
        try
        {
            return HdrCpuToneMapper.ToneMapFp16ToBitmap(handle.AddrOfPinnedObject(), pixels.Length * 8, pixels.Length, 1, sdrWhiteLevelInNits);
        }
        finally
        {
            handle.Free();
        }
    }

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
