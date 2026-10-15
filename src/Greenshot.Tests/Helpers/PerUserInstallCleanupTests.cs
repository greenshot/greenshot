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
using Greenshot.Helpers;
using Microsoft.Win32;
using Xunit;

namespace Greenshot.Tests.Helpers
{
    /// <summary>
    /// Finding an older Greenshot installed for the user only, against a fake registry below HKCU\Software\Greenshot.Tests
    /// </summary>
    public sealed class PerUserInstallCleanupTests : IDisposable
    {
        private const string AllUsers = @"C:\Program Files\Greenshot\";

        private readonly string _testRoot = $@"Software\Greenshot.Tests\{Guid.NewGuid():N}";
        private readonly string _oldInstallation;
        private readonly string _oldUninstaller;
        private readonly RegistryKey _user;
        private readonly RegistryKey _machine;

        public PerUserInstallCleanupTests()
        {
            _user = Registry.CurrentUser.CreateSubKey(_testRoot + @"\User");
            _machine = Registry.CurrentUser.CreateSubKey(_testRoot + @"\Machine");
            _oldInstallation = Path.Combine(Path.GetTempPath(), "Greenshot.Tests." + Guid.NewGuid().ToString("N")) + @"\";
            Directory.CreateDirectory(_oldInstallation);
            _oldUninstaller = Path.Combine(_oldInstallation, "unins000.exe");
            File.WriteAllText(_oldUninstaller, string.Empty);
        }

        public void Dispose()
        {
            _user.Dispose();
            _machine.Dispose();
            Registry.CurrentUser.DeleteSubKeyTree(_testRoot, false);
            Directory.Delete(_oldInstallation, true);
        }

        private static void Register(RegistryKey root, string location, string uninstaller = null)
        {
            using var key = root.CreateSubKey(PerUserInstallCleanup.UninstallKey);
            key.SetValue("InstallLocation", location);
            key.SetValue("UninstallString", $"\"{uninstaller ?? Path.Combine(location, "unins000.exe")}\"");
            key.SetValue("DisplayVersion", "1.3.312");
        }

        [Fact]
        public void TheAllUsersGreenshot_FindsTheOldOneOfTheUser()
        {
            Register(_machine, AllUsers);
            Register(_user, _oldInstallation, _oldUninstaller);

            Assert.Equal(_oldInstallation, PerUserInstallCleanup.FindOldPerUserInstallation(_user, _machine, AllUsers, out string uninstaller, out string version));
            Assert.Equal(_oldUninstaller, uninstaller);
            Assert.Equal("1.3.312", version);
        }

        [Fact]
        public void IsKept_ComparesTheFolders()
        {
            Assert.True(PerUserInstallCleanup.IsKept([@"C:\Users\anna\AppData\Local\Programs\Greenshot"], @"c:\users\anna\appdata\local\programs\greenshot\"));
            Assert.False(PerUserInstallCleanup.IsKept([@"C:\Users\carl\AppData\Local\Programs\Greenshot\"], @"C:\Users\anna\AppData\Local\Programs\Greenshot\"));
            Assert.False(PerUserInstallCleanup.IsKept(null, @"C:\Users\anna\AppData\Local\Programs\Greenshot\"));
        }

        [Fact]
        public void NothingInstalledForTheUser_NothingToDo()
        {
            Register(_machine, AllUsers);

            Assert.Null(PerUserInstallCleanup.FindOldPerUserInstallation(_user, _machine, AllUsers, out _, out _));
        }

        [Theory]
        [InlineData(@"D:\code\greenshot\src\Greenshot\bin\Debug\net480\")]
        [InlineData(@"D:\Portable\Greenshot\")]
        public void NotTheAllUsersGreenshot_LeavesItAlone(string runningDirectory)
        {
            Register(_machine, AllUsers);
            Register(_user, _oldInstallation, _oldUninstaller);

            Assert.Null(PerUserInstallCleanup.FindOldPerUserInstallation(_user, _machine, runningDirectory, out _, out _));
        }

        [Fact]
        public void NoInstallationForAllUsers_LeavesItAlone()
        {
            Register(_user, _oldInstallation, _oldUninstaller);

            Assert.Null(PerUserInstallCleanup.FindOldPerUserInstallation(_user, _machine, _oldInstallation, out _, out _));
        }

        [Fact]
        public void UninstallerGone_LeavesItAlone()
        {
            Register(_machine, AllUsers);
            Register(_user, @"C:\Users\Nobody\AppData\Local\Programs\Greenshot\");

            Assert.Null(PerUserInstallCleanup.FindOldPerUserInstallation(_user, _machine, AllUsers, out string uninstaller, out _));
            Assert.Null(uninstaller);
        }

        [Theory]
        [InlineData(@"C:\Program Files\Greenshot\", @"C:\Program Files\Greenshot", true)]
        [InlineData(@"c:\program files\greenshot\", @"C:\Program Files\Greenshot\", true)]
        [InlineData(@"C:\Program Files\Greenshot\", @"C:\Users\anna\AppData\Local\Programs\Greenshot\", false)]
        [InlineData(null, @"C:\Program Files\Greenshot\", false)]
        public void IsSameDirectory_IgnoresCaseAndTheTrailingBackslash(string first, string second, bool expected)
        {
            Assert.Equal(expected, PerUserInstallCleanup.IsSameDirectory(first, second));
        }
    }
}
