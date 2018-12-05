using System;
using BenchmarkDotNet.Attributes;
using Greenshot.Addons.Core;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class CapturePerformance
    {
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
    }
}
