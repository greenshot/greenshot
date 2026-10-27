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


#nullable disable

using System.Collections.Generic;
using System.IO;
using Greenshot.Linux.Editor;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Serialization;
using Greenshot.Linux.RecipeEditor;
using Greenshot.Linux.RecipeEditor.Helpers;
using Greenshot.Linux.RecipeEditor.ViewModels;
using Greenshot.Linux.Recipes;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Recipes
{
    /// <summary>
    /// The elements of an image editor or a .greenshot file as annotations of an Annotation step
    /// </summary>
    [Collection(TestCollections.RecipeManager)]
    public class EditorAnnotationImporterTests
    {
        public EditorAnnotationImporterTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static EditorSurface CreateTestSurface(int width = 800, int height = 600) => new(new SKBitmap(width, height));

        private static T Element<T>(float left, float top, float width, float height) where T : DrawableElement, new()
        {
            var element = new T();
            element.Style = element.CreateDefaultStyle();
            element.Bounds = SKRect.Create(left, top, width, height);
            return element;
        }

        [Fact]
        public void ConvertRectangle_MapsPropertiesCorrectly()
        {
            var rect = Element<RectangleElement>(50, 75, 200, 100);
            rect.Style.LineColor = SKColors.Blue;
            rect.Style.FillColor = new SKColor(255, 0, 0, 128);
            rect.Style.LineThickness = 4;
            rect.Style.Shadow = true;

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(rect);

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
        public void Convert_NormalizesNegativeDimensions()
        {
            var rect = Element<RectangleElement>(150, 200, -100, -50);

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(rect);

            Assert.Equal("50", dict["OffsetX"]);
            Assert.Equal("150", dict["OffsetY"]);
            Assert.Equal("100", dict["Width"]);
            Assert.Equal("50", dict["Height"]);
        }

        [Fact]
        public void ConvertText_MapsTextAndFont()
        {
            var text = Element<TextElement>(10, 20, 200, 40);
            text.Text = "Hello Greenshot";
            text.Style.FontFamily = "DejaVu Sans";
            text.Style.FontSize = 18;
            text.Style.FontBold = true;
            text.Style.TextHorizontalAlignment = TextHorizontalAlignment.Right;

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(text);

            Assert.Equal("Text", dict["Type"]);
            Assert.Equal("Hello Greenshot", dict["Text"]);
            Assert.Equal("DejaVu Sans", dict["FontFamily"]);
            Assert.Equal(18.0, dict["FontSize"]);
            Assert.True((bool)dict["Bold"]);
            Assert.False((bool)dict["Italic"]);
            Assert.Equal("Right", dict["TextAlign"]);
        }

        [Fact]
        public void ConvertSpeechbubble_HasTheTail()
        {
            var bubble = Element<SpeechBubbleElement>(100, 100, 100, 50);
            bubble.Text = "Look";
            bubble.Target = new SKPoint(60, 220);

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(bubble);

            Assert.Equal("Speechbubble", dict["Type"]);
            Assert.Equal("BottomLeft", dict["TailDirection"]);
            Assert.Equal("-40", dict["TailOffsetX"]);
            Assert.Equal("120", dict["TailOffsetY"]);
        }

        [Fact]
        public void ConvertStepLabels_AreNumberedInOrder()
        {
            var surface = CreateTestSurface();
            var first = Element<StepLabelElement>(10, 10, 30, 30);
            var second = Element<StepLabelElement>(60, 10, 30, 30);
            surface.Add(first, select: false);
            surface.Add(second, select: false);

            var annotations = EditorAnnotationImporter.ImportFromSurface(surface);

            Assert.Equal(new object[] { 1, 2 }, annotations.Select(a => a["Number"]));
            Assert.Equal("2", annotations[1]["Text"]);
        }

        [Theory]
        [InlineData(ArrowHeads.None, "NONE")]
        [InlineData(ArrowHeads.Start, "START_POINT")]
        [InlineData(ArrowHeads.End, "END_POINT")]
        [InlineData(ArrowHeads.Both, "BOTH")]
        public void ConvertArrow_MapsArrowHeads(ArrowHeads heads, string expected)
        {
            var arrow = Element<ArrowElement>(10, 10, 100, 50);
            arrow.Style.ArrowHeads = heads;

            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(arrow);

            Assert.Equal("Arrow", dict["Type"]);
            Assert.Equal(expected, dict["ArrowHeads"]);
        }

        [Fact]
        public void ConvertObfuscate_BlurAndPixelize()
        {
            var obfuscate = Element<ObfuscateElement>(10, 10, 100, 50);
            obfuscate.Style.ObfuscateFilter = ObfuscateFilter.Blur;
            obfuscate.Style.BlurRadius = 7;
            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(obfuscate);
            Assert.Equal("Blur", dict["Type"]);
            Assert.Equal(7, dict["BlurRadius"]);

            obfuscate.Style.ObfuscateFilter = ObfuscateFilter.Pixelize;
            obfuscate.Style.PixelSize = 9;
            dict = EditorAnnotationImporter.ConvertContainerToAnnotation(obfuscate);
            Assert.Equal("Pixelize", dict["Type"]);
            Assert.Equal(9, dict["PixelSize"]);
        }

        [Fact]
        public void ConvertHighlight_HighlightAndMagnify()
        {
            var highlight = Element<HighlightElement>(10, 10, 100, 50);
            highlight.Style.HighlightFilter = HighlightFilter.TextHighlight;
            highlight.Style.FillColor = SKColors.Yellow;
            var dict = EditorAnnotationImporter.ConvertContainerToAnnotation(highlight);
            Assert.Equal("Highlight", dict["Type"]);
            Assert.Equal("#FFFF00", dict["FillColor"]);

            highlight.Style.HighlightFilter = HighlightFilter.Magnification;
            highlight.Style.MagnificationFactor = 3;
            dict = EditorAnnotationImporter.ConvertContainerToAnnotation(highlight);
            Assert.Equal("Magnify", dict["Type"]);
            Assert.Equal(3, dict["MagnificationFactor"]);
        }

        [Fact]
        public void AnalyzeImage_EmbedsOrSavesThePng()
        {
            using var bitmap = new SKBitmap(4, 3);
            var surface = CreateTestSurface();
            surface.Add(ImageElement.FromBitmap(bitmap, new SKPoint(5, 5)), select: false);

            var item = Assert.Single(EditorAnnotationImporter.AnalyzeSurfaceElements(surface));
            Assert.Equal("Image", item.Type);
            Assert.True(item.CanSaveAsFile);
            Assert.NotEmpty(item.RawData);

            var embedded = Assert.Single(EditorAnnotationImporter.ProcessImport([item]));
            Assert.Equal(Convert.ToBase64String(item.RawData), embedded["ImageData"]);

            string folder = Path.Combine(Path.GetTempPath(), "gs-import-" + Guid.NewGuid().ToString("N"));
            try
            {
                item.StorageMode = AssetStorageMode.SaveToFile;
                var saved = Assert.Single(EditorAnnotationImporter.ProcessImport([item], folder, "logo"));
                Assert.False(saved.ContainsKey("ImageData"));
                Assert.True(File.Exists((string)saved["FilePath"]));
                Assert.StartsWith("logo_image_1", Path.GetFileName((string)saved["FilePath"]));
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Fact]
        public void ImportFromSurface_ImportsAllElementsInOrder()
        {
            var surface = CreateTestSurface();
            surface.Add(Element<RectangleElement>(10, 10, 50, 50), select: false);
            surface.Add(Element<EllipseElement>(70, 10, 60, 60), select: false);
            surface.Add(Element<LineElement>(150, 10, 100, 10), select: false);

            var annotations = EditorAnnotationImporter.ImportFromSurface(surface);

            Assert.Equal(new[] { "Rectangle", "Ellipse", "Line" }, annotations.Select(a => a["Type"]));
        }

        [Fact]
        public void StepNodeViewModel_ImportAnnotations_AppendAndReplace()
        {
            var config = new RecipeNodeConfig("step1", "Annotation");
            var nodeVm = new StepNodeViewModel(config, new Point(0, 0));
            nodeVm.AddAnnotation("Rectangle");
            Assert.Single(nodeVm.Annotations);

            var imported = new List<Dictionary<string, object>>
            {
                new() { ["Type"] = "Text", ["Text"] = "Imported", ["OffsetX"] = "10", ["OffsetY"] = "10", ["Width"] = "100", ["Height"] = "30" },
                new() { ["Type"] = "Arrow", ["OffsetX"] = "20", ["OffsetY"] = "20", ["Width"] = "80", ["Height"] = "40" }
            };

            nodeVm.ImportAnnotations(imported, replaceExisting: false);
            Assert.Equal(new[] { "Rectangle", "Text", "Arrow" }, nodeVm.Annotations.Select(a => a.Type));

            nodeVm.ImportAnnotations(imported, replaceExisting: true);
            Assert.Equal(new[] { "Text", "Arrow" }, nodeVm.Annotations.Select(a => a.Type));
        }

        [Fact]
        public void LoadSurfaceFromGreenshotFile_Roundtrip()
        {
            var surface = CreateTestSurface();
            surface.Add(Element<RectangleElement>(12, 34, 100, 80), select: false);
            var text = Element<TextElement>(50, 60, 150, 40);
            text.Text = "Test Annotation";
            surface.Add(text, select: false);

            string tempFile = Path.Combine(Path.GetTempPath(), $"greenshot_import_test_{Guid.NewGuid():N}.greenshot");
            try
            {
                GreenshotFile.Save(tempFile, surface.Image, surface.SerializeElements());

                var loaded = EditorAnnotationImporter.LoadSurfaceFromGreenshotFile(tempFile);
                Assert.Equal(2, loaded.Elements.Count);

                var imported = EditorAnnotationImporter.ImportFromSurface(loaded);
                Assert.Equal("Rectangle", imported[0]["Type"]);
                Assert.Equal("12", imported[0]["OffsetX"]);
                Assert.Equal("34", imported[0]["OffsetY"]);
                Assert.Equal("Text", imported[1]["Type"]);
                Assert.Equal("Test Annotation", imported[1]["Text"]);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
