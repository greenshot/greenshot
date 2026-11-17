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

using Greenshot.Base.Core;
using Greenshot.Configuration;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class EditionDefaultsTests
    {
        public EditionDefaultsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Theory]
        [InlineData(MemoryProfile.Fast)]
        [InlineData(MemoryProfile.Balanced)]
        [InlineData(MemoryProfile.LowMemory)]
        public void IniValues_OfAProfile_AreReadBackAsThatProfile(MemoryProfile profile)
        {
            // The values the edition defaults give Dapplo.Ini must parse to the profile, e.g. Greenshot Light's LowMemory
            var configuration = new CoreConfigurationImpl();
            configuration.ResetToDefaults();
            foreach (var value in MemoryProfiles.IniValuesOf(profile))
            {
                configuration.SetRawValue(value.Key, value.Value);
            }

            Assert.Equal(profile, MemoryProfiles.Detect(configuration));
        }

        [Fact]
        public void TryGetValue_GivesTheProfileValuesOfTheCoreSection()
        {
            var defaults = new EditionDefaults(MemoryProfile.Balanced);

            Assert.True(defaults.TryGetValue("Core", nameof(ICoreConfiguration.PrewarmEditor), out string prewarmEditor));
            Assert.Equal(bool.FalseString, prewarmEditor);
            // Dapplo.Ini compares keys and sections ignoring case (CaseSensitiveKeys = false)
            Assert.True(defaults.TryGetValue("core", "keepgraphicscaptureready", out string keepReady));
            Assert.Equal(bool.FalseString, keepReady);
            Assert.True(defaults.TryGetValue("Core", nameof(ICoreConfiguration.BufferPoolLimit), out string poolLimit));
            Assert.Equal(MemoryProfiles.BalancedBufferPoolLimit.ToString(System.Globalization.CultureInfo.InvariantCulture), poolLimit);
        }

        [Fact]
        public void TryGetValue_LeavesOtherKeysAndSectionsAlone()
        {
            var defaults = new EditionDefaults(MemoryProfile.Balanced);

            Assert.False(defaults.TryGetValue("Core", nameof(ICoreConfiguration.UseGraphicsCapture), out _));
            Assert.False(defaults.TryGetValue("Editor", nameof(ICoreConfiguration.PrewarmEditor), out _));
            Assert.False(defaults.TryGetValue("Core", null, out _));
        }
    }
}
