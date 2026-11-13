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
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Files;
using Greenshot.Editor.Standalone.Rendering;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Stamps;
using Xunit;

namespace Greenshot.Editor.Standalone.Tests.Misc
{
    public class SmallPartsTests
    {
        private static string TempFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "greenshot-editor-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        [Fact]
        public void LoggedText_StaysOnItsLine()
        {
            Assert.Equal("a\\r\\nb\\tc\\u0000d\\u2028e", EditorLog.Escape("a\r\nb\tc\0d" + (char)0x2028 + "e"));
            Assert.Equal("plain", EditorLog.Escape("plain"));
            Assert.Equal(string.Empty, EditorLog.Escape(null));
        }

        [Fact]
        public void AValueOfGreenshotsIni_IsReadFromItsSection()
        {
            string folder = TempFolder();
            try
            {
                string ini = Path.Combine(folder, "greenshot.ini");
                File.WriteAllText(ini, "; comment\n[Other]\nLanguage=fr-FR\n[Core]\n# also a comment\nLanguage = de-DE \nOutputFilePath=C:\\a=b\n");

                Assert.Equal("de-DE", EditorSettings.ReadValue(ini, "core", "language"));
                Assert.Equal("C:\\a=b", EditorSettings.ReadValue(ini, "Core", "OutputFilePath"));
                Assert.Null(EditorSettings.ReadValue(ini, "Core", "Missing"));
                Assert.Null(EditorSettings.ReadValue(Path.Combine(folder, "none.ini"), "Core", "Language"));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Fact]
        public void TheSettings_AreKeptInTheirOwnFile()
        {
            EditorSettings.Current.LastDirectory = "somewhere";
            EditorSettings.AddRecentEmoji("\U0001F600");
            EditorSettings.Save();
            EditorSettings.Reset();

            Assert.Equal("somewhere", EditorSettings.Current.LastDirectory);
            Assert.Equal("\U0001F600", EditorSettings.RecentEmojis.First());
            Assert.True(File.Exists(Path.Combine(EditorPaths.SettingsDirectory, EditorPaths.SettingsFileName)));
        }

        [Fact]
        public void TheTexts_ComeFromTheLanguagePacks()
        {
            try
            {
                EditorTexts.SetLanguage("de-DE");
                Assert.Equal("Datei", EditorTexts.Text.File);
                EditorTexts.SetLanguage("en-US");
                Assert.Equal("File", EditorTexts.Text.File);
                // A language without a pack falls back to English
                EditorTexts.SetLanguage("xx-XX");
                Assert.Equal("File", EditorTexts.Text.File);
            }
            finally
            {
                EditorTexts.SetLanguage("en-US");
            }
        }

        [Fact]
        public void TheStamps_AreTheImagesOfTheFolder_WithTheNamesOfItsIndex()
        {
            string folder = TempFolder();
            try
            {
                Directory.CreateDirectory(Path.Combine(folder, "Arrows"));
                byte[] png = Samples.Png(8, 8, Samples.Red).Data.ToArray();
                File.WriteAllBytes(Path.Combine(folder, "Arrows", "up.png"), png);
                File.WriteAllBytes(Path.Combine(folder, "approved.png"), png);
                File.WriteAllText(Path.Combine(folder, "notes.txt"), "not a stamp");
                File.WriteAllText(Path.Combine(folder, StampLibrary.IndexFileName), "{\"stamps\":{\"Arrows/up.png\":{\"name\":\"Upwards\",\"tags\":[\"north\"]}}}");
                var library = new StampLibrary(folder);

                library.Load();

                Assert.Equal(new[] { "Arrows/up.png", "approved.png" }, library.Stamps.Select(stamp => stamp.Key).OrderBy(key => key, StringComparer.Ordinal));
                var up = library.Stamps.Single(stamp => stamp.Key == "Arrows/up.png");
                Assert.Equal("Upwards", up.Name);
                Assert.Equal("Arrows", up.Category);
                Assert.True(up.Matches("NORTH"));
                Assert.False(up.Matches("south"));

                var added = library.Add(png, "My stamp");
                Assert.True(File.Exists(added.FullPath));
                Assert.Equal("My stamp", added.Name);
                Assert.Contains(library.Stamps, stamp => stamp.Key == added.Key);
                var again = library.Add(png, "My stamp");
                Assert.NotEqual(added.FullPath, again.FullPath);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Fact]
        public void OnlyTheAreaOfAChangedElement_IsDrawnAgain()
        {
            var box = new RectangleElement(Guid.NewGuid(), new RectD(20, 20, 30, 20)) { Style = Samples.Plain };
            var other = new RectangleElement(Guid.NewGuid(), new RectD(120, 60, 30, 20)) { Style = Samples.Plain };
            var before = Samples.Page(Samples.White, box, other);

            Assert.Equal(default(RectD), DocumentDiff.Changed(before, before));
            Assert.Equal(default(RectD), DocumentDiff.Changed(before, before with { Metadata = before.Metadata }));

            var moved = DocumentEdits.Replace(before, new RectangleElement(box.Id, new RectD(40, 20, 30, 20)) { Style = Samples.Plain });
            var area = Assert.IsType<RectD>(DocumentDiff.Changed(before, moved));
            Assert.True(area.X <= 20 && area.Y <= 20 && area.Right >= 70 && area.Bottom >= 40, "Where it was and where it is now");
            Assert.True(area.Right < 120, "The other element is not drawn again");

            // Another order, another canvas or a hidden layer change all of the canvas
            var swapped = before with { Layers = new[] { before.Layers[0], before.Layers[1] with { Elements = new Element[] { other, box } } } };
            Assert.Null(DocumentDiff.Changed(before, swapped));
            Assert.Null(DocumentDiff.Changed(before, before with { Canvas = before.Canvas with { Background = Samples.Red } }));
            Assert.Null(DocumentDiff.Changed(before, before with { Layers = new[] { before.Layers[0], before.Layers[1] with { Visible = false } } }));
            Assert.Null(DocumentDiff.Changed(null, before));
        }

        [Fact]
        public void ADocument_SurvivesSavingAndOpening_AndAnImageOpensAsACapture()
        {
            string folder = TempFolder();
            try
            {
                var document = Samples.Gallery();
                string file = Path.Combine(folder, "gallery.greenshot");
                DocumentFiles.Save(document, file);
                var opened = DocumentFiles.Open(file);
                Assert.Equal(document.Layers[1].Elements.Select(element => element.GetType()), opened.Layers[1].Elements.Select(element => element.GetType()));
                Assert.Equal(document.Groups.Count, opened.Groups.Count);

                string png = Path.Combine(folder, "gallery.png");
                DocumentFiles.Save(document, png);
                var image = DocumentFiles.Open(png);
                var capture = Assert.IsType<ImageElement>(Assert.Single(image.Layers.SelectMany(layer => layer.Elements)));
                Assert.Equal(new RectD(0, 0, 640, 420), capture.Bounds);

                Assert.Equal("a_b", DocumentFiles.FileNameOf("a" + Path.GetInvalidFileNameChars()[0] + "b"));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
