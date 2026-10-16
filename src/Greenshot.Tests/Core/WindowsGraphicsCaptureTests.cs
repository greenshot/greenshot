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
    public async Task TestCaptureHdrMonitor()
    {
        IntPtr primaryMonitor = HdrDisplayInfo.GetMonitorForWindow(IntPtr.Zero);
        _output.WriteLine($"Capturing monitor {primaryMonitor}...");

        using var bitmap = await WindowsGraphicsCaptureInterop.CaptureMonitorToBitmapAsync(primaryMonitor);
        _output.WriteLine($"Capture result: {(bitmap != null ? $"{bitmap.Width}x{bitmap.Height}" : "null")}");
        Assert.NotNull(bitmap);
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
}
