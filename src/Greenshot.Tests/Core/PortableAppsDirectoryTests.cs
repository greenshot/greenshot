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

using System;
using System.IO;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Finding the PortableApps.com package from the folder of Greenshot.exe, over temporary folders
    /// </summary>
    public sealed class PortableAppsDirectoryTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "GreenshotPortableAppsTest_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Fact]
        public void FindPortableAppsDirectory_GreenshotInAppOfPackage_ReturnsPackage()
        {
            var package = Path.Combine(_root, "GreenshotPortable");
            Directory.CreateDirectory(Path.Combine(package, @"App\AppInfo"));
            File.WriteAllText(Path.Combine(package, @"App\AppInfo\appinfo.ini"), "[Format]");
            var applicationDirectory = Directory.CreateDirectory(Path.Combine(package, @"App\Greenshot")).FullName;

            Assert.Equal(package, GreenshotEnvironment.FindPortableAppsDirectory(applicationDirectory));
            // AppContext.BaseDirectory ends with a backslash
            Assert.Equal(package, GreenshotEnvironment.FindPortableAppsDirectory(applicationDirectory + @"\"));
        }

        [Fact]
        public void FindPortableAppsDirectory_WithoutAppInfo_ReturnsNull()
        {
            var applicationDirectory = Directory.CreateDirectory(Path.Combine(_root, @"Tools\Greenshot\App\Greenshot")).FullName;

            Assert.Null(GreenshotEnvironment.FindPortableAppsDirectory(applicationDirectory));
        }

        [Fact]
        public void FindPortableAppsDirectory_Greenshot1xLayout_ReturnsNull()
        {
            // Greenshot 1.x had Greenshot.exe in the main folder of the package
            var package = Directory.CreateDirectory(Path.Combine(_root, "GreenshotPortable")).FullName;
            Directory.CreateDirectory(Path.Combine(package, @"App\AppInfo"));
            File.WriteAllText(Path.Combine(package, @"App\AppInfo\appinfo.ini"), "[Format]");

            Assert.Null(GreenshotEnvironment.FindPortableAppsDirectory(package));
        }
    }
}
