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
        public void EverythingADocumentFileHolds_IsWrittenTheSame_AfterReadingIt()
        {
            // Also the check for the editor compiled ahead (native AOT): the JSON is written and read by generated code,
            // which only knows the types it was generated for. What it doesn't know fails here, or is missing in the text.
            string folder = TempFolder();
            try
            {
                string first = Path.Combine(folder, "first.greenshot"), second = Path.Combine(folder, "second.greenshot");
                DocumentFiles.Save(Samples.Rich(), first);
                var opened = DocumentFiles.Open(first);
                DocumentFiles.Save(opened, second);
                if (Environment.GetEnvironmentVariable("GREENSHOT_TEST_OUTPUT") is { Length: > 0 } output)
                {
                    File.Copy(first, Path.Combine(output, "rich.greenshot"), true);
                }

                static string Entry(string file, string name)
                {
                    using var zip = System.IO.Compression.ZipFile.OpenRead(file);
                    using var reader = new StreamReader(zip.GetEntry(name).Open());
                    return reader.ReadToEnd();
                }

                string document = Entry(first, "document.json");
                Assert.Equal(document, Entry(second, "document.json"));
                Assert.Equal(Entry(first, "analysis.json"), Entry(second, "analysis.json"));
                foreach (string expected in new[]
                         {
                             "\"type\": \"frame\"", "\"placement\": \"outside\"", "\"type\": \"plugin\"", "\"format\":\"QR_CODE\"", "\"heads\": \"both\"", "\"mode\": \"magnify\"",
                             "\"format\":\"upperLetters\"", "\"greenshot.test\"", "\"anchor\"", "\"underline\": true"
                         })
                {
                    Assert.Contains(expected, document);
                }

                Assert.Contains("\"engine\": \"test\"", Entry(first, "analysis.json"));
                string metadata = Entry(second, "metadata.json");
                Assert.Contains("\"title\": \"A window\"", metadata);
                Assert.Contains("\"mode\": \"window\"", metadata);
                Assert.Equal(new StepNumbering(3, StepFormat.UpperLetters), StepNumbering.Of(opened.Groups[0]));
                Assert.Equal("Hello", opened.Analysis.Values.Single().Ocr.Lines[0].Words[0].Text);
                // What was read is complete enough to draw
                using var drawn = Greenshot.Editor.Standalone.Rendering.DocumentRenderer.Render(opened);
                Assert.True(drawn.Width >= 640);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Fact]
        public void ValuesWhichAreMissingInTheFile_GetTheirDefaults()
        {
            // The trap of the generated JSON code: a value which has its default only at the property would be 0 or null
            const string id = "\"id\": \"01a12eae-0000-7000-8000-000000000001\", \"area\": {\"x\": 1, \"y\": 2, \"width\": 30, \"height\": 40}";
            var elements = GreenshotPackage.ReadElements("[" +
                "{\"type\": \"rectangle\", " + id + ", \"label\": {\"text\": \"a\"}}," +
                "{\"type\": \"line\", \"id\": \"01a12eae-0000-7000-8000-000000000002\", \"start\": {\"x\": 1, \"y\": 2}, \"end\": {\"x\": 3, \"y\": 4}, \"label\": {\"text\": \"b\"}}," +
                "{\"type\": \"highlight\", " + id + ", \"mode\": \"magnify\"}," +
                "{\"type\": \"cutMark\", " + id + ", \"horizontal\": true, \"seed\": 3}," +
                "{\"type\": \"cutMark\", " + id + ", \"shape\": {\"style\": \"wave\"}}," +
                "{\"type\": \"frame\", " + id + ", \"seed\": 3}," +
                "{\"type\": \"plugin\", " + id + ", \"kind\": \"x\"}," +
                "{\"type\": \"highlight\", " + id + ", \"mode\": \"text\", \"color\": \"#FF000080\", \"magnification\": 4}]");
            var rectangle = Assert.IsType<RectangleElement>(elements[0]);
            Assert.Equal(new RectD(1, 2, 30, 40), rectangle.Area);
            Assert.Same(Style.Empty, rectangle.Style);
            Assert.Equal(new BoxLabel("a"), rectangle.Label);
            Assert.Equal(LabelPlace.Center, rectangle.Label.Y);
            Assert.Equal(new LineLabel("b"), Assert.IsType<LineElement>(elements[1]).Label);
            Assert.Equal(new HighlightElement(rectangle.Id, rectangle.Area, HighlightMode.Magnify), elements[2]);
            Assert.Equal(new CutMarkElement(rectangle.Id, rectangle.Area, true, 3), elements[3]);
            Assert.Equal(new EdgeShape(EdgeStyle.Wave) { Height = 12, Range = 20 }, Assert.IsType<CutMarkElement>(elements[4]).Shape);
            Assert.Equal(new EdgeShape(EdgeStyle.Torn), Assert.IsType<FrameElement>(elements[5]).Shape);
            Assert.Empty(Assert.IsType<PluginElement>(elements[6]).Resources);
            var highlight = Assert.IsType<HighlightElement>(elements[7]);
            Assert.Equal((new Rgba(255, 0, 0, 128), 4, 3, 0.9), (highlight.Color, highlight.Magnification, highlight.BlurRadius, highlight.Brightness));

            // A layer without "visible" is visible
            string folder = TempFolder();
            try
            {
                string file = Path.Combine(folder, "layers.greenshot");
                DocumentFiles.Save(Samples.Page(Samples.Green), file);
                using (var zip = System.IO.Compression.ZipFile.Open(file, System.IO.Compression.ZipArchiveMode.Update))
                {
                    var entry = zip.GetEntry("document.json");
                    string json;
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        json = reader.ReadToEnd();
                    }

                    Assert.Contains("\"visible\": true", json);
                    entry.Delete();
                    using var writer = new StreamWriter(zip.CreateEntry("document.json").Open());
                    writer.Write(System.Text.RegularExpressions.Regex.Replace(json, ",\\s*\"visible\": true", string.Empty));
                }

                var opened = DocumentFiles.Open(file);
                Assert.All(opened.Layers, layer => Assert.True(layer.Visible));
                Assert.Equal(Rgba.Transparent, opened.Canvas.Background);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
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
