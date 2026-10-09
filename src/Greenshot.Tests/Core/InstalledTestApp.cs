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
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Windows.ApplicationModel;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// An installed packaged (MSIX/AppX) app with an App Execution Alias and a logo, found on the machine running the tests.
    /// Tests use it instead of hard coding a specific app (like Paint), which is not installed everywhere
    /// (e.g. Windows Server, LTSC or N editions). We assume at least one such app exists; if not, the tests fail with a clear message.
    /// </summary>
    internal sealed class InstalledTestApp
    {
        /// <summary>Inbox apps preferred for deterministic results; any other alias is used when none of these exists.</summary>
        private static readonly string[] PreferredAliases = { "mspaint.exe", "notepad.exe", "wt.exe", "winget.exe" };

        private static readonly Lazy<InstalledTestApp> Instance = new Lazy<InstalledTestApp>(Discover);

        private InstalledTestApp(string aliasPath, Package package)
        {
            AliasPath = aliasPath;
            Package = package;
            ExeName = Path.GetFileName(aliasPath);
            DisplayName = package.DisplayName;
            FamilyName = package.Id.FamilyName;
        }

        /// <summary>Full path of the App Execution Alias, e.g. %LOCALAPPDATA%\Microsoft\WindowsApps\mspaint.exe</summary>
        public string AliasPath { get; }

        /// <summary>File name of the alias, e.g. mspaint.exe</summary>
        public string ExeName { get; }

        public string DisplayName { get; }

        public string FamilyName { get; }

        public Package Package { get; }

        public override string ToString() => $"{DisplayName} ({ExeName}, {FamilyName})";

        public static InstalledTestApp Get()
        {
            var app = Instance.Value;
            Assert.True(app != null,
                @"No packaged app with an App Execution Alias and a logo was found in %LOCALAPPDATA%\Microsoft\WindowsApps; these tests need at least one.");
            return app;
        }

        private static InstalledTestApp Discover()
        {
            string aliasDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps");
            if (!Directory.Exists(aliasDirectory))
            {
                return null;
            }

            var candidates = PreferredAliases
                .Select(alias => Path.Combine(aliasDirectory, alias))
                .Concat(Directory.GetFiles(aliasDirectory, "*.exe").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (string aliasPath in candidates)
            {
                try
                {
                    var package = WindowsAppHelper.FindPackage(aliasPath);
                    if (package == null || string.IsNullOrEmpty(package.DisplayName))
                    {
                        continue;
                    }
                    using (var logo = System.Threading.Tasks.Task.Run(() => WindowsAppHelper.GetAppxLogoAsync(package, new NativeSize(48, 48))).GetAwaiter().GetResult())
                    {
                        if (logo != null)
                        {
                            return new InstalledTestApp(aliasPath, package);
                        }
                    }
                }
                catch (Exception)
                {
                    // Try the next alias
                }
            }
            return null;
        }
    }
}
