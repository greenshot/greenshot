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
using System.IO;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class PluginUtilsTests
    {
        public PluginUtilsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void GetExePath_ProgramName_IsFoundOnThePath()
        {
            string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

            Assert.Equal(expected, PluginUtils.GetExePath("cmd.exe"), ignoreCase: true);
        }

        [Fact]
        public void GetExePath_ExistingFullPath_IsReturned()
        {
            string cmd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

            Assert.Equal(cmd, PluginUtils.GetExePath(cmd), ignoreCase: true);
        }

        [Fact]
        public void GetExePath_MissingFullPath_IsNull()
        {
            string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.exe");

            Assert.Null(PluginUtils.GetExePath(missing));
        }

        [Fact]
        public void GetExePath_InvalidPath_IsNull()
        {
            Assert.Null(PluginUtils.GetExePath("in|valid<.exe"));
        }
    }
}
