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
using System.Runtime.CompilerServices;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The idle collection is repeated while a closed window is still reachable, a few times at most
    /// </summary>
    [Collection(nameof(PooledMemoryTests))]
    public class PooledMemoryTests
    {
        [Fact]
        public void ShouldCollectAgain_WhileTheClosedObjectIsReachable_AtMostThreeTimes()
        {
            var closed = new object();
            PooledMemory.TrackClosed(closed);

            Assert.True(PooledMemory.ShouldCollectAgain());
            Assert.True(PooledMemory.ShouldCollectAgain());
            Assert.True(PooledMemory.ShouldCollectAgain());
            Assert.False(PooledMemory.ShouldCollectAgain());
            // Given up: forgotten, no more retries for it
            Assert.False(PooledMemory.ShouldCollectAgain());
            GC.KeepAlive(closed);
        }

        [Fact]
        public void ShouldCollectAgain_OnceTheClosedObjectIsCollected_IsFalse()
        {
            TrackObjectWhichIsGarbage();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(PooledMemory.ShouldCollectAgain());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TrackObjectWhichIsGarbage() => PooledMemory.TrackClosed(new object());
    }

    /// <summary>
    /// The list of closed objects is static, windows of other tests (Settings, About) add to it when they close
    /// </summary>
    [CollectionDefinition(nameof(PooledMemoryTests), DisableParallelization = true)]
    public sealed class PooledMemoryTestsCollection
    {
    }
}
