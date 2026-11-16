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
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// CaptureWait and CaptureWaitMilliseconds replace CaptureDelay: a value other than its old default of 100 ms is moved once
    /// </summary>
    public class CoreConfigurationCaptureWaitTests
    {
        public CoreConfigurationCaptureWaitTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Defaults_NoWait()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            config.OnAfterLoad();

            Assert.False(config.CaptureWait);
            Assert.Equal(3000, config.CaptureWaitMilliseconds);
            Assert.True(config.CaptureWaitCountdown);
        }

        [Theory]
        [InlineData("100")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData("not a number")]
        public void OldDefaultOrNoDelay_StaysWithoutWait(string captureDelay)
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.SetRawValue("CaptureDelay", captureDelay);

            config.OnAfterLoad();

            Assert.False(config.CaptureWait);
            Assert.Equal(3000, config.CaptureWaitMilliseconds);
        }

        [Theory]
        [InlineData("5000", 5000)]
        [InlineData("250", 250)]
        public void ChosenDelay_IsMovedOnce(string captureDelay, int expectedMilliseconds)
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.SetRawValue("CaptureDelay", captureDelay);

            config.OnAfterLoad();

            Assert.True(config.CaptureWait);
            Assert.Equal(expectedMilliseconds, config.CaptureWaitMilliseconds);
            // The countdown keeps its default: a short wait doesn't show it anyway, a longer one chosen later should
            Assert.True(config.CaptureWaitCountdown);

            // Moved only once: switching it off stays off
            config.CaptureWait = false;
            config.OnAfterLoad();
            Assert.False(config.CaptureWait);
        }
    }
}
