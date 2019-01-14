using System;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.User32;
using Greenshot.Addons.Core;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class CapturePerformance
    {
        private readonly ScreenCapture _screenCapture = new ScreenCapture(null, DisplayInfo.ScreenBounds);

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

        
        [Benchmark]
        public void CaptureBuffered()
        {
            _screenCapture.CaptureFrame();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _screenCapture.Dispose();
        }
    }
}
