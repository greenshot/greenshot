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
using System.Threading.Tasks;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Dapplo.Windows.User32;
using Xunit;
using Greenshot.Base.Native.DirectX;
using Greenshot.Base.Native;

namespace Greenshot.Tests.Core;

/// <summary>
/// A custom xUnit Fact attribute that dynamically marks the test as Skipped
/// when HDR is not enabled on the primary display.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class HdrFactAttribute : FactAttribute
{
    public HdrFactAttribute([CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
    {
        try
        {
            IntPtr primaryMonitor = HdrDisplayInfo.GetMonitorForWindow(IntPtr.Zero);
            if (!HdrDisplayInfo.IsHdrActiveForMonitor(primaryMonitor))
            {
                Skip = "HDR is not currently enabled on the display.";
            }
        }
        catch (Exception ex)
        {
            Skip = $"Failed to query display HDR status: {ex.Message}";
        }
    }
}

public class WindowsGraphicsCaptureTests
{
    private readonly ITestOutputHelper _output;

    public WindowsGraphicsCaptureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestDisplayHdrDetection()
    {
        IntPtr primaryMonitor = HdrDisplayInfo.GetMonitorForWindow(IntPtr.Zero);
        _output.WriteLine($"Primary monitor handle: {primaryMonitor}");
        bool isHdr = HdrDisplayInfo.IsHdrActiveForMonitor(primaryMonitor);
        float sdrWhite = HdrDisplayInfo.GetSdrWhiteLevelInNits(primaryMonitor);
        _output.WriteLine($"Monitor HDR active: {isHdr}, SDR white level: {sdrWhite} nits");
    }

    [HdrFact]
    public void TestGpuToneMapperWithFp16Texture()
    {
        // 1. Create D3D11 Device
        int hr = D3D11CreateDevice(
            IntPtr.Zero,
            1, // D3D_DRIVER_TYPE_HARDWARE
            IntPtr.Zero,
            0x20, // D3D11_CREATE_DEVICE_BGRA_SUPPORT
            null,
            0,
            7, // D3D11_SDK_VERSION
            out var d3d11Device,
            out var featureLevel,
            out var context);

        _output.WriteLine($"D3D11CreateDevice hr: 0x{hr:X8}");
        Assert.Equal(0, hr);
        Assert.NotNull(d3d11Device);

        try
        {
            // 2. Create a test FP16 texture (64x64)
            var hdrDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = 64,
                Height = 64,
                MipLevels = 1,
                ArraySize = 1,
                Format = 10, // DXGI_FORMAT_R16G16B16A16_FLOAT
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = 0x20 | 0x8, // BIND_RENDER_TARGET | BIND_SHADER_RESOURCE
                CPUAccessFlags = (D3D11_CPU_ACCESS_FLAG)0,
                MiscFlags = 0
            };

            d3d11Device.CreateTexture2D(ref hdrDesc, IntPtr.Zero, out var hdrTexture);
            Assert.NotNull(hdrTexture);

            try
            {
                _output.WriteLine("Creating HdrToneMapper...");
                using var toneMapper = new HdrToneMapper(d3d11Device);
                _output.WriteLine("HdrToneMapper created successfully.");

                _output.WriteLine("Calling ToneMapToSdr...");
                var sdrTexture = toneMapper.ToneMapToSdr(hdrTexture, d3d11Device, 240.0f, 64, 64);
                _output.WriteLine("ToneMapToSdr succeeded!");
                Assert.NotNull(sdrTexture);
                Marshal.ReleaseComObject(sdrTexture);
            }
            finally
            {
                Marshal.ReleaseComObject(hdrTexture);
            }
        }
        finally
        {
            if (context != null) Marshal.ReleaseComObject(context);
            if (d3d11Device != null) Marshal.ReleaseComObject(d3d11Device);
        }
    }

    [HdrFact]
    public async Task TestCaptureMonitorUsingGpuToneMapper()
    {
        IntPtr primaryMonitor = HdrDisplayInfo.GetMonitorForWindow(IntPtr.Zero);
        _output.WriteLine($"Capturing monitor {primaryMonitor}...");

        using var bitmap = await WindowsGraphicsCaptureInterop.CaptureMonitorToBitmapAsync(primaryMonitor);
        _output.WriteLine($"Capture result: {(bitmap != null ? $"{bitmap.Width}x{bitmap.Height}" : "null")}");
        Assert.NotNull(bitmap);
    }

    [HdrFact]
    public void TestGpuToneMapperOnRealCaptureFrame()
    {
        IntPtr primaryMonitor = HdrDisplayInfo.GetMonitorForWindow(IntPtr.Zero);
        _output.WriteLine($"Testing on monitor {primaryMonitor}...");

        WindowsGraphicsCaptureInterop.CreateD3D11Device(out var d3d11Device, out var context);
        try
        {
            var captureItem = WindowsGraphicsCaptureInterop.CreateCaptureItemForMonitor(primaryMonitor);
            Assert.NotNull(captureItem);

            var d3dInterOpDevice = WindowsGraphicsCaptureInterop.CreateID3DDeviceFromD3D11Device(d3d11Device);
            using var framePool = Windows.Graphics.Capture.Direct3D11CaptureFramePool.CreateFreeThreaded(
                d3dInterOpDevice,
                Windows.Graphics.DirectX.DirectXPixelFormat.R16G16B16A16Float,
                1,
                captureItem.Size);
            using var session = framePool.CreateCaptureSession(captureItem);
            session.IsCursorCaptureEnabled = false;

            using var frameArrivedEvent = new System.Threading.ManualResetEvent(false);
            framePool.FrameArrived += (s, e) => frameArrivedEvent.Set();
            session.StartCapture();

            Assert.True(frameArrivedEvent.WaitOne(2000), "Timeout waiting for FrameArrived");

            using var frame = framePool.TryGetNextFrame();
            Assert.NotNull(frame);

            var texture = WindowsGraphicsCaptureInterop.CreateTexture2DFromID3DSurface(frame.Surface);
            Assert.NotNull(texture);
            try
            {
                texture.GetDesc(out var desc);
                _output.WriteLine($"Captured texture: {desc.Width}x{desc.Height}, Format={desc.Format}, BindFlags=0x{desc.BindFlags:X}, Usage={desc.Usage}, MiscFlags=0x{desc.MiscFlags:X}");

                _output.WriteLine("Instantiating HdrToneMapper...");
                using var toneMapper = new HdrToneMapper(d3d11Device);
                _output.WriteLine("HdrToneMapper instantiated.");

                float sdrWhite = HdrDisplayInfo.GetSdrWhiteLevelInNits(primaryMonitor);
                _output.WriteLine($"Calling ToneMapToSdr with sdrWhite={sdrWhite}...");

                var sdrTexture = toneMapper.ToneMapToSdr(texture, d3d11Device, sdrWhite, desc.Width, desc.Height);
                _output.WriteLine("ToneMapToSdr succeeded!");
                Assert.NotNull(sdrTexture);

                _output.WriteLine("Calling TransformTextureToBitmap on sdrTexture...");
                using var gpuBitmap = WindowsGraphicsCaptureInterop.TransformTextureToBitmap(sdrTexture, d3d11Device, context);
                _output.WriteLine($"GPU Bitmap converted: {gpuBitmap.Width}x{gpuBitmap.Height}");
                Assert.NotNull(gpuBitmap);

                Marshal.ReleaseComObject(sdrTexture);

                _output.WriteLine("Calling CpuToneMapFp16...");
                using var cpuBitmap = WindowsGraphicsCaptureInterop.CpuToneMapFp16(texture, d3d11Device, context, sdrWhite, desc.Width, desc.Height);
                _output.WriteLine($"CPU Bitmap converted: {cpuBitmap.Width}x{cpuBitmap.Height}");
                Assert.NotNull(cpuBitmap);

                // Verify GPU matches CPU within rounding tolerance (diff <= 1) across the entire screen
                int maxDiff = 0;
                long totalDiff = 0;
                int pixelCount = 0;
                int step = 10;
                for (int y = 0; y < desc.Height; y += step)
                {
                    for (int x = 0; x < desc.Width; x += step)
                    {
                        Color gCol = gpuBitmap.GetPixel(x, y);
                        Color cCol = cpuBitmap.GetPixel(x, y);
                        int diff = Math.Max(Math.Abs(gCol.R - cCol.R), Math.Max(Math.Abs(gCol.G - cCol.G), Math.Abs(gCol.B - cCol.B)));
                        if (diff > maxDiff) maxDiff = diff;
                        totalDiff += diff;
                        pixelCount++;
                    }
                }
                float avgDiff = (float)totalDiff / pixelCount;
                _output.WriteLine($"Sampled {pixelCount} pixels across {desc.Width}x{desc.Height}: MaxDiff={maxDiff}, AvgDiff={avgDiff:F4}");
                Assert.True(maxDiff <= 1, $"GPU vs CPU tone mapping mismatch: MaxDiff was {maxDiff}");
            }
            finally
            {
                Marshal.ReleaseComObject(texture);
            }
        }
        finally
        {
            if (context != null) Marshal.ReleaseComObject(context);
            if (d3d11Device != null) Marshal.ReleaseComObject(d3d11Device);
        }
    }

    [Fact]
    public void TestCaptureSessionBorderRequiredInterop()
    {
        IntPtr primaryMonitor = HdrDisplayInfo.GetMonitorForWindow(IntPtr.Zero);
        WindowsGraphicsCaptureInterop.CreateD3D11Device(out var d3d11Device, out var context);
        try
        {
            var captureItem = WindowsGraphicsCaptureInterop.CreateCaptureItemForMonitor(primaryMonitor);
            Assert.NotNull(captureItem);

            var d3dInterOpDevice = WindowsGraphicsCaptureInterop.CreateID3DDeviceFromD3D11Device(d3d11Device);
            using var framePool = Windows.Graphics.Capture.Direct3D11CaptureFramePool.CreateFreeThreaded(
                d3dInterOpDevice,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                captureItem.Size);
            using var session = framePool.CreateCaptureSession(captureItem);
            Assert.NotNull(session);

            bool isWin11 = Environment.OSVersion.Version.Build >= 22000;
            _output.WriteLine($"OS Build: {Environment.OSVersion.Version.Build}, isWin11: {isWin11}");

            if ((object)session is IGraphicsCaptureSession3 session3)
            {
                _output.WriteLine("session implements IGraphicsCaptureSession3 successfully.");
                bool initial = session3.IsBorderRequired;
                _output.WriteLine($"Initial IsBorderRequired: {initial}");
                session3.IsBorderRequired = false;
                Assert.False(session3.IsBorderRequired);
                _output.WriteLine("Successfully set IsBorderRequired to false!");
            }
            else
            {
                _output.WriteLine("session does NOT implement IGraphicsCaptureSession3 (expected on Windows 10).");
                Assert.False(isWin11, "Expected Windows 11 build to implement IGraphicsCaptureSession3");
            }
        }
        finally
        {
            if (context != null) Marshal.ReleaseComObject(context);
            if (d3d11Device != null) Marshal.ReleaseComObject(d3d11Device);
        }
    }

    [Fact]
    public async Task TestCaptureRectangle_SingleMonitor_ReturnsValidBitmap()
    {
        var primaryDisplay = DisplayInfo.AllDisplayInfos.FirstOrDefault(d => d.IsPrimary) ?? DisplayInfo.AllDisplayInfos.First();
        Assert.NotNull(primaryDisplay);

        using var bitmap = await WindowsGraphicsCaptureInterop.CaptureRectangleAsync(primaryDisplay.Bounds);
        Assert.NotNull(bitmap);
        Assert.Equal(primaryDisplay.Bounds.Width, bitmap.Width);
        Assert.Equal(primaryDisplay.Bounds.Height, bitmap.Height);
    }

    [Fact]
    public async Task TestRepeatedCapture_ReusesCachedDevice()
    {
        IntPtr primaryMonitor = HdrDisplayInfo.GetMonitorForWindow(IntPtr.Zero);
        using var first = await WindowsGraphicsCaptureInterop.CaptureMonitorToBitmapAsync(primaryMonitor);
        Assert.NotNull(first);

        using var second = await WindowsGraphicsCaptureInterop.CaptureMonitorToBitmapAsync(primaryMonitor);
        Assert.NotNull(second);
        Assert.Equal(first.Width, second.Width);
        Assert.Equal(first.Height, second.Height);
    }

    [Fact]
    public unsafe void TestUnpremultiplyRow()
    {
        // B, G, R, A: opaque, transparent, half transparent, a quarter transparent
        byte[] pixels =
        {
            10, 20, 30, 255,
            0, 0, 0, 0,
            50, 64, 100, 128,
            16, 32, 64, 64
        };
        fixed (byte* row = pixels)
        {
            WindowsGraphicsCaptureInterop.UnpremultiplyRow(row, 4);
        }

        Assert.Equal(new byte[] { 10, 20, 30, 255 }, pixels.Take(4).ToArray());
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, pixels.Skip(4).Take(4).ToArray());
        // 50 * 255 / 128 = 99.6, 64 * 255 / 128 = 127.5, 100 * 255 / 128 = 199.2
        Assert.Equal(new byte[] { 100, 128, 199, 128 }, pixels.Skip(8).Take(4).ToArray());
        // 64 * 255 / 64 = 255: stays in range
        Assert.Equal(new byte[] { 64, 128, 255, 64 }, pixels.Skip(12).Take(4).ToArray());
    }

    [Fact]
    public void TestWarpDeviceCanBeCreated()
    {
        // The fallback when there is no usable GPU (virtual machine, RDP): WARP must deliver a device WGC can use
        int hr = WindowsGraphicsCaptureInterop.CreateD3D11Device(WindowsGraphicsCaptureInterop.WarpDriverType, out var device, out var context);
        Assert.Equal(0, hr);
        try
        {
            var winrtDevice = WindowsGraphicsCaptureInterop.CreateID3DDeviceFromD3D11Device(device);
            Assert.NotNull(winrtDevice);
            (winrtDevice as IDisposable)?.Dispose();
        }
        finally
        {
            Marshal.ReleaseComObject(context);
            Marshal.ReleaseComObject(device);
        }
    }

    [Fact]
    public async Task TestCaptureChildWindow_CapturesTopLevelAndCrops()
    {
        if (!WindowsGraphicsCaptureInterop.IsSupported)
        {
            _output.WriteLine("Windows Graphics Capture isn't supported here.");
            return;
        }

        Panel child = null;
        using var host = await TestFormHost.ShowAsync(() =>
        {
            var form = new Form
            {
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                Bounds = new Rectangle(100, 100, 300, 200),
                BackColor = Color.Red,
                TopMost = true
            };
            child = new Panel
            {
                Bounds = new Rectangle(40, 30, 120, 80),
                BackColor = Color.Blue
            };
            form.Controls.Add(child);
            return form;
        });

        IntPtr childHandle = IntPtr.Zero;
        await host.InvokeAsync(_ => childHandle = child.Handle);

        using var bitmap = await WindowsGraphicsCaptureInterop.CaptureWindowToBitmapAsync(childHandle);
        Assert.NotNull(bitmap);
        Assert.Equal(120, bitmap.Width);
        Assert.Equal(80, bitmap.Height);
        var center = bitmap.GetPixel(60, 40);
        _output.WriteLine($"Center pixel of the child: {center}");
        Assert.True(center.B > 200 && center.R < 50, $"Expected the blue child, got {center}");
    }

    [Fact]
    public async Task TestCaptureMinimizedWindow_RestoresIt()
    {
        if (!WindowsGraphicsCaptureInterop.IsSupported)
        {
            _output.WriteLine("Windows Graphics Capture isn't supported here.");
            return;
        }

        using var host = await TestFormHost.ShowAsync(() => new Form
        {
            StartPosition = FormStartPosition.Manual,
            FormBorderStyle = FormBorderStyle.None,
            Bounds = new Rectangle(120, 120, 240, 160),
            BackColor = Color.Green,
            Text = "Greenshot minimized capture test"
        }, form => form.WindowState = FormWindowState.Minimized);

        IntPtr handle = IntPtr.Zero;
        await host.InvokeAsync(form => handle = form.Handle);
        Assert.True(User32Api.IsIconic(handle));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var bitmap = await WindowsGraphicsCaptureInterop.CaptureWindowToBitmapAsync(handle);
        _output.WriteLine($"Capture of the minimized window took {stopwatch.ElapsedMilliseconds} ms");
        Assert.NotNull(bitmap);
        Assert.False(User32Api.IsIconic(handle));
        Assert.Equal(240, bitmap.Width);
        Assert.Equal(160, bitmap.Height);
    }

    [DllImport("d3d11.dll")]
    private static extern int D3D11CreateDevice(
        IntPtr pAdapter,
        int driverType,
        IntPtr Software,
        uint flags,
        int[] pFeatureLevels,
        uint FeatureLevels,
        uint SDKVersion,
        out ID3D11Device ppDevice,
        out int pFeatureLevel,
        out ID3D11DeviceContext ppImmediateContext);
}
