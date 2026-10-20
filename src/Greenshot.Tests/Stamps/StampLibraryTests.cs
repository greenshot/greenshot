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
using Greenshot.Plugin.Stamps.Library;
using Xunit;

namespace Greenshot.Tests.Stamps
{
    /// <summary>
    /// The stamps folder: files, names and tags in stamps.json, search, import and delete. No image is decoded here.
    /// </summary>
    public sealed class StampLibraryTests : IDisposable
    {
        private static readonly string[] Extensions = { ".png", ".svg" };
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"greenshot_stamps_test_{Guid.NewGuid():N}");

        public StampLibraryTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private string CreateFile(string relativePath)
        {
            string path = Path.Combine(_directory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            return path;
        }

        private StampLibrary Load()
        {
            var library = new StampLibrary(_directory, Extensions);
            library.Load();
            return library;
        }

        [Fact]
        public void Load_ShowsTheImageFiles_AlsoInSubFolders_NamedAfterTheFile()
        {
            CreateFile("Check.png");
            CreateFile(@"Arrows\Left.SVG");
            CreateFile("notes.txt");

            var library = Load();

            Assert.Equal(new[] { "Check", "Left" }, library.Stamps.Select(stamp => stamp.Name));
            Assert.Equal("Arrows/Left.SVG", library.Stamps[1].Key);
            Assert.Empty(library.Stamps[0].Tags);
        }

        [Fact]
        public void Load_WithoutTheFolder_HasNoStamps()
        {
            var library = new StampLibrary(Path.Combine(_directory, "missing"), Extensions);

            library.Load();

            Assert.Empty(library.Stamps);
        }

        [Fact]
        public void Update_IsKeptInStampsJson_ForTheNextLoad()
        {
            CreateFile("check.png");
            var library = Load();

            library.Update(library.Stamps[0], " Done ", "green, ok;Green ,");

            var stamp = Assert.Single(Load().Stamps);
            Assert.Equal("Done", stamp.Name);
            Assert.Equal(new[] { "green", "ok" }, stamp.Tags);
            Assert.True(File.Exists(Path.Combine(_directory, StampLibrary.IndexFileName)));
        }

        [Fact]
        public void Update_WithAnEmptyName_UsesTheFileName()
        {
            CreateFile("check.png");
            var library = Load();

            library.Update(library.Stamps[0], "  ", null);

            Assert.Equal("check", Load().Stamps[0].Name);
        }

        [Fact]
        public void Update_KeepsWhatAnotherUserSavedMeanwhile()
        {
            CreateFile("one.png");
            CreateFile("two.png");
            var mine = Load();
            var theirs = Load();

            theirs.Update(theirs.Stamps.Single(stamp => stamp.Name == "two"), "two", "theirs");
            mine.Update(mine.Stamps.Single(stamp => stamp.Name == "one"), "one", "mine");

            var stamps = Load().Stamps;
            Assert.Equal(new[] { "mine" }, stamps.Single(stamp => stamp.Name == "one").Tags);
            Assert.Equal(new[] { "theirs" }, stamps.Single(stamp => stamp.Name == "two").Tags);
        }

        [Fact]
        public void Load_WithABrokenStampsJson_ShowsTheFilesWithoutTags()
        {
            CreateFile("check.png");
            File.WriteAllText(Path.Combine(_directory, StampLibrary.IndexFileName), "{ not json");

            Assert.Equal("check", Assert.Single(Load().Stamps).Name);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("green", true)]
        [InlineData("GRE", true)]
        [InlineData("done ok", true)]
        [InlineData("check", true)]
        [InlineData("done red", false)]
        public void Matches_EveryWordInTheNameATagOrTheFileName(string search, bool matches)
        {
            CreateFile("check.png");
            var library = Load();
            library.Update(library.Stamps[0], "Done", "green, ok");

            Assert.Equal(matches, library.Stamps[0].Matches(search));
        }

        [Fact]
        public void Import_CopiesTheFile_WithAFreeName()
        {
            CreateFile("check.png");
            var library = Load();
            string source = Path.Combine(Path.GetTempPath(), $"greenshot_stamps_source_{Guid.NewGuid():N}");
            Directory.CreateDirectory(source);
            try
            {
                string file = Path.Combine(source, "check.PNG");
                File.WriteAllBytes(file, new byte[] { 4 });

                var stamp = library.Import(file);

                Assert.Equal("check (2)", stamp.Name);
                Assert.Equal(Path.Combine(_directory, "check (2).png"), stamp.FullPath);
                Assert.Equal(2, library.Stamps.Count);
                Assert.True(File.Exists(file));
            }
            finally
            {
                Directory.Delete(source, true);
            }
        }

        [Fact]
        public void Import_OfAStampFile_ReturnsTheStamp()
        {
            string file = CreateFile("check.png");
            var library = Load();

            Assert.Same(library.Stamps[0], library.Import(file));
            Assert.Single(library.Stamps);
        }

        [Fact]
        public void Import_OfAnotherFileType_IsRefused()
        {
            var library = Load();

            Assert.Throws<ArgumentException>(() => library.Import(CreateFile("notes.txt")));
        }

        [Fact]
        public void Add_WritesTheContent_WithAFileNameOfTheName()
        {
            var library = Load();

            var stamp = library.Add("a:b", ".png", new byte[] { 7, 8 });

            Assert.Equal(Path.Combine(_directory, "a_b.png"), stamp.FullPath);
            Assert.Equal(new byte[] { 7, 8 }, File.ReadAllBytes(stamp.FullPath));
        }

        [Fact]
        public void Delete_RemovesTheFileAndItsEntry()
        {
            string file = CreateFile("check.png");
            var library = Load();
            library.Update(library.Stamps[0], "Done", "ok");

            library.Delete(library.Stamps[0]);

            Assert.Empty(library.Stamps);
            Assert.False(File.Exists(file));
            Assert.DoesNotContain("check.png", File.ReadAllText(Path.Combine(_directory, StampLibrary.IndexFileName)));
        }

        [Fact]
        public void Category_IsTheSubFolder_EmptyInTheStampsFolder()
        {
            CreateFile("check.png");
            CreateFile(@"Arrows\Red\left.png");
            CreateFile(@"Shapes\star.png");

            var library = Load();

            Assert.Equal("", library.Stamps.Single(stamp => stamp.Name == "check").Category);
            Assert.Equal("Arrows/Red", library.Stamps.Single(stamp => stamp.Name == "left").Category);
            Assert.Equal(new[] { "Arrows/Red", "Shapes" }, library.Categories);
        }

        [Fact]
        public void Matches_TheCategory()
        {
            CreateFile(@"Arrows\left.png");

            var stamp = Load().Stamps[0];

            Assert.True(stamp.Matches("arrows"));
            Assert.False(stamp.Matches("shapes"));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("  ", "")]
        [InlineData("Arrows", "Arrows")]
        [InlineData(@" Arrows \ Red/ ", "Arrows/Red")]
        [InlineData("../a:b/./c.", "a_b/c")]
        public void NormalizeCategory_GivesASafeFolderPath(string category, string expected)
        {
            Assert.Equal(expected, StampLibrary.NormalizeCategory(category));
        }

        [Fact]
        public void Import_IntoACategory_CopiesIntoItsFolder()
        {
            string source = Path.Combine(_directory, "..", $"greenshot_stamp_{Guid.NewGuid():N}.png");
            File.WriteAllBytes(source, new byte[] { 1 });
            try
            {
                var library = Load();

                var stamp = library.Import(source, "Arrows/Red");

                Assert.Equal("Arrows/Red", stamp.Category);
                Assert.Equal(Path.Combine(_directory, "Arrows", "Red", Path.GetFileName(source)), stamp.FullPath);
                Assert.True(File.Exists(stamp.FullPath));
            }
            finally
            {
                File.Delete(source);
            }
        }

        [Fact]
        public void Add_IntoACategory_WritesIntoItsFolder()
        {
            var library = Load();

            var stamp = library.Add("star", ".svg", new byte[] { 1 }, "Shapes");

            Assert.Equal(Path.Combine(_directory, "Shapes", "star.svg"), stamp.FullPath);
            Assert.Equal("Shapes/star.svg", stamp.Key);
        }

        [Fact]
        public void Move_MovesTheFile_AndKeepsTheNameAndTags()
        {
            string file = CreateFile("check.png");
            var library = Load();
            library.Update(library.Stamps[0], "Done", "ok");

            library.Move(library.Stamps[0], "Marks");

            Assert.False(File.Exists(file));
            var stamp = Assert.Single(Load().Stamps);
            Assert.Equal("Marks", stamp.Category);
            Assert.Equal("Done", stamp.Name);
            Assert.Equal(new[] { "ok" }, stamp.Tags);
        }

        [Fact]
        public void Move_WhenTheNameIsTaken_GivesAFreeName()
        {
            CreateFile("check.png");
            CreateFile(@"Marks\check.png");
            var library = Load();

            library.Move(library.Stamps.Single(stamp => stamp.Category == ""), "Marks");

            Assert.Equal(new[] { "Marks/check (2).png", "Marks/check.png" }, Load().Stamps.Select(stamp => stamp.Key).OrderBy(key => key, StringComparer.Ordinal));
        }

        [Fact]
        public void Move_BackToNoCategory_PutsItIntoTheStampsFolder()
        {
            CreateFile(@"Marks\check.png");
            var library = Load();

            library.Move(library.Stamps[0], "");

            Assert.Equal("check.png", Assert.Single(Load().Stamps).Key);
        }
    }
}
