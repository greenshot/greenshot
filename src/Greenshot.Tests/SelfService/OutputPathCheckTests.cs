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
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.SelfService;
using Xunit;

namespace Greenshot.Tests.SelfService
{
    /// <summary>
    /// The check of the screenshot folder in Self-Service: each reason why screenshots don't end up where the user expects them
    /// </summary>
    public sealed class OutputPathCheckTests : IDisposable
    {
        private const string Pattern = "greenshot ${payload.time:yyyy-MM-dd HH_mm_ss}";
        private static readonly string[] NoOneDrive = Array.Empty<string>();
        private readonly string _folder;

        public OutputPathCheckTests()
        {
            _folder = Path.Combine(Path.GetTempPath(), "greenshot-outputpath-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_folder, true);
            }
            catch
            {
                // ignored, it's in the temp folder
            }
        }

        [Fact]
        public void ExistingWritableFolder_IsOk_WithAnExampleFile()
        {
            var result = OutputPathCheck.Run(_folder, Pattern, "png", NoOneDrive);

            Assert.Equal(OutputPathFinding.Ok, result.Items.First().Finding);
            Assert.Equal(OutputPathSeverity.Ok, result.Worst);
            Assert.StartsWith(_folder, result.ExampleFile);
            Assert.EndsWith(".png", result.ExampleFile);
            // The write test leaves nothing behind
            Assert.Empty(Directory.GetFiles(_folder));
        }

