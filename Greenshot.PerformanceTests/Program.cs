using System;
using BenchmarkDotNet.Running;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This initializes the benchmal tests
    /// </summary>
    public static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<GfxPerformance>();
            Console.ReadLine();
        }
    }
}
