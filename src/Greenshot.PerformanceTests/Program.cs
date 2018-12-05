using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This initializes the benchmark tests
    /// </summary>
    public static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            var job = Job.Default.With(Platform.X64);
            var config = DefaultConfig.Instance.With(job).KeepBenchmarkFiles(true);
            BenchmarkRunner.Run<GfxPerformanceShort>(config);
            Console.ReadLine();
        }
    }
}
