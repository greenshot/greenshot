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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Drawing;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Recipes;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Drawing.Filters;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Plugin.Zxing;
using Greenshot.Plugin.RecipeEditor.Helpers;
using Greenshot.Plugin.RecipeEditor.ViewModels;
using Xunit;

namespace Greenshot.Tests.Recipes
{
    public class EditorAnnotationImporterTests
    {
        public EditorAnnotationImporterTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static Surface CreateTestSurface(int width = 800, int height = 600)
        {
            var surface = new Surface();
            surface.Image = new Bitmap(width, height);
            return surface;
        }

        [Fact]
        public void ConvertRectangleContainer_MapsPropertiesCorrectly()
        {
            var surface = CreateTestSurface();
            var rect = new RectangleContainer(surface)
            {
                Left = 50,
                Top = 75,
                Width = 200,
                Height = 100
            };
            rect.SetFieldValue(FieldType.LINE_COLOR, Color.Blue);
            rect.SetFieldValue(FieldType.FILL_COLOR, Color.FromArgb(128, 255, 0, 0));
            rect.SetFieldValue(FieldType.LINE_THICKNESS, 4);
            rect.SetFieldValue(FieldType.SHADOW, true);

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(rect);

            Assert.NotNull(dict);
            Assert.Equal("Rectangle", dict["Type"]);
            Assert.Equal("50", dict["OffsetX"]);
            Assert.Equal("75", dict["OffsetY"]);
            Assert.Equal("200", dict["Width"]);
            Assert.Equal("100", dict["Height"]);
            Assert.Equal("None", dict["HorizontalAnchor"]);
            Assert.Equal("None", dict["VerticalAnchor"]);
            Assert.Equal(4, dict["LineThickness"]);
            Assert.Equal("#0000FF", dict["LineColor"]);
            Assert.Equal("#80FF0000", dict["FillColor"]);
            Assert.True((bool)dict["Shadow"]);
        }

        [Fact]
        public void ConvertContainer_NormalizesNegativeDimensions()
        {
            var surface = CreateTestSurface();
            var rect = new RectangleContainer(surface)
            {
                Left = 150,
                Top = 200,
                Width = -100,
                Height = -50
            };

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(rect);

            Assert.NotNull(dict);
            Assert.Equal("50", dict["OffsetX"]);
            Assert.Equal("150", dict["OffsetY"]);
            Assert.Equal("100", dict["Width"]);
            Assert.Equal("50", dict["Height"]);
        }

        [Fact]
        public void ConvertTextContainer_MapsTextAndFontCorrectly()
        {
            var surface = CreateTestSurface();
            var text = new TextContainer(surface)
            {
                Left = 20,
                Top = 30,
                Width = 180,
                Height = 60,
                Text = "Hello Greenshot"
            };
            text.SetFieldValue(FieldType.FONT_FAMILY, "Segoe UI");
            text.SetFieldValue(FieldType.FONT_SIZE, 16.0f);
            text.SetFieldValue(FieldType.FONT_BOLD, true);
            text.SetFieldValue(FieldType.FONT_ITALIC, false);
            text.SetFieldValue(FieldType.TEXT_HORIZONTAL_ALIGNMENT, StringAlignment.Center);

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(text);

            Assert.NotNull(dict);
            Assert.Equal("Text", dict["Type"]);
            Assert.Equal("Hello Greenshot", dict["Text"]);
            Assert.Equal("Segoe UI", dict["FontFamily"]);
            Assert.Equal("Center", dict["TextAlign"]);
            Assert.True((bool)dict["Bold"]);
        }

        [Fact]
        public void ConvertStepLabelContainer_MapsNumberCorrectly()
        {
            var surface = CreateTestSurface();
            var label = new StepLabelContainer(surface)
            {
                Left = 40,
                Top = 40,
                Width = 28,
                Height = 28,
                Number = 5
            };

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(label);

            Assert.NotNull(dict);
            Assert.Equal("StepLabel", dict["Type"]);
            Assert.Equal(5, dict["Number"]);
            Assert.Equal("5", dict["Text"]);
        }

        [Fact]
        public void ConvertEmojiContainer_MapsEmojiCorrectly()
        {
            var surface = CreateTestSurface();
            var emoji = new EmojiContainer(surface)
            {
                Left = 10,
                Top = 10,
                Width = 32,
                Height = 32,
                Emoji = "🚀"
            };

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(emoji);

            Assert.NotNull(dict);
            Assert.Equal("Emoji", dict["Type"]);
            Assert.Equal("🚀", dict["Emoji"]);
        }

        [Fact]
        public void ConvertArrowContainer_MapsArrowHeadsCorrectly()
        {
            var surface = CreateTestSurface();
            var arrow = new ArrowContainer(surface)
            {
                Left = 100,
                Top = 100,
                Width = 200,
                Height = 50
            };
            arrow.SetFieldValue(FieldType.ARROWHEADS, 2); // END_POINT

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(arrow);

            Assert.NotNull(dict);
            Assert.Equal("Arrow", dict["Type"]);
            Assert.Equal("END_POINT", dict["ArrowHeads"]);
        }

        [Fact]
        public void ConvertObfuscateContainer_BlurAndPixelize_MappedCorrectly()
        {
            var surface = CreateTestSurface();
            var obfuscate = new ObfuscateContainer(surface)
            {
                Left = 50,
                Top = 50,
                Width = 100,
                Height = 50
            };
            obfuscate.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.BLUR);

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(obfuscate);
            Assert.NotNull(dict);
            Assert.Equal("Blur", dict["Type"]);

