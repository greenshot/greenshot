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

using Dapplo.Ini;
using Dapplo.Windows.Kernel32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The memory Greenshot keeps for reuse outside the managed heap: ImageSharp's unmanaged buffer pool and the working set.
    /// The pools of the <see cref="RecyclableMemoryStreamFactory"/> follow the same BufferPoolLimit setting.
    /// </summary>
    public static class PooledMemory
    {
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
    }
}
