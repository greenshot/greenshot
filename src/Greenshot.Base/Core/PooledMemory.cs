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
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Kernel32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The memory Greenshot keeps for reuse outside the managed heap: ImageSharp's unmanaged buffer pool and the working set.
    /// The pools of the <see cref="RecyclableMemoryStreamFactory"/> follow the same BufferPoolLimit setting.
    /// </summary>
    public static class PooledMemory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PooledMemory));

        /// <summary>
        /// How long Greenshot has to stay idle (no capture ending, no editor closing) before the memory is collected
        /// </summary>
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);

        private static readonly Timer IdleTimer = new Timer(_ => CollectNow(), null, Timeout.Infinite, Timeout.Infinite);

        /// <summary>
        /// Limits ImageSharp's pool to the BufferPoolLimit setting. Without a limit ImageSharp keeps up to 128 MB on .NET Framework
        /// (trimmed by half per idle minute). Call it once at the start, before the first image is made.
        /// </summary>
        public static void ApplyPoolLimit()
        {
            int limitInMb = IniConfigRegistry.GetSection<ICoreConfiguration>()?.BufferPoolLimit ?? 0;
            if (limitInMb <= 0)
            {
                return;
            }

            Configuration.Default.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
            {
                MaximumPoolSizeMegabytes = limitInMb
            });
        }

        /// <summary>
        /// Frees ImageSharp's pooled buffers which aren't in use and trims the working set, for the MinimizeWorkingSetSize setting.
        /// Buffers in use stay valid, the pool allocates new ones when it needs them again.
        /// </summary>
        public static void Trim()
        {
            Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
            PsApi.EmptyWorkingSet();
        }

        /// <summary>
        /// A capture ended or a window closed: a few seconds later, when nothing else ended in the meantime, the memory of the
        /// closed captures is collected. Without this nothing collects it while Greenshot is idle: the .NET GC only runs when
        /// something allocates, so the garbage (and the free space of the large object heap) stays committed until the next capture.
        /// With MinimizeWorkingSetSize the pools and the working set are trimmed afterwards too.
        /// </summary>
        public static void ReleaseWhenIdle() => IdleTimer.Change(IdleDelay, Timeout.InfiniteTimeSpan);

        private static void CollectNow()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                long before = GC.GetTotalMemory(false);
                // The large object heap is never compacted by itself: its free gaps stay committed
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                // Bitmaps which nobody disposed free their GDI+ memory in their finalizer, the next collection takes the objects
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                Log.InfoFormat("Collected the memory while idle in {0} ms, managed heap {1:0.0} MB -> {2:0.0} MB",
                    stopwatch.ElapsedMilliseconds, before / 1048576.0, GC.GetTotalMemory(false) / 1048576.0);

                if (IniConfigRegistry.GetSection<ICoreConfiguration>()?.MinimizeWorkingSetSize == true)
                {
                    Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Collecting the memory while idle failed", ex);
            }
        }
    }
}