            obfuscate.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.PIXELIZE);
            dict = EditorAnnotationImporter.ConvertContainerToAnnotation(obfuscate);
            Assert.NotNull(dict);
            Assert.Equal("Pixelize", dict["Type"]);
        }

        [Fact]
        public void ConvertHighlightContainer_HighlightAndMagnify_MappedCorrectly()
        {
            var surface = CreateTestSurface();
            var highlight = new HighlightContainer(surface)
            {
                Left = 30,
                Top = 30,
                Width = 120,
                Height = 40
            };
            highlight.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT);

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(highlight);
            Assert.NotNull(dict);
            Assert.Equal("Highlight", dict["Type"]);

            highlight.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.MAGNIFICATION);
            dict = EditorAnnotationImporter.ConvertContainerToAnnotation(highlight);
            Assert.NotNull(dict);
            Assert.Equal("Magnify", dict["Type"]);
        }

        [Fact]
        public void ConvertBarcodeContainer_ExtractsModelDetails()
        {
            var surface = CreateTestSurface();
            var model = new ZxingModel
            {
                RawText = "https://greenshot.org",
                ForeColor = Color.DarkBlue,
                BackColor = Color.LightYellow,
                Margin = 2,
                RoundedDots = true,
                FormatIndex = 0 // QR Code
            };
            var barcode = new BarcodeContainer(surface, model)
            {
                Left = 10,
                Top = 10,
                Width = 150,
                Height = 150
            };

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(barcode);

            Assert.NotNull(dict);
            Assert.Equal("QRCode", dict["Type"]);
            Assert.Equal("https://greenshot.org", dict["Text"]);
            Assert.Equal("#00008B", dict["ForeColor"]);
            Assert.Equal("#FFFFE0", dict["BackColor"]);
            Assert.Equal(2, dict["Margin"]);
            Assert.True((bool)dict["RoundedDots"]);
        }

        [Fact]
        public void ImportFromSurface_ImportsAllElementsInOrder()
        {
            var surface = CreateTestSurface();
            surface.Elements.Add(new RectangleContainer(surface) { Left = 10, Top = 10, Width = 50, Height = 50 });
            surface.Elements.Add(new EllipseContainer(surface) { Left = 70, Top = 10, Width = 60, Height = 60 });
            surface.Elements.Add(new LineContainer(surface) { Left = 150, Top = 10, Width = 100, Height = 10 });

            var annotations = EditorAnnotationImporter.ImportFromSurface(surface);

            Assert.Equal(3, annotations.Count);
            Assert.Equal("Rectangle", annotations[0]["Type"]);
            Assert.Equal("Ellipse", annotations[1]["Type"]);
            Assert.Equal("Line", annotations[2]["Type"]);
        }

        [Fact]
        public void StepNodeViewModel_ImportAnnotations_AppendAndReplaceWorkCorrectly()
        {
            var config = new RecipeNodeConfig
            {
                Id = "step1",
                StepType = "Annotation",
                Parameters = new Dictionary<string, object>()
            };
            var nodeVm = new StepNodeViewModel(config, new System.Windows.Point(0, 0));
            nodeVm.AddAnnotation("Rectangle");
            Assert.Single(nodeVm.Annotations);

            var imported = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["Type"] = "Text", ["Text"] = "Imported", ["OffsetX"] = "10", ["OffsetY"] = "10", ["Width"] = "100", ["Height"] = "30" },
                new Dictionary<string, object> { ["Type"] = "Arrow", ["OffsetX"] = "20", ["OffsetY"] = "20", ["Width"] = "80", ["Height"] = "40" }
            };

            // Test Append (replaceExisting = false)
            nodeVm.ImportAnnotations(imported, replaceExisting: false);
            Assert.Equal(3, nodeVm.Annotations.Count);
            Assert.Equal("Rectangle", nodeVm.Annotations[0].Type);
            Assert.Equal("Text", nodeVm.Annotations[1].Type);
            Assert.Equal("Arrow", nodeVm.Annotations[2].Type);

            // Test Replace (replaceExisting = true)
            nodeVm.ImportAnnotations(imported, replaceExisting: true);
            Assert.Equal(2, nodeVm.Annotations.Count);
            Assert.Equal("Text", nodeVm.Annotations[0].Type);
            Assert.Equal("Arrow", nodeVm.Annotations[1].Type);
        }

        [Fact]
        public void LoadSurfaceFromGreenshotFile_RoundtripImportTest()
        {
            var surface = CreateTestSurface();
            surface.Elements.Add(new RectangleContainer(surface) { Left = 12, Top = 34, Width = 100, Height = 80 });
            surface.Elements.Add(new TextContainer(surface) { Left = 50, Top = 60, Width = 150, Height = 40, Text = "Test Annotation" });

            string tempFile = Path.Combine(Path.GetTempPath(), $"greenshot_import_test_{Guid.NewGuid():N}.greenshot");
            try
            {
                var handler = new GreenshotFileFormatHandler();
                using (var fs = File.Create(tempFile))
                {
                    Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, fs, ".greenshot", surface));
                }

                // Now load it via EditorAnnotationImporter
                var loadedSurface = EditorAnnotationImporter.LoadSurfaceFromGreenshotFile(tempFile);
                Assert.NotNull(loadedSurface);
                Assert.Equal(2, loadedSurface.Elements.Count);

                var imported = EditorAnnotationImporter.ImportFromSurface(loadedSurface);
                Assert.Equal(2, imported.Count);
                Assert.Equal("Rectangle", imported[0]["Type"]);
                Assert.Equal("12", imported[0]["OffsetX"]);
                Assert.Equal("34", imported[0]["OffsetY"]);
                Assert.Equal("Text", imported[1]["Type"]);
                Assert.Equal("Test Annotation", imported[1]["Text"]);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