        [Fact]
        public void MissingFolder_IsAWarning_BecauseGreenshotCreatesIt()
        {
            var result = OutputPathCheck.Run(Path.Combine(_folder, "missing"), Pattern, "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.Missing && i.Severity == OutputPathSeverity.Warning);
            Assert.DoesNotContain(result.Items, i => i.Finding == OutputPathFinding.Ok);
        }

        [Fact]
        public void FolderOnAMissingDrive_IsAnError()
        {
            string drive = GetUnusedDrive();

            var result = OutputPathCheck.Run(drive + @"Screenshots", Pattern, "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.DriveMissing && i.Severity == OutputPathSeverity.Error && i.Detail == drive);
        }

        [Fact]
        public void FolderWithVariables_IsInformation_AndShowsTheFilledInFolder()
        {
            var result = OutputPathCheck.Run(Path.Combine(_folder, "${time.now:yyyy}"), Pattern, "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.ContainsVariables && i.Severity == OutputPathSeverity.Info);
            Assert.Equal(Path.Combine(_folder, DateTime.Now.Year.ToString()), result.ExpandedPath);
        }

        [Fact]
        public void GetOutputDirectory_FolderWithVariables_IsFilledInAndCreated_TheSettingStays()
        {
            // A configuration of its own: the tests don't touch the user's greenshot.ini
            var configuration = new CoreConfigurationImpl { OutputDirectory = Path.Combine(_folder, "${time.now:yyyy}") };

            string directory = configuration.GetOutputDirectory(null, out string problem);

            Assert.Null(problem);
            Assert.Equal(Path.Combine(_folder, DateTime.Now.Year.ToString()), directory);
            Assert.True(Directory.Exists(directory));
            Assert.Equal(Path.Combine(_folder, "${time.now:yyyy}"), configuration.OutputDirectory);
        }

        [Fact]
        public void GetOutputDirectory_MissingFolder_IsCreated()
        {
            string folder = Path.Combine(_folder, "deleted", "Screenshots");
            var configuration = new CoreConfigurationImpl { OutputDirectory = folder };

            Assert.Equal(folder, configuration.GetOutputDirectory(null, out string problem));
            Assert.Null(problem);
            Assert.True(Directory.Exists(folder));
        }

        [Fact]
        public void GetOutputDirectory_MissingDrive_UsesTheDefaultFolderOnce_TheSettingStays()
        {
            string folder = GetUnusedDrive() + "Screenshots";
            var configuration = new CoreConfigurationImpl { OutputDirectory = folder };

            string directory = configuration.GetOutputDirectory(null, out string problem);

            Assert.NotNull(problem);
            Assert.NotEqual(folder, directory);
            Assert.True(Directory.Exists(directory));
            // The next save tries the configured folder again, e.g. when the network drive is back
            Assert.Equal(folder, configuration.OutputDirectory);
        }

        [Fact]
        public void ValidateAndCorrectOutputDirectory_KeepsAMissingFolder_SetsTheDefaultWhenEmpty()
        {
            string folder = GetUnusedDrive() + "Screenshots";
            var configuration = new CoreConfigurationImpl { OutputDirectory = folder };

            configuration.ValidateAndCorrectOutputDirectory();
            Assert.Equal(folder, configuration.OutputDirectory);

            configuration.OutputDirectory = null;
            configuration.ValidateAndCorrectOutputDirectory();
            Assert.False(string.IsNullOrEmpty(configuration.OutputDirectory));
        }

        [Fact]
        public void FolderWithAValueOfTheCapture_IsAnError()
        {
            var result = OutputPathCheck.Run(Path.Combine(_folder, "${payload.title}"), Pattern, "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.ContainsVariables && i.Severity == OutputPathSeverity.Error);
        }

        [Fact]
        public void Settings_Of13_AreConvertedOnce_AndStay()
        {
            var configuration = new CoreConfigurationImpl
            {
                OutputFilePath = @"C:\Shots\${YYYY}",
                OutputFileFilenamePattern = "${capturetime:d\"yyyy-MM-dd\"} ${title} ${NUM}",
                OutputPrintFooterPattern = "${title}"
            };

            configuration.OnAfterLoad();

            Assert.Equal(@"C:\Shots\${payload.time:yyyy}", configuration.OutputDirectory);
            Assert.Equal("${payload.time:yyyy-MM-dd} ${payload.title} ${counter.global:000000}", configuration.OutputFileNameTemplate);
            Assert.Equal("${payload.title}", configuration.OutputPrintFooterTemplate);
            // For going back to 1.3
            Assert.Equal("${title}", configuration.OutputPrintFooterPattern);

            // A template set in 1.4 stays
            configuration.OutputFileNameTemplate = "${payload.title}";
            configuration.OnAfterLoad();
            Assert.Equal("${payload.title}", configuration.OutputFileNameTemplate);
        }

        /// <summary>
        /// A drive letter which isn't used on this PC, e.g. "Q:\"
        /// </summary>
        private static string GetUnusedDrive()
        {
            var used = DriveInfo.GetDrives().Select(d => char.ToUpperInvariant(d.Name[0])).ToList();
            char letter = "QRSTUVWXYZJKLMNOP".First(c => !used.Contains(c));
            return letter + @":\";
        }

        [Theory]
        [InlineData(null)]
        [InlineData("  ")]
        public void NoFolder_IsAWarning(string path)
        {
            var result = OutputPathCheck.Run(path, Pattern, "png", NoOneDrive);

            Assert.Single(result.Items);
            Assert.Equal(OutputPathFinding.NotConfigured, result.Items[0].Finding);
        }

        [Fact]
        public void UncFolder_IsANetworkFolder()
        {
            Assert.True(OutputPathCheck.IsNetworkPath(@"\\server\share\Screenshots"));
            Assert.False(OutputPathCheck.IsNetworkPath(_folder));
        }

        [Fact]
        public void FolderInOneDrive_IsReported()
        {
            string oneDrive = Path.GetDirectoryName(_folder);

            var result = OutputPathCheck.Run(_folder, Pattern, "png", new[] { oneDrive });

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.OneDrive && i.Severity == OutputPathSeverity.Info);
            // Only information: the folder still works
            Assert.Equal(OutputPathFinding.Ok, result.Items.First().Finding);
        }

        [Fact]
        public void OneDrive_MatchesWholeFolderNamesOnly()
        {
            Assert.True(OutputPathCheck.IsInOneDrive(@"C:\Users\a\OneDrive\Pictures", new[] { @"C:\Users\a\OneDrive" }));
            Assert.False(OutputPathCheck.IsInOneDrive(@"C:\Users\a\OneDriveBackup", new[] { @"C:\Users\a\OneDrive" }));
        }

        [Theory]
        [InlineData("capture <today>")]
        [InlineData("capture: today")]
        [InlineData("capture|today")]
        public void PatternWithInvalidCharacters_IsAnInvalidFilename(string pattern)
        {
            // Greenshot fills in the variables safely, but keeps the other characters of the pattern
            var result = OutputPathCheck.Run(_folder, pattern, "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.InvalidFilename && i.Severity == OutputPathSeverity.Error);
        }

        [Fact]
        public void PatternWithSubfolder_IsInformation()
        {
            var result = OutputPathCheck.Run(_folder, @"${payload.time:yyyy}\capture", "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.PatternCreatesFolders);
        }

        [Fact]
        public void VeryLongPath_IsTooLong()
        {
            var result = OutputPathCheck.Run(_folder, new string('x', 300), "png", NoOneDrive);

            var tooLong = result.Items.FirstOrDefault(i => i.Finding == OutputPathFinding.TooLong);
            Assert.NotNull(tooLong);
            Assert.True(int.Parse(tooLong.Detail) > OutputPathCheck.MaxPathLength);
        }

        [Fact]
        public void TryWrite_InMissingFolder_ReturnsTheError()
        {
            Assert.NotNull(OutputPathCheck.TryWrite(Path.Combine(_folder, "missing")));
            Assert.Null(OutputPathCheck.TryWrite(_folder));
        }
    }
}
