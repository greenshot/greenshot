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
using Greenshot.Base.Core.Enums;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// OutputFileIfExists replaces OutputFileAllowOverwrite: False (the save dialog) is changed once to Ask
    /// </summary>
    public class CoreConfigurationIfFileExistsTests
    {
        public CoreConfigurationIfFileExistsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Defaults_Overwrite()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            config.OnAfterLoad();

            Assert.Equal(FileExistsAction.Overwrite, config.OutputFileIfExists);
            Assert.True(config.OutputFileAllowOverwrite);
        }

        [Fact]
        public void AllowOverwriteFalse_BecomesAsk_Once()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.OutputFileAllowOverwrite = false;

            config.OnAfterLoad();

            Assert.Equal(FileExistsAction.Ask, config.OutputFileIfExists);
            Assert.True(config.OutputFileAllowOverwrite);

            // A later choice stays
            config.OutputFileIfExists = FileExistsAction.AddNumber;
            config.OnAfterLoad();
            Assert.Equal(FileExistsAction.AddNumber, config.OutputFileIfExists);
        }
    }
}
