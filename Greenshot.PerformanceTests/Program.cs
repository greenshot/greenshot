using System;
using BenchmarkDotNet.Running;

namespace Greenshot.PerformanceTests
{
    public static class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<GfxPerformance>();
            Console.ReadLine();
        }
    }
}
