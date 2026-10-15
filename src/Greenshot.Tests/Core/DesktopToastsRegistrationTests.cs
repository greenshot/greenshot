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
using Greenshot.Base.Native.Toasts;
using Microsoft.Win32;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Removing the toast registrations the Windows Community Toolkit left. Uses keys of its own in HKCU with an exe name no real
    /// registration has, and removes them afterwards.
    /// </summary>
    public class DesktopToastsRegistrationTests
    {
        private const string AppIdsKey = @"Software\Classes\AppUserModelId";

        [Theory]
        [InlineData("\"C:\\Program Files\\Greenshot\\Greenshot.exe\" -ToastActivated", "Greenshot.exe")]
        [InlineData("C:\\Tools\\Greenshot.exe -ToastActivated", "Greenshot.exe")]
        [InlineData("\"C:\\Users\\a b\\Downloads\\Greenshot-PORTABLE-1.3.304-RELEASE\\Greenshot.exe\"", "Greenshot.exe")]
        [InlineData("\"C:\\Tools\\MyGreenshot.exe\" -ToastActivated", "MyGreenshot.exe")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void GetExeFileName_ReturnsTheFileNameOfTheStartedExe(string commandLine, string expected)
        {
            Assert.Equal(expected, DesktopToasts.GetExeFileName(commandLine));
        }

        [Fact]
        public void RemoveOldRegistrations_RemovesTheToolkitRegistrationsOfTheExe_AndKeepsTheRest()
        {
            string id = Guid.NewGuid().ToString("N");
            string exe = $"GreenshotCleanupTest{id}.exe";
            string ownAppId = $"GreenshotTest.Own.{id}";
            string toolkitAppId = $"C:/Test/{id}/{exe}";
            string foreignAppId = $"GreenshotTest.Foreign.{id}";
            string decoyAppId = $"GreenshotTest.Decoy.{id}";
            string ownClsid = NewClsid(), toolkitClsid = NewClsid(), foreignClsid = NewClsid(), decoyClsid = NewClsid();
            string tempRoot = Path.Combine(Path.GetTempPath(), $"GreenshotCleanupTest{id}");
            string iconDirectory = Path.Combine(tempRoot, "ToastNotificationManagerCompat", "Apps", toolkitClsid);
            Directory.CreateDirectory(iconDirectory);
            File.WriteAllText(Path.Combine(iconDirectory, "Icon.png"), "png");
            try
            {
                // The CustomActivator the Toolkit wrote for the own app id
                CreateAppId(ownAppId, ownClsid, ("DisplayName", "Greenshot"), ("IconBackgroundColor", "FFDDDDDD"), ("Has7.0.1Fix", 1));
                CreateServer(ownClsid, $"\"C:\\Program Files\\Test\\{exe}\" -ToastActivated");
                // An app id the Toolkit created for an exe path
                CreateAppId(toolkitAppId, toolkitClsid, ("DisplayName", "Greenshot"), ("IconUri", Path.Combine(iconDirectory, "Icon.png")), ("Has7.0.1Fix", 1), ("HasSentNotification", 1));
                CreateServer(toolkitClsid, $"\"C:\\Test\\{id}\\{exe}\" -ToastActivated");
                // The Toolkit's CustomActivator in the app id of another app, which has a value of its own
                CreateAppId(foreignAppId, foreignClsid, ("OtherValue", "stays"));
                CreateServer(foreignClsid, $"\"C:\\Test\\{exe}\" -ToastActivated");
                // Another exe whose name only contains the one of ours
                CreateAppId(decoyAppId, decoyClsid, ("DisplayName", "Decoy"));
                CreateServer(decoyClsid, $"\"C:\\Test\\My{exe}\" -ToastActivated");

                var removed = DesktopToasts.RemoveOldRegistrations(ownAppId, exe);

                Assert.Equal(3, removed.Count);
                Assert.Contains(ownAppId, removed);
                Assert.Contains(toolkitAppId, removed);
                Assert.Contains(foreignAppId, removed);

                using (var own = OpenAppId(ownAppId))
                {
                    Assert.NotNull(own);
                    Assert.Equal("Greenshot", own.GetValue("DisplayName"));
                    Assert.Null(own.GetValue("CustomActivator"));
                    Assert.Null(own.GetValue("IconBackgroundColor"));
                    Assert.Null(own.GetValue("Has7.0.1Fix"));
                }
                Assert.False(ServerExists(ownClsid));

                using (var toolkit = OpenAppId(toolkitAppId))
                {
                    Assert.Null(toolkit);
                }
                Assert.False(ServerExists(toolkitClsid));
                Assert.False(Directory.Exists(iconDirectory));

                using (var foreign = OpenAppId(foreignAppId))
                {
                    Assert.NotNull(foreign);
                    Assert.Equal("stays", foreign.GetValue("OtherValue"));
                    Assert.Null(foreign.GetValue("CustomActivator"));
                }
                Assert.False(ServerExists(foreignClsid));

                using (var decoy = OpenAppId(decoyAppId))
                {
                    Assert.Equal(decoyClsid, decoy?.GetValue("CustomActivator"));
                }
                Assert.True(ServerExists(decoyClsid));
            }
            finally
            {
                foreach (var appId in new[] { ownAppId, toolkitAppId, foreignAppId, decoyAppId })
                {
                    Registry.CurrentUser.DeleteSubKeyTree($@"{AppIdsKey}\{appId}", false);
                }
                foreach (var clsid in new[] { ownClsid, toolkitClsid, foreignClsid, decoyClsid })
                {
                    Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{clsid}", false);
                }
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
        }

        private static string NewClsid() => Guid.NewGuid().ToString("B");

        private static void CreateAppId(string appId, string clsid, params (string Name, object Value)[] values)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"{AppIdsKey}\{appId}");
            key.SetValue("CustomActivator", clsid);
            foreach (var (name, value) in values)
            {
                key.SetValue(name, value);
            }
        }

        private static void CreateServer(string clsid, string commandLine)
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{clsid}\LocalServer32");
            key.SetValue(null, commandLine);
        }

        private static RegistryKey OpenAppId(string appId) => Registry.CurrentUser.OpenSubKey($@"{AppIdsKey}\{appId}");

        private static bool ServerExists(string clsid)
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\CLSID\{clsid}");
            return key != null;
        }
    }
}
