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
using Greenshot.SelfService;
using Xunit;

namespace Greenshot.Tests.SelfService
{
    /// <summary>
    /// The check of the screenshot folder in Self-Service: each reason why screenshots don't end up where the user expects them
    /// </summary>
    public sealed class OutputPathCheckTests : IDisposable
    {
        private const string Pattern = "greenshot ${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}";
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
        public void MissingFolder_IsAnError()
        {
            var result = OutputPathCheck.Run(Path.Combine(_folder, "missing"), Pattern, "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.Missing && i.Severity == OutputPathSeverity.Error);
            Assert.DoesNotContain(result.Items, i => i.Finding == OutputPathFinding.Ok);
        }

        [Fact]
        public void FolderWithVariables_IsAnError_BecauseGreenshotResetsIt()
        {
            var result = OutputPathCheck.Run(Path.Combine(_folder, "${YYYY}"), Pattern, "png", NoOneDrive);

            Assert.Contains(result.Items, i => i.Finding == OutputPathFinding.ContainsVariables);
            Assert.DoesNotContain("${YYYY}", result.ExpandedPath);
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
            var result = OutputPathCheck.Run(_folder, @"${YYYY}\capture", "png", NoOneDrive);

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
