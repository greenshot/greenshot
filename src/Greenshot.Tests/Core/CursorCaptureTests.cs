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

using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Xunit;

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
        capture = WindowCapture.CaptureCursor(capture);

        if (capture.Cursor != null)
        {
            NativePoint cursorLocation = User32Api.GetCursorLocation();
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
    public void TestCustomWindowCaptureHandler_SetsLocation()
    {
        using var dummyBitmap = new Bitmap(500, 400);
        WindowCaptureHelper.CustomWindowCaptureHandler = hwnd => dummyBitmap;

        try
        {
            var windowDetails = WindowDetails.GetActiveWindow();
            if (windowDetails != null)
            {
                var capture = WindowCaptureHelper.CaptureWindow(windowDetails, null, WindowCaptureMode.Auto);
                Assert.NotNull(capture);
                Assert.NotNull(capture.Image);
                Assert.Equal(windowDetails.Location, capture.Location);
            }
        }
        finally
        {
            WindowCaptureHelper.CustomWindowCaptureHandler = null;
        }
    }
}
