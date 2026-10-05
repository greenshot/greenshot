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


using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class MemoryProfilesTests
    {
        public MemoryProfilesTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Defaults_AreTheFastProfile()
        {
            // Greenshot stays as fast as it is unless the user turns something down
            Assert.Equal(MemoryProfile.Fast, MemoryProfiles.Detect(new CoreConfigurationImpl()));
        }

        [Theory]
        [InlineData(MemoryProfile.Fast)]
        [InlineData(MemoryProfile.Balanced)]
        [InlineData(MemoryProfile.LowMemory)]
        public void Apply_ThenDetect_GivesTheSameProfile(MemoryProfile profile)
        {
            var configuration = new CoreConfigurationImpl();
            MemoryProfiles.Apply(configuration, profile);
            Assert.Equal(profile, MemoryProfiles.Detect(configuration));
        }

        [Fact]
        public void Balanced_KeepsHardwareRendering()
        {
            var configuration = new CoreConfigurationImpl();
            MemoryProfiles.Apply(configuration, MemoryProfile.Balanced);

            Assert.True(configuration.HardwareRendering);
            Assert.True(configuration.PrewarmCapture);
            Assert.False(configuration.KeepGraphicsCaptureReady);
            Assert.False(configuration.PrewarmEditor);
            Assert.Equal(MemoryProfiles.BalancedBufferPoolLimit, configuration.BufferPoolLimit);
        }

        [Fact]
        public void LowMemory_TurnsEverythingDown()
        {
            var configuration = new CoreConfigurationImpl();
            MemoryProfiles.Apply(configuration, MemoryProfile.LowMemory);

            Assert.False(configuration.HardwareRendering);
            Assert.False(configuration.KeepGraphicsCaptureReady);
            Assert.False(configuration.PrewarmCapture);
            Assert.False(configuration.PrewarmEditor);
            Assert.True(configuration.MinimizeWorkingSetSize);
            Assert.Equal(MemoryProfiles.LowMemoryBufferPoolLimit, configuration.BufferPoolLimit);
        }

        [Fact]
        public void Profiles_LeaveTheCaptureMethodAlone()
        {
            // The GDI capture changes what a capture contains, so no profile switches it
            var configuration = new CoreConfigurationImpl { UseGraphicsCapture = false };
            MemoryProfiles.Apply(configuration, MemoryProfile.LowMemory);
            Assert.False(configuration.UseGraphicsCapture);
            Assert.Equal(MemoryProfile.LowMemory, MemoryProfiles.Detect(configuration));
        }

        [Fact]
        public void OneChangedSetting_IsCustom()
        {
            var configuration = new CoreConfigurationImpl { PrewarmEditor = false };
            Assert.Equal(MemoryProfile.Custom, MemoryProfiles.Detect(configuration));
        }

        [Fact]
        public void ApplyCustom_ChangesNothing()
        {
            var configuration = new CoreConfigurationImpl { PrewarmEditor = false };
            MemoryProfiles.Apply(configuration, MemoryProfile.Custom);
            Assert.False(configuration.PrewarmEditor);
            Assert.True(configuration.PrewarmCapture);
        }
    }
}
