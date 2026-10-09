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

using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Applies and recognizes the <see cref="MemoryProfile"/> values. UseGraphicsCapture is no part of a profile:
    /// it changes what a capture contains (HDR, covered windows), not only memory and speed.
    /// </summary>
    public static class MemoryProfiles
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryProfiles));

        /// <summary>
        /// The MB the buffer pools keep with the Balanced profile
        /// </summary>
        public const int BalancedBufferPoolLimit = 16;

        /// <summary>
        /// The MB the buffer pools keep with the LowMemory profile
        /// </summary>
        public const int LowMemoryBufferPoolLimit = 4;

        private sealed class Values
        {
            public bool HardwareRendering;
            public bool KeepGraphicsCaptureReady;
            public bool PrewarmCapture;
            public bool PrewarmEditor;
            public int BufferPoolLimit;
            public bool MinimizeWorkingSetSize;
        }

        private static Values ValuesOf(MemoryProfile profile) => profile switch
        {
            MemoryProfile.Fast => new Values
            {
                HardwareRendering = true, KeepGraphicsCaptureReady = true, PrewarmCapture = true, PrewarmEditor = true,
                BufferPoolLimit = 0, MinimizeWorkingSetSize = false
            },
            // Keeps the smooth capture window (hardware rendering) and its quick start, drops what only the first editor needs
            MemoryProfile.Balanced => new Values
            {
                HardwareRendering = true, KeepGraphicsCaptureReady = false, PrewarmCapture = true, PrewarmEditor = false,
                BufferPoolLimit = BalancedBufferPoolLimit, MinimizeWorkingSetSize = true
            },
            MemoryProfile.LowMemory => new Values
            {
                HardwareRendering = false, KeepGraphicsCaptureReady = false, PrewarmCapture = false, PrewarmEditor = false,
                BufferPoolLimit = LowMemoryBufferPoolLimit, MinimizeWorkingSetSize = true
            },
            _ => null
        };

        /// <summary>
        /// Set the memory settings to the values of the profile, Custom changes nothing
        /// </summary>
        public static void Apply(ICoreConfiguration configuration, MemoryProfile profile)
        {
            var values = ValuesOf(profile);
            if (configuration == null || values == null)
            {
                return;
            }

            configuration.HardwareRendering = values.HardwareRendering;
            configuration.KeepGraphicsCaptureReady = values.KeepGraphicsCaptureReady;
            configuration.PrewarmCapture = values.PrewarmCapture;
            configuration.PrewarmEditor = values.PrewarmEditor;
            configuration.BufferPoolLimit = values.BufferPoolLimit;
            configuration.MinimizeWorkingSetSize = values.MinimizeWorkingSetSize;
            Log.InfoFormat("Memory profile {0} applied", profile);
        }

        /// <summary>
        /// The profile the memory settings match, Custom when they match none
        /// </summary>
        public static MemoryProfile Detect(ICoreConfiguration configuration)
        {
            if (configuration == null)
            {
                return MemoryProfile.Fast;
            }

            foreach (var profile in new[] { MemoryProfile.Fast, MemoryProfile.Balanced, MemoryProfile.LowMemory })
            {
                var values = ValuesOf(profile);
                if (configuration.HardwareRendering == values.HardwareRendering
                    && configuration.KeepGraphicsCaptureReady == values.KeepGraphicsCaptureReady
                    && configuration.PrewarmCapture == values.PrewarmCapture
                    && configuration.PrewarmEditor == values.PrewarmEditor
                    && configuration.BufferPoolLimit == values.BufferPoolLimit
                    && configuration.MinimizeWorkingSetSize == values.MinimizeWorkingSetSize)
                {
                    return profile;
                }
            }

            return MemoryProfile.Custom;
        }
    }
}
