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
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Capturing;
using Greenshot.Base.Interfaces;
using Xunit;
using System.Threading.Tasks;
using Greenshot.Base.Threading;

namespace Greenshot.Tests.Core;

public class CursorCaptureTests
{
    public CursorCaptureTests()
    {
        TestEnvironment.EnsureInitialized();
    }

    [Fact]
    public void TestCaptureCursor_WithWindowLocation_AlignsToWindowOrigin()
    {
        // Simulate a window capture at (400, 200) with a 800x600 image
        using var testImage = new Bitmap(800, 600);
        ICapture capture = new Capture
        {
            Image = testImage,
            Location = new NativePoint(400, 200),
            ScreenBounds = new NativeRect(0, 0, 1920, 1080)
        };

        // Capture the cursor
        NativePoint cursorBefore = User32Api.GetCursorLocation();
        capture = WindowCapture.CaptureCursor(capture);
        NativePoint cursorLocation = User32Api.GetCursorLocation();

        // Only comparable when the mouse did not move during the capture
        if (capture.Cursor != null && cursorBefore.Equals(cursorLocation))
        {
            int expectedX = cursorLocation.X - capture.Cursor.HotSpot.X - 400;
            int expectedY = cursorLocation.Y - capture.Cursor.HotSpot.Y - 200;

            Assert.Equal(expectedX, capture.CursorLocation.X);
            Assert.Equal(expectedY, capture.CursorLocation.Y);
        }
    }

    [Fact]
    public void TestCaptureCursor_WithoutImage_AlignsToScreenBounds()
    {
        // Simulate capture before image is acquired
        ICapture capture = new Capture
        {
            Image = null,
            Location = new NativePoint(0, 0),
            ScreenBounds = new NativeRect(100, 50, 1920, 1080)
        };

        capture = WindowCapture.CaptureCursor(capture);

        if (capture.Cursor != null)
        {
            NativePoint cursorLocation = User32Api.GetCursorLocation();
            int expectedX = cursorLocation.X - capture.Cursor.HotSpot.X - 100;
            int expectedY = cursorLocation.Y - capture.Cursor.HotSpot.Y - 50;

            Assert.Equal(expectedX, capture.CursorLocation.X);
            Assert.Equal(expectedY, capture.CursorLocation.Y);
        }
    }

    [Fact]
    public void TestCaptureWindow_SetsLocation()
    {
        // Uses a window of its own instead of whatever window happens to be active on the desktop
        Exception failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                RunWindowCaptureTestAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static async Task RunWindowCaptureTestAsync()
    {
        using var host = await TestFormHost.ShowAsync(() => new Form
        {
            StartPosition = FormStartPosition.Manual,
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            Bounds = new Rectangle(123, 77, 320, 240),
            Text = "Greenshot capture handler test"
        });
        IntPtr handle = IntPtr.Zero;
        await host.InvokeAsync(form => handle = form.Handle);

        // Windows Graphics Capture, or the GDI fallback where it isn't supported
        var windowDetails = new WindowDetails(handle);
        var capture = await WindowCapture.CaptureWindowAsync(windowDetails);

        Assert.NotNull(capture);
        Assert.NotNull(capture.Image);
        Assert.Equal(320, capture.Image.Width);
        Assert.Equal(240, capture.Image.Height);
        Assert.Equal(windowDetails.Location, capture.Location);
        Assert.Equal("Greenshot capture handler test", capture.CaptureDetails.Title);
        Assert.True(capture.CaptureDetails.MetaData.ContainsKey(ScreenCapture.CaptureMethodKey));
    }
}
