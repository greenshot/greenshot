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
        public void FindPackage_Paint_FindsPackage()
        {
            var package = WindowsAppHelper.FindPackage("mspaint.exe", "Paint");
            Assert.NotNull(package);
            Assert.Contains("Paint", package.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindPackage_Notepad_FindsPackage()
        {
            var package = WindowsAppHelper.FindPackage("notepad.exe", "Notepad");
            Assert.NotNull(package);
            Assert.Contains("Notepad", package.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetAppxLogoAsync_PaintPackage_ReturnsValidImage()
        {
            var package = WindowsAppHelper.FindPackage("mspaint.exe", "Paint");
            Assert.NotNull(package);

            using var image = await WindowsAppHelper.GetAppxLogoAsync(package, new NativeSize(64, 64));
            Assert.NotNull(image);
            Assert.True(image.Width > 0);
            Assert.True(image.Height > 0);
        }

        [Fact]
        public void GetAppxLogo_SynchronousCall_ReturnsValidImageWithoutDeadlock()
        {
            var package = WindowsAppHelper.FindPackage("mspaint.exe", "Paint");
            Assert.NotNull(package);

            using var image = WindowsAppHelper.GetAppxLogo(package, new NativeSize(48, 48));
            Assert.NotNull(image);
            Assert.True(image.Width > 0);
            Assert.True(image.Height > 0);
        }

        [Fact]
        public void GetAppxLogo_Paint_IconIsNotExcessivelyPadded()
        {
            var package = WindowsAppHelper.FindPackage("mspaint.exe", "Paint");
            Assert.NotNull(package);

            using var image = WindowsAppHelper.GetAppxLogo(package, new NativeSize(64, 64)) as System.Drawing.Bitmap;
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
            double coverage = (double)contentWidth / image.Width;

            // Coverage should be >= 75% (tightly cropped), unlike the raw 150x150 tile logo which was ~36%
            Assert.True(coverage >= 0.75, $"Expected icon coverage >= 75%, but got {coverage:P0} ({contentWidth}x{contentHeight} in {image.Width}x{image.Height})");
        }

        [Fact]
        public void GetAppLogo_ByCommandLineOrName_ReturnsImageAndCaches()
        {
            var image1 = WindowsAppHelper.GetAppLogo("mspaint.exe", "Paint");
            Assert.NotNull(image1);
            Assert.True(image1.Width > 0);

            // Second call should come from cache
            var image2 = WindowsAppHelper.GetAppLogo("mspaint.exe", "Paint");
            Assert.Same(image1, image2);
        }

        [Fact]
        public void GetAppLogo_CalledFromStaThread_DoesNotDeadlock()
        {
            Exception threadEx = null;
            System.Drawing.Image img = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    img = WindowsAppHelper.GetAppLogo("mspaint.exe", "Paint");
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
        public void GetAppLogo_AppExecutionAliasPath_ReturnsImage()
        {
            string aliasPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "WindowsApps",
                "mspaint.exe");

            if (File.Exists(aliasPath))
            {
                var image = WindowsAppHelper.GetAppLogo(aliasPath);
                Assert.NotNull(image);
                Assert.True(image.Width > 0);
            }
        }

        [Fact]
        public void IconCache_IconForCommand_ResolvesAppIconForExternalCommand()
        {
            var config = IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
            if (config != null)
            {
                if (config.Commands == null) config.Commands = new System.Collections.Generic.List<string>();
                if (config.Commandline == null) config.Commandline = new System.Collections.Generic.Dictionary<string, string>();

                string cmdName = "TestPaintApp";
                config.Commands.Add(cmdName);
                config.Commandline[cmdName] = "mspaint.exe";

                try
                {
                    var icon = IconCache.IconForCommand(cmdName);
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
}
