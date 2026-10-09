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
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Plugin.ExternalCommand;
using Dapplo.Ini;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class WindowsAppHelperTests
    {
        public WindowsAppHelperTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void FindPackage_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(WindowsAppHelper.FindPackage(null, null));
            Assert.Null(WindowsAppHelper.FindPackage("", ""));
            Assert.Null(WindowsAppHelper.FindPackage("   ", "   "));
        }

        [Fact]
        public void FindPackage_NonExistentPackage_ReturnsNull()
        {
            Assert.Null(WindowsAppHelper.FindPackage("DefNonExistentAppXYZ12345.exe", "DefNonExistentAppXYZ12345"));
        }

        [Fact]
        public void FindPackage_ByExecutableName_FindsInstalledApp()
        {
            var app = InstalledTestApp.Get();
            var package = WindowsAppHelper.FindPackage(app.ExeName, app.DisplayName);
            Assert.NotNull(package);
            Assert.Equal(app.FamilyName, package.Id.FamilyName);
        }

        [Fact]
        public void FindPackage_ByAliasPath_FindsInstalledApp()
        {
            var app = InstalledTestApp.Get();
            var package = WindowsAppHelper.FindPackage(app.AliasPath);
            Assert.NotNull(package);
            Assert.Equal(app.FamilyName, package.Id.FamilyName);
        }

        [Fact]
        public async Task GetAppLogoAsync_ConcurrentLookups_DoNotCrash()
        {
            // Regression: concurrent lookups read properties of the same cached WinRT packages,
            // which crashed with an (uncatchable) AccessViolationException in Windows.ApplicationModel.
            var app = InstalledTestApp.Get();
            var lookups = new Task<System.Drawing.Image>[16];
            for (int i = 0; i < lookups.Length; i++)
            {
                // Different sizes: different cache keys, so every lookup searches the packages
                int size = 16 + i;
                lookups[i] = WindowsAppHelper.GetAppLogoAsync(i % 2 == 0 ? app.ExeName : $"NoSuchApp{i}.exe", i % 2 == 0 ? app.DisplayName : $"NoSuchApp{i}", new NativeSize(size, size));
            }

            var logos = await Task.WhenAll(lookups);
            for (int i = 0; i < logos.Length; i += 2)
            {
                Assert.NotNull(logos[i]);
            }
        }

        [Fact]
        public async Task GetAppxLogoAsync_InstalledAppPackage_ReturnsValidImage()
        {
            var package = InstalledTestApp.Get().Package;

            using var image = await WindowsAppHelper.GetAppxLogoAsync(package, new NativeSize(64, 64));
            Assert.NotNull(image);
            Assert.True(image.Width > 0);
            Assert.True(image.Height > 0);
        }

        [Fact]
        public async Task GetAppxLogoAsync_48_ReturnsValidImage()
        {
            var package = InstalledTestApp.Get().Package;

            using var image = await WindowsAppHelper.GetAppxLogoAsync(package, new NativeSize(48, 48));
            Assert.NotNull(image);
            Assert.True(image.Width > 0);
            Assert.True(image.Height > 0);
        }

        [Fact]
        public async Task GetAppxLogo_InstalledApp_IconIsNotExcessivelyPadded()
        {
            var app = InstalledTestApp.Get();
            var package = app.Package;

            using var image = await WindowsAppHelper.GetAppxLogoAsync(package, new NativeSize(64, 64)) as System.Drawing.Bitmap;
            Assert.NotNull(image);

            // Verify content covers the majority of the image canvas (no huge 65%+ tile margins)
            int minX = image.Width, maxX = 0, minY = image.Height, maxY = 0;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (image.GetPixel(x, y).A > 15)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            int contentWidth = maxX - minX + 1;
            int contentHeight = maxY - minY + 1;
            // Logos are not always square: the trimmed logo must fill the canvas in at least one dimension
            double coverage = Math.Max((double)contentWidth / image.Width, (double)contentHeight / image.Height);

            // Coverage should be >= 75% (tightly cropped), unlike the raw 150x150 tile logo which was ~36%
            Assert.True(coverage >= 0.75, $"Expected icon coverage >= 75% for {app}, but got {coverage:P0} ({contentWidth}x{contentHeight} in {image.Width}x{image.Height})");
        }

        [Fact]
        public async Task GetAppLogo_ByCommandLineOrName_ReturnsImageAndCaches()
        {
            var app = InstalledTestApp.Get();
            var image1 = await WindowsAppHelper.GetAppLogoAsync(app.ExeName, app.DisplayName);
            Assert.NotNull(image1);
            Assert.True(image1.Width > 0);

            // Second call should come from cache
            var image2 = await WindowsAppHelper.GetAppLogoAsync(app.ExeName, app.DisplayName);
            Assert.Same(image1, image2);
        }

        [Fact]
        public void GetAppLogo_CalledFromStaThread_DoesNotDeadlock()
        {
            var app = InstalledTestApp.Get();
            Exception threadEx = null;
            System.Drawing.Image img = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    // Blocking on purpose: the WinRT calls must not need the STA thread
                    img = WindowsAppHelper.GetAppLogoAsync(app.ExeName, app.DisplayName).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            bool completed = thread.Join(TimeSpan.FromSeconds(5));
            Assert.True(completed, "GetAppLogo should complete within 5 seconds without STA deadlock");
            Assert.Null(threadEx);
            Assert.NotNull(img);
        }

        [Fact]
        public async Task GetAppLogo_AppExecutionAliasPath_ReturnsImage()
        {
            var app = InstalledTestApp.Get();
            var image = await WindowsAppHelper.GetAppLogoAsync(app.AliasPath);
            Assert.NotNull(image);
            Assert.True(image.Width > 0);
        }

        [Fact]
        public async Task IconCache_IconForCommand_ResolvesAppIconForExternalCommand()
        {
            // The test configures its own external command for an app that is installed on this machine
            var app = InstalledTestApp.Get();
            var config = IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
            Assert.NotNull(config);
            if (config.Commands == null) config.Commands = new System.Collections.Generic.List<string>();
            if (config.Commandline == null) config.Commandline = new System.Collections.Generic.Dictionary<string, string>();

            string cmdName = $"TestApp_{Guid.NewGuid():N}";
            config.Commands.Add(cmdName);
            config.Commandline[cmdName] = app.ExeName;

            try
            {
                var icon = await IconCache.IconForCommandAsync(cmdName);
                Assert.NotNull(icon);
                Assert.True(icon.Width > 0);
                Assert.True(icon.Height > 0);
            }
            finally
            {
                config.Commands.Remove(cmdName);
                config.Commandline.Remove(cmdName);
            }
        }
    }
}
