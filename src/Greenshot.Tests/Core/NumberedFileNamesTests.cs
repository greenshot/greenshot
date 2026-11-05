/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The numbered names for a file name which is taken
    /// </summary>
    public sealed class NumberedFileNamesTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"greenshot_numbered_test_{Guid.NewGuid():N}");

        public NumberedFileNamesTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Theory]
        [InlineData("capture.png", "capture (2).png")]
        [InlineData("capture (2).png", "capture (3).png")]
        [InlineData("capture (x).png", "capture (x) (2).png")]
        [InlineData("capture", "capture (2)")]
        public void Next_Numbers(string filename, string expected)
        {
            Assert.Equal(expected, NumberedFileNames.Next(filename));
        }

        [Fact]
        public void FirstFree_NoFile_IsThePath()
        {
            string path = Path.Combine(_directory, "capture.png");
            Assert.Equal(path, NumberedFileNames.FirstFree(path));
        }

        [Fact]
        public void FirstFree_Taken_IsTheFirstFreeNumber()
        {
            string path = Path.Combine(_directory, "capture.png");
            File.WriteAllBytes(path, new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(_directory, "capture (2).png"), new byte[] { 1 });

            Assert.Equal(Path.Combine(_directory, "capture (3).png"), NumberedFileNames.FirstFree(path));
        }
    }
}
