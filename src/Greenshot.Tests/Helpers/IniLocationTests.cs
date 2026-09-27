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
using Dapplo.Ini;
using Greenshot.Configuration;
using Greenshot.Helpers;
using Xunit;

namespace Greenshot.Tests.Helpers
{
    /// <summary>
    /// Registering an extra ini file makes the registry-wide IniConfigRegistry.Get() / GetSection() throw,
    /// so these tests must not run in parallel with the rest of the suite.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class IniRegistryCollection
    {
        public const string Name = "IniRegistry";
    }

    [Collection(IniRegistryCollection.Name)]
    public sealed class IniLocationTests : IDisposable
    {
        // A unique name so the real greenshot.ini / design-time registrations are never touched.
        private const string IniFileName = "greenshot-inilocation-test.ini";

        private readonly string _root;
        private readonly string _appData;
        private readonly string _startup;
        private readonly string _iniDirectory;

        public IniLocationTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "greenshot-inilocation-" + Guid.NewGuid().ToString("N"));
            _appData = Path.Combine(_root, "appdata");
            _startup = Path.Combine(_root, "startup");
            _iniDirectory = Path.Combine(_root, "override");
            Directory.CreateDirectory(_appData);
            Directory.CreateDirectory(_startup);
        }

        public void Dispose()
        {
            IniConfigRegistry.Unregister(IniFileName);
            try
            {
                Directory.Delete(_root, true);
            }
            catch (IOException)
            {
                // Best effort cleanup of the temp directory
            }
        }

        private static void WriteIni(string directory, string fileName, string ocrLanguage)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), $"[Win10]\r\nOcrLanguage={ocrLanguage}\r\n");
        }

        private (IniConfig Config, IWin10Configuration Section, string UsedDirectory) Build(string iniDirectory)
        {
            var builder = IniConfigRegistry.ForFile(IniFileName);
            var usedDirectory = IniLocation.Configure(builder, iniDirectory, _appData, _startup);
            var section = new Win10ConfigurationImpl();
            var config = builder.RegisterSection<IWin10Configuration>(section).Build();
            return (config, section, usedDirectory);
        }

        [Fact]
        public void Configure_WithIniDirectory_IgnoresExistingAppDataIni()
        {
            WriteIni(_appData, IniFileName, "appdata");
            WriteIni(_startup, IniFileName, "startup");

            var (config, section, usedDirectory) = Build(_iniDirectory);

            Assert.Equal(_iniDirectory, usedDirectory);
            Assert.Equal(Path.Combine(_iniDirectory, IniFileName), config.LoadedFromPath);
            Assert.Equal("", section.OcrLanguage);

            config.Save();

            Assert.True(File.Exists(Path.Combine(_iniDirectory, IniFileName)));
            Assert.Contains("OcrLanguage=appdata", File.ReadAllText(Path.Combine(_appData, IniFileName)));
        }

        [Fact]
        public void Configure_WithIniDirectory_ReadsExistingIniFromThere()
        {
            WriteIni(_appData, IniFileName, "appdata");
            WriteIni(_iniDirectory, IniFileName, "override");

            var (config, section, _) = Build(_iniDirectory);

            Assert.Equal(Path.Combine(_iniDirectory, IniFileName), config.LoadedFromPath);
            Assert.Equal("override", section.OcrLanguage);
        }

        [Fact]
        public void Configure_WithMissingIniDirectory_CreatesIt()
        {
            var nested = Path.Combine(_iniDirectory, "nested");

            var (config, _, usedDirectory) = Build(nested);

            Assert.True(Directory.Exists(nested));
            Assert.Equal(nested, usedDirectory);
            Assert.Equal(Path.Combine(nested, IniFileName), config.LoadedFromPath);
        }

        [Fact]
        public void Configure_WithIniDirectory_StillAppliesFixedFileFromStartupPath()
        {
            WriteIni(_startup, IniLocation.ConstantsFileName, "fixed");

            var (_, section, _) = Build(_iniDirectory);

            Assert.Equal("fixed", section.OcrLanguage);
        }

        [Fact]
        public void Configure_WithIniDirectory_PrefersDefaultsFileInIniDirectory()
        {
            WriteIni(_startup, IniLocation.DefaultsFileName, "startup-defaults");
            WriteIni(_iniDirectory, IniLocation.DefaultsFileName, "override-defaults");

            var (_, section, _) = Build(_iniDirectory);

            Assert.Equal("override-defaults", section.OcrLanguage);
        }

        [Fact]
        public void Configure_WithoutIniDirectory_PrefersAppDataOverStartupPath()
        {
            WriteIni(_appData, IniFileName, "appdata");
            WriteIni(_startup, IniFileName, "startup");

            var (config, section, usedDirectory) = Build(null);

            Assert.Null(usedDirectory);
            Assert.Equal(Path.Combine(_appData, IniFileName), config.LoadedFromPath);
            Assert.Equal("appdata", section.OcrLanguage);
        }

        [Fact]
        public void Configure_WithoutIniDirectory_WritesToAppDataWhenNoIniExists()
        {
            var (config, _, _) = Build("");

            Assert.Equal(Path.Combine(_appData, IniFileName), config.LoadedFromPath);
        }

        [Fact]
        public void Configure_WithUnusableIniDirectory_FallsBackToDefaultLocations()
        {
            // A file with the same name blocks creating the directory.
            File.WriteAllText(_iniDirectory, "");
            WriteIni(_appData, IniFileName, "appdata");

            var (config, section, usedDirectory) = Build(_iniDirectory);

            Assert.Null(usedDirectory);
            Assert.Equal(Path.Combine(_appData, IniFileName), config.LoadedFromPath);
            Assert.Equal("appdata", section.OcrLanguage);
        }
    }
}
