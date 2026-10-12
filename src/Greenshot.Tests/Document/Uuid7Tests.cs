/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Linq;
using Greenshot.Document;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class Uuid7Tests
    {
        [Fact]
        public void CreateVersion7_SetsTheVersionAndVariant()
        {
            string id = Uuid7.CreateVersion7().ToString("D");

            Assert.Equal('7', id[14]);
            Assert.Contains(id[19], "89ab");
        }

        [Fact]
        public void CreateVersion7_PutsTheTimeFirst()
        {
            var timestamp = new DateTimeOffset(2026, 10, 12, 5, 54, 25, 123, TimeSpan.Zero);

            string id = Uuid7.CreateVersion7(timestamp).ToString("N");

            Assert.StartsWith(timestamp.ToUnixTimeMilliseconds().ToString("x12"), id);
        }

        [Fact]
        public void CreateVersion7_SortsByTime()
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var ids = Enumerable.Range(0, 100).Select(i => Uuid7.CreateVersion7(start.AddMilliseconds(i * 997L)).ToString()).ToList();

            Assert.Equal(ids.OrderBy(id => id, StringComparer.Ordinal), ids);
        }

        [Fact]
        public void TryGetTimestamp_ReturnsTheCreationTime()
        {
            var timestamp = new DateTimeOffset(2026, 10, 12, 5, 54, 25, 123, TimeSpan.Zero);

            Assert.True(Uuid7.TryGetTimestamp(Uuid7.CreateVersion7(timestamp), out var read));
            Assert.Equal(timestamp, read);
        }

        [Fact]
        public void TryGetTimestamp_IsFalseForOtherIds()
        {
            Assert.False(Uuid7.TryGetTimestamp(Guid.NewGuid(), out _));
            Assert.False(Uuid7.TryGetTimestamp(Guid.Empty, out _));
        }

        [Fact]
        public void CreateVersion7_RefusesATimeBefore1970()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Uuid7.CreateVersion7(new DateTimeOffset(1969, 12, 31, 0, 0, 0, TimeSpan.Zero)));
        }
    }
}
