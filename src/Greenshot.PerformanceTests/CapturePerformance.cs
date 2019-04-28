// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Drawing.Imaging;
using System.IO;
using BenchmarkDotNet.Attributes;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Addons.Core;
using SharpAvi;
using SharpAvi.Output;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class CapturePerformance
    {
        private static readonly LogSource Log = new LogSource();
        // A ScreenCapture which captures the whole screen (multi-monitor)
        private GdiScreenCapture _screenCapture;
        // A ScreenCapture which captures the whole screen (multi-monitor) but with half the destination size, uses stretch-blt
        private GdiScreenCapture _screenCaptureResized;
        private BitmapScreenCapture _screenBitmapCapture;
        private BitmapScreenCapture _screenBitmapCaptureResized;
        private AviWriter _aviWriter;
        private IAviVideoStream _aviVideoStream;

        [GlobalSetup]
        public void Setup()
        {
            _screenCapture = new GdiScreenCapture(DisplayInfo.ScreenBounds);
            _screenBitmapCapture = new BitmapScreenCapture();
            var resizedSize = new NativeSize(DisplayInfo.ScreenBounds.Width / 2, DisplayInfo.ScreenBounds.Height / 2);
            _screenCaptureResized = new GdiScreenCapture(DisplayInfo.ScreenBounds, resizedSize);
            _screenBitmapCaptureResized = new BitmapScreenCapture(DisplayInfo.ScreenBounds, resizedSize);

            var aviFile = Path.Combine(Path.GetTempPath(), @"test.avi");
            Log.Info().WriteLine("Writing AVI to {0}", aviFile);
            _aviWriter = new AviWriter(aviFile)
            {
                FramesPerSecond = 30,
                // Emitting AVI v1 index in addition to OpenDML index (AVI v2)
                // improves compatibility with some software, including 
                // standard Windows programs like Media Player and File Explorer
                EmitIndex1 = true
            };
            _aviVideoStream = _aviWriter.AddVideoStream(resizedSize.Width, resizedSize.Height, BitsPerPixel.Bpp24);
           
        }

        /// <summary>
        /// This benchmarks a screen capture which does a lot of additional work
        /// </summary>
        //[Benchmark]
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
        /// Capture the screen directly into a bitmap
        /// </summary>
        [Benchmark]
        public void CaptureBitmap()
        {
            _screenBitmapCapture.CaptureFrame();
        }

        /// <summary>
        /// Capture the screen with buffered settings, but resized (smaller) destination
        /// </summary>
        //[Benchmark]
        public void CaptureBufferedResized()
        {
            _screenCaptureResized.CaptureFrame();
        }
        
        
        /// <summary>
        /// Capture the screen with buffered settings, but resized (smaller) destination
        /// </summary>
        //[Benchmark]
        public void CapturebitmapResized()
        {
            _screenBitmapCaptureResized.CaptureFrame();
        }


        /// <summary>
        /// Capture the screen with buffered settings, but resized (smaller) destination
        /// </summary>
        //[Benchmark]
        public void CaptureAvi()
        {
            _screenCaptureResized.CaptureFrame();
            // TODO: Write frame...
            //_aviVideoStream.WriteFrame(false, new []{}, 0, 0);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _screenCapture.Dispose();
            _screenBitmapCapture.Dispose();
            _screenBitmapCaptureResized.Dispose();
            _aviWriter.Close();
        }
    }
}
