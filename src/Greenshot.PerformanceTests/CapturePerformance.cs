#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Addons.Core;
using Greenshot.PerformanceTests.Capture;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class CapturePerformance
    {
        // A ScreenCapture which captures the whole screen (multi-monitor)
        private readonly ScreenCapture _screenCapture = new ScreenCapture(DisplayInfo.ScreenBounds);
        // A ScreenCapture which captures the whole screen (multi-monitor) but with half the destination size, uses stretch-blt
        private readonly ScreenCapture _screenCaptureResized = new ScreenCapture(DisplayInfo.ScreenBounds, new NativeSize(DisplayInfo.ScreenBounds.Width / 2, DisplayInfo.ScreenBounds.Height / 2));

        /// <summary>
        /// This benchmarks a screen capture which does a lot of additional work
        /// </summary>
        [Benchmark]
        public void Capture()
        {
            using (var capture = WindowCapture.CaptureScreen())
            {
                if (capture.Bitmap == null)
                {
                    throw new NotSupportedException();
                }
                if (capture.Bitmap.Width <= 0 || capture.Bitmap.Height <= 0)
                {
                    throw new NotSupportedException();
                }
            }
        }
        
        /// <summary>
        /// Capture the screen with buffered settings
        /// </summary>
        [Benchmark]
        public void CaptureBuffered()
        {
            _screenCapture.CaptureFrame();
        }

        /// <summary>
        /// Capture the screen with buffered settings, but resized (smaller) destination
        /// </summary>
        [Benchmark]
        public void CaptureBufferedResized()
        {
            _screenCaptureResized.CaptureFrame();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _screenCapture.Dispose();
        }
    }
}
