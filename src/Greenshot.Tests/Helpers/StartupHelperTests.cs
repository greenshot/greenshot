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
using Greenshot.Helpers;
using Microsoft.Win32;
using Xunit;

namespace Greenshot.Tests.Helpers
{
    /// <summary>
    /// The start with Windows, against a fake registry below HKCU\Software\Greenshot.Tests
    /// </summary>
    public sealed class StartupHelperTests : IDisposable
    {
        private const string Installed = @"C:\Program Files\Greenshot\Greenshot.exe";
        private const string Portable = @"D:\Portable\Greenshot\Greenshot.exe";
        private static readonly byte[] SwitchedOn = [2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        private static readonly byte[] SwitchedOff = [3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        private readonly string _testRoot = $@"Software\Greenshot.Tests\{Guid.NewGuid():N}";
        private readonly RegistryKey _user;
        private readonly RegistryKey _machine64;
        private readonly RegistryKey _machine32;
        private readonly RunLocations _locations;

        public StartupHelperTests()
        {
            _user = Registry.CurrentUser.CreateSubKey(_testRoot + @"\User");
            _machine64 = Registry.CurrentUser.CreateSubKey(_testRoot + @"\Machine64");
            _machine32 = Registry.CurrentUser.CreateSubKey(_testRoot + @"\Machine32");
            _locations = new RunLocations(_user, _machine64, _machine32);
        }

        public void Dispose()
        {
            _user.Dispose();
            _machine64.Dispose();
            _machine32.Dispose();
            Registry.CurrentUser.DeleteSubKeyTree(_testRoot, false);
        }

        [Theory]
        [InlineData("\"C:\\Program Files\\Greenshot\\Greenshot.exe\"", @"C:\Program Files\Greenshot\Greenshot.exe")]
        [InlineData("\"C:\\Program Files\\Greenshot\\Greenshot.exe\" --language de-DE", @"C:\Program Files\Greenshot\Greenshot.exe")]
        [InlineData(@"C:\Program Files\Greenshot\Greenshot.exe /background", @"C:\Program Files\Greenshot\Greenshot.exe")]
        [InlineData(@"D:\Greenshot\Greenshot.exe", @"D:\Greenshot\Greenshot.exe")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void GetExecutable_ReadsTheRunValue(string runValue, string expected)
        {
            Assert.Equal(expected, StartupHelper.GetExecutable(runValue));
        }

        [Fact]
        public void Enable_WritesTheRunValueOfTheUser()
        {
            Assert.False(StartupHelper.IsAutostartEnabled(_locations, Installed));

            StartupHelper.EnableAutostart(_locations, Installed);

            Assert.Equal(StartupHelper.CreateRunValue(Installed), GetValue(_user, StartupHelper.RunKey));
            Assert.True(StartupHelper.IsAutostartEnabled(_locations, Installed));
            Assert.False(StartupHelper.IsAutostartEnabled(_locations, Portable));
        }

        [Fact]
        public void Enable_ReplacesTheEntryOfAnotherGreenshot()
        {
            SetValue(_user, StartupHelper.RunKey, $"\"{Portable}\"");

            StartupHelper.EnableAutostart(_locations, Installed);

            Assert.Equal(StartupHelper.CreateRunValue(Installed), GetValue(_user, StartupHelper.RunKey));
        }

        [Fact]
        public void SwitchedOffInWindows_IsOff_AndEnableSwitchesItOnAgain()
        {
            SetValue(_user, StartupHelper.RunKey, $"\"{Installed}\"");
            SetValue(_user, StartupHelper.StartupApprovedRunKey, SwitchedOff);
            Assert.False(StartupHelper.IsAutostartEnabled(_locations, Installed));

            StartupHelper.EnableAutostart(_locations, Installed);

            Assert.Null(GetValue(_user, StartupHelper.StartupApprovedRunKey));
            Assert.True(StartupHelper.IsAutostartEnabled(_locations, Installed));
        }

        [Fact]
        public void SwitchedOnInWindows_StaysAsItIs()
        {
            SetValue(_user, StartupHelper.StartupApprovedRunKey, SwitchedOn);

            StartupHelper.EnableAutostart(_locations, Installed);

            Assert.Equal(SwitchedOn, GetValue(_user, StartupHelper.StartupApprovedRunKey));
        }

        [Fact]
        public void EntryForAllUsers_CountsAndIsNotDoubled()
        {
            SetValue(_machine32, StartupHelper.RunKey, $"\"{Installed}\"");
            Assert.True(StartupHelper.IsAutostartEnabled(_locations, Installed));

            StartupHelper.EnableAutostart(_locations, Installed);

            Assert.Null(GetValue(_user, StartupHelper.RunKey));
        }

        [Fact]
        public void EntryForAllUsers_SwitchedOff_InRun32()
        {
            SetValue(_machine32, StartupHelper.RunKey, $"\"{Installed}\"");
            SetValue(_machine64, StartupHelper.StartupApprovedRun32Key, SwitchedOff);
            Assert.False(StartupHelper.IsAutostartEnabled(_locations, Installed));

            StartupHelper.EnableAutostart(_locations, Installed);

            Assert.Equal(StartupHelper.CreateRunValue(Installed), GetValue(_user, StartupHelper.RunKey));
        }

        [Fact]
        public void Disable_RemovesOnlyTheEntriesOfThisGreenshot()
        {
            SetValue(_user, StartupHelper.RunKey, $"\"{Installed}\"");
            SetValue(_machine64, StartupHelper.RunKey, $"\"{Installed}\"");
            SetValue(_machine32, StartupHelper.RunKey, $"\"{Portable}\"");

            StartupHelper.DisableAutostart(_locations, Installed);

            Assert.Null(GetValue(_user, StartupHelper.RunKey));
            Assert.Null(GetValue(_machine64, StartupHelper.RunKey));
            Assert.Equal($"\"{Portable}\"", GetValue(_machine32, StartupHelper.RunKey));
            Assert.False(StartupHelper.IsAutostartEnabled(_locations, Installed));
        }

        private static object GetValue(RegistryKey root, string path)
        {
            using var key = root.OpenSubKey(path);
            return key?.GetValue(StartupHelper.ValueName);
        }

        private static void SetValue(RegistryKey root, string path, object value)
        {
            using var key = root.CreateSubKey(path);
            key.SetValue(StartupHelper.ValueName, value);
        }
    }
}
