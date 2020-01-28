// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
using BenchmarkDotNet.Attributes;
using Greenshot.Gfx;

namespace Greenshot.PerformanceTests
{
    /// <summary>
    /// This defines the benchmarks which can be done
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class Murmur3Performance
    {
        private static readonly string TestString = "The quick brown fox jumps over the lazy dog";

        [Benchmark]
        public void MurmurPerformanceTest()
        {
            var hashAlgorithm = new Murmur3(0x9747b28c);
            hashAlgorithm.CalculateHash(TestString.AsSpan());
        }
    }
}
