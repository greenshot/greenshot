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
using Greenshot.UI.SelfService;
using Xunit;

namespace Greenshot.Tests.Forms
{
    public class ChecksumValidationTests
    {
        [Theory]
        [InlineData("unins000.exe", true)]
        [InlineData("unins001.dat", true)]
        [InlineData("readme.txt", true)]
        [InlineData("manifest.spdx.json.sha256", true)]
        [InlineData("log4net.xml", true)]
        [InlineData(@"Languages\help-en-US.html", true)]
        [InlineData(@"Languages\language-de-DE.xml", true)]
        [InlineData(@"Languages\greenshot.en-US.ini", true)]
        [InlineData(@"Languages\greenshot.imgur.de-DE.ini", true)]
        [InlineData(@"Plugins\Greenshot.Plugin.Jira\Greenshot.Plugin.Jira.pdb", true)]
        // A library left behind by an older version, and program files, are not skipped
        [InlineData("Nodify.dll", false)]
        [InlineData("Greenshot.exe", false)]
        [InlineData(@"Languages\Greenshot.Base.dll", false)]
        [InlineData(@"Plugins\Greenshot.Plugin.Jira\readme.txt", false)]
        public void CanSkipFile_SkipsOnlyFilesThatAreNotProgramFiles(string path, bool skipped)
        {
            Assert.Equal(skipped, ChecksumSectionViewModel.CanSkipFile(path));
        }

        [Fact]
        public void FilesOfAPluginThatIsNotInstalled_AreNotExpected()
        {
            string baseDir = Path.Combine(Path.GetTempPath(), "gs_checksum_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Plugins", "Greenshot.Plugin.Jira"));
            try
            {
                // Installed plugin: its files are checked
                Assert.False(ChecksumSectionViewModel.IsFileOfMissingPlugin(baseDir, "Plugins/Greenshot.Plugin.Jira/Dapplo.Jira.dll"));
                // Plugin not installed: its files are not reported as missing
                Assert.True(ChecksumSectionViewModel.IsFileOfMissingPlugin(baseDir, @"Plugins\Greenshot.Plugin.Box\Greenshot.Plugin.Box.dll"));
                // Files of Greenshot itself are always checked
                Assert.False(ChecksumSectionViewModel.IsFileOfMissingPlugin(baseDir, "Greenshot.Base.dll"));
                Assert.False(ChecksumSectionViewModel.IsFileOfMissingPlugin(baseDir, "Plugins/readme.txt"));
            }
            finally
            {
                Directory.Delete(baseDir, true);
            }
        }
    }
}
