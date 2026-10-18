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
using Greenshot.SelfService.ViewModels;
using Microsoft.Win32;
using Xunit;

namespace Greenshot.Tests.Forms
{
    public class SetupSectionViewModelTests
    {
        private const string ProgramFilesGreenshot = @"C:\Program Files\Greenshot\Greenshot.exe";

        [Theory]
        [InlineData(@"{6D809377-6AF0-444B-8957-A3773F02200E}\Greenshot\Greenshot.exe", true)]
        [InlineData(@"C:\Program Files\Greenshot\Greenshot.exe", true)]
        [InlineData(@"c:\program files\greenshot\greenshot.exe", true)]
        [InlineData(@"{6D809377-6AF0-444B-8957-A3773F02200E}\Other\Greenshot.exe", false)]
        [InlineData(@"D:\Portable\Greenshot\Greenshot.exe", false)]
        [InlineData("", false)]
        public void IsSameExecutable_ResolvesKnownFolderPaths(string registeredPath, bool expected)
        {
            Assert.Equal(expected, SetupSectionViewModel.IsSameExecutable(registeredPath, ProgramFilesGreenshot));
        }

        [Fact]
        public void IsHiddenInOverflow_FollowsIsPromoted_OfTheIconWithTheId()
        {
            string testRoot = $@"Software\Greenshot.Tests\{Guid.NewGuid():N}";
            using var root = Registry.CurrentUser.CreateSubKey(testRoot);
            try
            {
                // Windows 10: no NotifyIconSettings
                Assert.False(SetupSectionViewModel.IsHiddenInOverflow(root, ProgramFilesGreenshot, 0));
                Assert.False(SetupSectionViewModel.ShowNextToClock(root, ProgramFilesGreenshot, 0));

                using var settings = root.CreateSubKey(@"Control Panel\NotifyIconSettings");
                using (var other = settings.CreateSubKey("1"))
                {
                    other.SetValue("ExecutablePath", @"C:\Windows\explorer.exe");
                    other.SetValue("UID", 0, RegistryValueKind.DWord);
                }
                // The icon of an older version (WinForms used id 1) was shown, that doesn't count for the current one
                using (var old = settings.CreateSubKey("2"))
                {
                    old.SetValue("ExecutablePath", @"{6D809377-6AF0-444B-8957-A3773F02200E}\Greenshot\Greenshot.exe");
                    old.SetValue("UID", 1, RegistryValueKind.DWord);
                    old.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                }
                // Greenshot's icon with id 0 has no key (yet)
                Assert.False(SetupSectionViewModel.IsHiddenInOverflow(root, ProgramFilesGreenshot, 0));
                Assert.False(SetupSectionViewModel.ShowNextToClock(root, ProgramFilesGreenshot, 0));

                using var icon = settings.CreateSubKey("3");
                icon.SetValue("ExecutablePath", @"{6D809377-6AF0-444B-8957-A3773F02200E}\Greenshot\Greenshot.exe");
                icon.SetValue("UID", 0, RegistryValueKind.DWord);
                // A new icon is hidden until the user shows it
                Assert.True(SetupSectionViewModel.IsHiddenInOverflow(root, ProgramFilesGreenshot, 0));

                icon.SetValue("IsPromoted", 0, RegistryValueKind.DWord);
                Assert.True(SetupSectionViewModel.IsHiddenInOverflow(root, ProgramFilesGreenshot, 0));

                Assert.True(SetupSectionViewModel.ShowNextToClock(root, ProgramFilesGreenshot, 0));
                Assert.Equal(1, icon.GetValue("IsPromoted"));
                Assert.False(SetupSectionViewModel.IsHiddenInOverflow(root, ProgramFilesGreenshot, 0));

                using var explorer = settings.OpenSubKey("1");
                Assert.Null(explorer.GetValue("IsPromoted"));
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(testRoot, false);
            }
        }
    }
}
