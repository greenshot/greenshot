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
using System.Text;
using Greenshot.Gfx;
using Greenshot.Tests.Extensions;
using Xunit;

namespace Greenshot.Tests
{
    /// <summary>
    ///  Some simple tests to prove Murmur3 was correctly implemented
    /// </summary>
    public class Murmur3Tests
    {
        private static readonly uint Seed = 0x9747b28c;
        private static readonly string TestString = "The quick brown fox jumps over the lazy dog";
        
        [Fact]
        public void Murmur3_Test()
        {
            var murmur3Span = new Murmur3(Seed);
            var testBytes = Encoding.UTF8.GetBytes(TestString);
            var hash = murmur3Span.CalculateHash(testBytes.AsSpan());
            Assert.Equal(0x2FA826CDu, hash);
        }
    }
}
