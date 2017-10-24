using System.Diagnostics;
using BenchmarkDotNet.Running;
using Xunit;

namespace Greenshot.Testss
{
    public class GfxPerformanceTests
    {
        [Fact]
        public void TestBlur()
        {
            var summary = BenchmarkRunner.Run<GfxPerformance>();
            Assert.False(summary.HasCriticalValidationErrors);
            Debug.WriteLine(summary.ToString());
        }
    }
}
