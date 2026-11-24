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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Greenshot.Document;
using Greenshot.Document.Legacy;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class GreenshotPackageTests
    {
        private static readonly string TestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "LegacyFormat");

        public static IEnumerable<object[]> GreenshotFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "Greenshotfile"), "*.greenshot", SearchOption.AllDirectories)
            .Select(file => new object[] { file.Substring(TestDataDirectory.Length + 1) });

        [Fact]
        public void Write_DropsAFrameWhoseTargetIsGone()
        {
            var box = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));
            var kept = new FrameElement(Guid.NewGuid(), box.Area, 1) { Target = box.Id };
            var orphan = new FrameElement(Guid.NewGuid(), box.Area, 2) { Target = Guid.NewGuid() };
            var free = new FrameElement(Guid.NewGuid(), box.Area, 3);
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>
            {
                new(Guid.NewGuid(), "Elements", LayerKind.Custom, new List<Element> { box, kept, orphan, free })
            }, new Dictionary<Guid, Resource>());
            using var stream = new MemoryStream();

            GreenshotPackage.Write(document, stream, _ => "png");

            stream.Position = 0;
            Assert.Equal(new[] { box.Id, kept.Id, free.Id }, GreenshotPackage.Read(stream).Layers[0].Elements.Select(element => element.Id));
        }

        /// <summary>
        /// A document with every element type, and values which aren't the defaults
        /// </summary>
        [Fact]
        public void Write_LeavesOutDefaults_AndWritesTheSizeOfAResource_AndTheCanvasAsTarget()
        {
            var png = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 13, 10, 26, 10, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R', 0, 0, 7, 128, 0, 0, 4, 56 });
            var capture = new ImageElement(Guid.NewGuid(), new RectD(0, 0, 960, 540), png.Id) { Anchor = new Anchor { Target = Anchors.Canvas, X = new AxisAnchor(AnchorLine.Center, AnchorLine.Center) } };
            var document = new GreenshotDocument(Guid.NewGuid(), Canvas.Fixed(1920, 1080), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { capture })
            }, new Dictionary<Guid, Resource> { [png.Id] = png });
            using var stream = new MemoryStream();

            GreenshotPackage.Write(document, stream, _ => "png");

            stream.Position = 0;
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, true))
            using (var reader = new StreamReader(zip.GetEntry("document.json")!.Open()))
            {
                string json = reader.ReadToEnd();
                Assert.DoesNotContain("\"scaling\"", json);
                Assert.DoesNotContain("\"rotation\"", json);
                Assert.DoesNotContain("\"style\"", json);
                Assert.DoesNotContain("\"offset\"", json);
                Assert.DoesNotContain("\"locked\"", json);
                Assert.Contains("\"target\": \"canvas\"", json);
                Assert.Contains("\"width\": 1920", json);
            }

            stream.Position = 0;
            var read = GreenshotPackage.Read(stream);
            Assert.Equal(1920, read.Resources[png.Id].Width);
            Assert.Equal(1080, read.Resources[png.Id].Height);
            Assert.Equal(capture, read.Layers[0].Elements[0]);
            Assert.Equal(document.Canvas, read.Canvas);
        }

        private static GreenshotDocument AllElements()
        {
            var red = new Rgba(255, 0, 0, 255);
            var image = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 });
            var svg = new Resource(Guid.NewGuid(), "image/svg+xml", Encoding.UTF8.GetBytes("<svg/>"));
            var mask = new Resource(Guid.NewGuid(), "image/bmp", new byte[] { (byte)'B', (byte)'M', 0, 255 });
            var style = new Style
            {
                Stroke = StrokeStyle.Of(red, 2.5) with { Position = StrokePosition.Inside },
                Fill = FillStyle.Of(new Rgba(1, 2, 3, 128)),
                Shadow = new ShadowStyle { Enabled = true },
                Text = new TextStyle
                {
                    Enabled = true, Family = "Segoe UI, Arial, sans-serif", Size = 12.5, Weight = 600, Italic = true, Underline = true, Strikeout = false, Color = red,
                    Horizontal = TextAlignment.Start, Vertical = TextAlignment.End, LineHeight = 1.5, Wrap = false, Outline = StrokeStyle.Of(new Rgba(255, 255, 255, 255), 2)
                },
                Extensions = new RawJson("{\"zxing\":{\"quietZone\":4}}")
            };
            var area = new RectD(10.5, 20, 30, 40);
            var stepLabel = new StepLabelElement(Guid.NewGuid(), area) { Style = new Style { Fill = new FillStyle { Enabled = false } }, Extensions = new RawJson("{\"plugin\":[1,2]}") };
            var elements = new List<Element>
            {
                new RectangleElement(Guid.NewGuid(), area) { Style = style, Rotation = 30.5, Label = new BoxLabel("Login") { X = LabelPlace.Start, Y = LabelPlace.After, Offset = 4, Width = 80 } },
                new EllipseElement(Guid.NewGuid(), area),
                new LineElement(Guid.NewGuid(), new PointD(1, 2), new PointD(3, 4)) { Label = new LineLabel("20 ms") { At = 0.25, Offset = -6, Orientation = LabelOrientation.Along } },
                new ArrowElement(Guid.NewGuid(), new PointD(1, 2), new PointD(3, 4), ArrowHeads.Both)
                {
                    StartAnchor = new Anchor { Target = Guid.NewGuid(), X = new AxisAnchor(AnchorLine.End, AnchorLine.Center, -2.5) },
                    EndAnchor = new Anchor { Y = new AxisAnchor(AnchorLine.Center) }
                },
                new FreehandElement(Guid.NewGuid(), new List<PointD> { new(1, 2), new(3, 4), new(5, 6) }),
                new TextElement(Guid.NewGuid(), area, "Text\nwith \"quotes\" and ü") { Style = style },
                new SpeechBubbleElement(Guid.NewGuid(), area, "Bubble", new PointD(100, 200))
                {
                    Anchor = new Anchor { X = new AxisAnchor(AnchorLine.Start, AnchorLine.End, 8) },
                    TailAnchor = new Anchor { Target = Guid.NewGuid(), Y = new AxisAnchor(AnchorLine.End) }
                },
                stepLabel,
                new HighlightElement(Guid.NewGuid(), area, HighlightMode.Area) { Color = red, Brightness = 0.5, BlurRadius = 7, Magnification = 4 },
                new ObfuscateElement(Guid.NewGuid(), area, ObfuscateMode.Blur, 12),
                new ImageElement(Guid.NewGuid(), area, image.Id) { Rotation = 90, Source = new RectD(0.25, 0.1, 0.5, 0.25), Scaling = ImageScaling.Pixelated },
                new ImageElement(Guid.NewGuid(), area, svg.Id),
                new EmojiElement(Guid.NewGuid(), area, "👍") { Rotation = 45 },
                new CursorElement(Guid.NewGuid(), area, image.Id) { MaskResource = mask.Id, Hotspot = new PointD(0.25, 0.5) },
                new CutMarkElement(Guid.NewGuid(), area, true, 42) { Shape = new EdgeShape(EdgeStyle.ZigZag) { Height = 5, Range = 6 } },
                new FrameElement(Guid.NewGuid(), area, 43)
                {
                    Shape = new EdgeShape(EdgeStyle.Torn) { Height = 5, Range = 6 },
                    Sides = new FrameSides { Bottom = new EdgeShape(EdgeStyle.Wave), Right = new EdgeShape(EdgeStyle.None) },
                    CornerRadius = 8,
                    Placement = FramePlacement.Center,
                    Band = new FrameBand(16, new Rgba(255, 255, 255, 255)),
                    Style = new Style { Shadow = new ShadowStyle { Enabled = true, Size = 6, Darkness = 0.6, Offset = new PointD(3, 3) } }
                },
                new PluginElement(Guid.NewGuid(), area, "greenshot.barcode") { Version = 2, Data = "{\"text\":\"Greenshot\",\"size\":[1,2]}", Resources = new[] { svg.Id }, Fallback = image.Id }
            };
            var layers = new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 800, 600), image.Id) }) { Locked = true },
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, elements) { Visible = false }
            };
            return new GreenshotDocument(Guid.NewGuid(), new Canvas { Fit = CanvasFit.Content, Padding = 20, Background = red }, layers,
                new Dictionary<Guid, Resource> { [image.Id] = image, [svg.Id] = svg, [mask.Id] = mask })
            {
                Groups = new[]
                {
                    new Group(Guid.NewGuid(), GroupTypes.Steps, new Guid?[] { null, stepLabel.Id }) { Name = "Steps", Style = style, Properties = new StepNumbering(5, StepFormat.LowerRoman).ToProperties() },
                    new Group(Guid.NewGuid(), "zxing.barcodes", new Guid?[] { stepLabel.Id }) { Properties = new RawJson("{\"x\":1}"), Extensions = new RawJson("{}") }
                }
            };
        }

        /// <summary>
        /// Like the file format registry: the extension of a supported media type, otherwise null
        /// </summary>
        private static string ExtensionFor(string mediaType) => mediaType switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/gif" => "gif",
            "image/bmp" => "bmp",
            "image/tiff" => "tiff",
            "image/svg+xml" => "svg",
            "image/x-icon" => "ico",
            _ => null
        };

        private static GreenshotDocument RoundTrip(GreenshotDocument document)
        {
            using var stream = new MemoryStream();
            GreenshotPackage.Write(document, stream, ExtensionFor);
            stream.Position = 0;
            return GreenshotPackage.Read(stream);
        }

        /// <summary>
        /// Records compare lists by reference, so layers, freehand points and resource bytes are compared one by one
        /// </summary>
        private static void AssertSameDocument(GreenshotDocument expected, GreenshotDocument actual)
        {
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Canvas, actual.Canvas);
            Assert.Equal(expected.Groups.Select(group => group with { Members = null }), actual.Groups.Select(group => group with { Members = null }));
            Assert.Equal(expected.Groups.Select(group => string.Join(",", group.Members)), actual.Groups.Select(group => string.Join(",", group.Members)));
            Assert.Equal(expected.Layers.Count, actual.Layers.Count);
            for (int layer = 0; layer < expected.Layers.Count; layer++)
            {
                Assert.Equal(expected.Layers[layer] with { Elements = null }, actual.Layers[layer] with { Elements = null });
                var expectedElements = expected.Layers[layer].Elements;
                var actualElements = actual.Layers[layer].Elements;
                Assert.Equal(expectedElements.Count, actualElements.Count);
                for (int i = 0; i < expectedElements.Count; i++)
                {
                    if (expectedElements[i] is PluginElement expectedPlugin)
                    {
                        var actualPlugin = Assert.IsType<PluginElement>(actualElements[i]);
                        Assert.Equal(expectedPlugin.Resources, actualPlugin.Resources);
                        Assert.Equal(expectedPlugin with { Resources = null }, actualPlugin with { Resources = null });
                    }
                    else if (expectedElements[i] is FreehandElement expectedFreehand)
                    {
                        var actualFreehand = Assert.IsType<FreehandElement>(actualElements[i]);
                        Assert.Equal(expectedFreehand.Points, actualFreehand.Points);
                        Assert.Equal(expectedFreehand with { Points = null }, actualFreehand with { Points = null });
                    }
                    else
                    {
                        Assert.Equal(expectedElements[i], actualElements[i]);
                    }
                }
            }

            Assert.Equal(expected.Resources.Keys.OrderBy(id => id), actual.Resources.Keys.OrderBy(id => id));
            foreach (var resource in expected.Resources.Values)
            {
                Assert.Equal(resource.MediaType, actual.Resources[resource.Id].MediaType);
                Assert.Equal(resource.Data.ToArray(), actual.Resources[resource.Id].Data.ToArray());
            }
        }

        [Fact]
        public void RoundTrip_KeepsEveryElement()
        {
            var document = AllElements();

            AssertSameDocument(document, RoundTrip(document));
        }

        [Theory]
        [MemberData(nameof(GreenshotFiles))]
        public void RoundTrip_KeepsLegacyFiles(string relativePath)
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, relativePath));
            var document = LegacyToDocument.ReadGreenshotFile(stream);

            AssertSameDocument(document, RoundTrip(document));
        }

        [Fact]
        public void Write_PutsTheMimetypeFirstAndUsesShortNames()
        {
            using var stream = new MemoryStream();
            GreenshotPackage.Write(AllElements(), stream, ExtensionFor);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            Assert.Equal("mimetype", zip.Entries[0].FullName);
            using var reader = new StreamReader(zip.GetEntry("document.json").Open());
            string json = reader.ReadToEnd();
            Assert.Contains("\"type\": \"arrow\"", json);
            Assert.Contains("\"color\": \"#FF0000FF\"", json);
            Assert.DoesNotContain("Greenshot.Document", json);
            Assert.DoesNotContain("bounds", json);
            Assert.DoesNotContain("outerEdge", json);
            Assert.Contains("\"position\": \"inside\"", json);
            Assert.Contains("\"horizontal\": \"start\"", json);
            Assert.Contains("\"placement\": \"center\"", json);
            Assert.Equal(3, zip.Entries.Count(entry => entry.FullName.StartsWith("resources/")));
        }

        [Fact]
        public void Write_KeepsThePluginDataAsJson()
        {
            using var stream = new MemoryStream();
            GreenshotPackage.Write(AllElements(), stream, ExtensionFor);
            stream.Position = 0;

            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            using var reader = new StreamReader(zip.GetEntry("document.json").Open());
            using var json = JsonDocument.Parse(reader.ReadToEnd());
            var plugin = json.RootElement.GetProperty("layers")[1].GetProperty("elements").EnumerateArray().Single(element => element.GetProperty("type").GetString() == "plugin");
            Assert.Equal("Greenshot", plugin.GetProperty("data").GetProperty("text").GetString());
        }

        [Fact]
        public void Strict_KeepsOnlyWhatIsDrawn()
        {
            var extensions = new RawJson("{\"other.plugin\":{\"secret\":1}}");
            var png = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1 }) { Extensions = extensions };
            var fallback = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 2 });
            var hidden = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 3 });
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(1, 2, 3, 4)) { Extensions = extensions, Style = new Style { Extensions = extensions } };
            var plugin = new PluginElement(Guid.NewGuid(), new RectD(5, 6, 7, 8), "greenshot.barcode") { Data = "{\"text\":\"secret\"}", Fallback = fallback.Id };
            var withoutFallback = new PluginElement(Guid.NewGuid(), new RectD(5, 6, 7, 8), "other.plugin") { Data = "{}" };
            var unknown = new UnknownElement(Guid.NewGuid(), new RectD(1, 1, 1, 1), "hologram", $"{{\"type\":\"hologram\",\"source\":\"{hidden.Id}\"}}");
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 10, 10), png.Id) }) { Extensions = extensions },
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, new List<Element> { rectangle, plugin, withoutFallback, unknown })
            }, new Dictionary<Guid, Resource> { [png.Id] = png, [fallback.Id] = fallback, [hidden.Id] = hidden })
            {
                Extensions = extensions,
                Metadata = new DocumentMetadata { Title = "Secret" },
                Analysis = new Dictionary<Guid, ImageAnalysis> { [png.Id] = new() { Ocr = new OcrResult(new[] { new OcrLine("secret", default, new List<OcrWord>()) }) } },
                Groups = new List<Group>
                {
                    new(Guid.NewGuid(), GroupTypes.Set, new Guid?[] { rectangle.Id }) { Extensions = extensions },
                    new(Guid.NewGuid(), "other.tracking", new Guid?[] { rectangle.Id })
                }
            };

            Assert.False(document.Strict().Resources.ContainsKey(hidden.Id));
            var read = RoundTrip(document.Strict());

            Assert.Null(read.Metadata);
            Assert.Empty(read.Analysis);
            Assert.Null(read.Extensions);
            Assert.Null(read.Layers[0].Extensions);
            Assert.Null(read.Resources[png.Id].Extensions);
            Assert.False(read.Resources.ContainsKey(hidden.Id));
            var elements = read.Layers[1].Elements;
            Assert.Equal(2, elements.Count);
            var strictRectangle = Assert.IsType<RectangleElement>(elements[0]);
            Assert.Null(strictRectangle.Extensions);
            Assert.Null(strictRectangle.Style.Extensions);
            Assert.Equal(new ImageElement(plugin.Id, plugin.Area, fallback.Id), elements[1]);
            var group = Assert.Single(read.Groups);
            Assert.Equal(GroupTypes.Set, group.Type);
            Assert.Null(group.Extensions);

            // The default keeps everything
            Assert.Equal("Secret", RoundTrip(document).Metadata.Title);
        }

        [Fact]
        public void RoundTrip_KeepsTheExtensionsOfTheDocument_ALayer_AndAResource()
        {
            var png = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 }) { Extensions = new RawJson("{\"greenshot.ocr\":{\"text\":\"Save\"}}") };
            var layer = new Layer(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 10, 10), png.Id) })
            {
                Extensions = new RawJson("{\"export\":{\"blend\":\"multiply\"}}")
            };
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer> { layer }, new Dictionary<Guid, Resource> { [png.Id] = png })
            {
                Extensions = new RawJson("{\"confluence\":{\"pageId\":\"123456\"}}")
            };
            using var stream = new MemoryStream();

            GreenshotPackage.Write(document, stream, ExtensionFor);
            stream.Position = 0;
            var read = GreenshotPackage.Read(stream);

            Assert.Equal("123456", JsonDocument.Parse(read.Extensions.Json).RootElement.GetProperty("confluence").GetProperty("pageId").GetString());
            Assert.Equal("multiply", JsonDocument.Parse(read.Layers[0].Extensions.Json).RootElement.GetProperty("export").GetProperty("blend").GetString());
            Assert.Equal("Save", JsonDocument.Parse(read.Resources[png.Id].Extensions.Json).RootElement.GetProperty("greenshot.ocr").GetProperty("text").GetString());
        }

        [Fact]
        public void RoundTrip_KeepsTheAnalysisOfAWrittenResource_AndUnknownKinds()
        {
            var png = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 });
            var unlinked = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 4 });
            var ocr = new OcrResult(new[] { new OcrLine("Save changes", new RectD(12, 40, 300, 18), new[] { new OcrWord("Save", new RectD(12, 40, 60, 18)) { Confidence = 0.97 } }) })
            {
                Engine = "Windows.Media.Ocr", Language = "en-US", TextAngle = 0
            };
            var barcode = new BarcodeResult("QR_CODE", "https://getgreenshot.org", BarcodeResult.Corners(new RectD(400, 80, 80, 80)));
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 10, 10), png.Id) })
            }, new Dictionary<Guid, Resource> { [png.Id] = png, [unlinked.Id] = unlinked })
            {
                Analysis = new Dictionary<Guid, ImageAnalysis>
                {
                    [png.Id] = new() { Ocr = ocr, Barcodes = new[] { barcode } },
                    [unlinked.Id] = new() { Barcodes = new[] { barcode } }
                }
            };
            using var stream = new MemoryStream();
            GreenshotPackage.Write(document, stream, ExtensionFor);

            // A newer Greenshot adds a kind this version doesn't know
            stream.Position = 0;
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, true))
            {
                var entry = zip.GetEntry("analysis.json");
                string json;
                using (var reader = new StreamReader(entry.Open()))
                {
                    json = reader.ReadToEnd();
                }

                entry.Delete();
                using var writer = new StreamWriter(zip.CreateEntry("analysis.json").Open());
                writer.Write(json.Replace("\"barcodes\"", "\"faces\": [ { \"x\": 1 } ], \"barcodes\""));
            }

            stream.Position = 0;
            var read = GreenshotPackage.Read(stream);
            Assert.False(read.Analysis.ContainsKey(unlinked.Id));
            var analysis = read.Analysis[png.Id];
            Assert.Equal("Windows.Media.Ocr", analysis.Ocr.Engine);
            Assert.Equal("en-US", analysis.Ocr.Language);
            Assert.Equal(new RectD(12, 40, 300, 18), analysis.Ocr.Lines[0].Bounds);
            Assert.Equal(0.97, analysis.Ocr.Lines[0].Words[0].Confidence);
            Assert.Null(analysis.Ocr.Lines[0].Confidence);
            Assert.Equal(barcode.Points, analysis.Barcodes[0].Points);
            Assert.Equal(new RectD(400, 80, 80, 80), analysis.Barcodes[0].Bounds);
            Assert.Equal(1, analysis.Unknown["faces"][0].GetProperty("x").GetInt32());

            using var again = new MemoryStream();
            GreenshotPackage.Write(read, again, ExtensionFor);
            again.Position = 0;
            Assert.True(GreenshotPackage.Read(again).Analysis[png.Id].Unknown.ContainsKey("faces"));
        }

        [Fact]
        public void RoundTrip_WritesTheMetadataAsSaved_AndKeepsUnknownValues()
        {
            var png = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 });
            var created = new DateTimeOffset(2026, 10, 12, 16, 20, 0, TimeSpan.FromHours(2));
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 10, 10), png.Id) })
            }, new Dictionary<Guid, Resource> { [png.Id] = png });
            using (var withoutMetadata = new MemoryStream())
            {
                GreenshotPackage.Write(document, withoutMetadata, ExtensionFor);
                withoutMetadata.Position = 0;
                using var zip = new ZipArchive(withoutMetadata, ZipArchiveMode.Read);
                Assert.Null(zip.GetEntry("metadata.json"));
            }

            document = document with
            {
                Metadata = new DocumentMetadata
                {
                    Title = "Checkout dialog",
                    Tags = new[] { "bug-report" },
                    Created = created,
                    Revision = 2,
                    Source = new MetadataSource(SourceTypes.Capture)
                    {
                        Mode = "window",
                        Date = created,
                        Window = new WindowInfo { Title = "Checkout", Application = new ApplicationInfo { Name = "Notepad", Id = "notepad.exe" } },
                        Screens = new[] { new ScreenInfo { Name = "DISPLAY1", Bounds = new RectD(0, 0, 1920, 1080), Dpi = 144 } }
                    },
                    Session = new SessionInfo(Guid.NewGuid()) { Name = "PROD-1042", WorkItem = new WorkItem("jira", "PROD-1042") { Title = "Checkout times out" } }
                }
            };
            using var stream = new MemoryStream();
            GreenshotPackage.Write(document, stream, ExtensionFor);

            // A newer Greenshot adds values this version doesn't know
            stream.Position = 0;
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, true))
            {
                var entry = zip.GetEntry("metadata.json");
                string json;
                using (var reader = new StreamReader(entry.Open()))
                {
                    json = reader.ReadToEnd();
                }

                entry.Delete();
                using var writer = new StreamWriter(zip.CreateEntry("metadata.json").Open());
                writer.Write(json.Replace("\"title\": \"Checkout dialog\"", "\"rights\": \"CC-BY\", \"title\": \"Checkout dialog\"").Replace("\"mode\"", "\"viewport\": { \"width\": 1280 }, \"mode\""));
            }

            stream.Position = 0;
            var read = GreenshotPackage.Read(stream).Metadata;
            Assert.Equal("Checkout dialog", read.Title);
            Assert.Equal(new[] { "bug-report" }, read.Tags);
            Assert.Equal(created, read.Created);
            Assert.Equal(3, read.Revision);
            Assert.NotNull(read.Modified);
            Assert.Equal("notepad.exe", read.Source.Window.Application.Id);
            Assert.Equal(144, read.Source.Screens[0].Dpi);
            Assert.Equal("Checkout times out", read.Session.WorkItem.Title);
            Assert.Equal(document.Metadata.Session.Id, read.Session.Id);
            Assert.Equal("CC-BY", read.Unknown["rights"].GetString());
            Assert.Equal(1280, read.Source.Unknown["viewport"].GetProperty("width").GetInt32());

            var again = RoundTrip(GreenshotPackage.Read(Rewind(stream))).Metadata;
            Assert.Equal(4, again.Revision);
            Assert.True(again.Unknown.ContainsKey("rights"));
            Assert.True(again.Source.Unknown.ContainsKey("viewport"));
        }

        private static Stream Rewind(Stream stream)
        {
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public void Read_KeepsAnElementOfAnUnknownType()
        {
            var document = AllElements();
            var resource = document.Resources.Values.Single(r => r.MediaType == "image/svg+xml");
            string unknownJson = $"{{\"type\":\"hologram\",\"id\":\"{Guid.NewGuid()}\",\"area\":{{\"x\":1,\"y\":2,\"width\":3,\"height\":4}},\"source\":\"{resource.Id}\",\"depth\":{{\"z\":[1,2]}}}}";
            using var json = JsonDocument.Parse(unknownJson);
            var unknown = new UnknownElement(Guid.Empty, default, "hologram", unknownJson);
            // Only the unknown element links the svg
            var layers = document.Layers.Select(layer => layer with { Elements = layer.Elements.Where(element => !(element is ImageElement { Rotation: 0 } image && image.Resource == resource.Id) && element is not PluginElement).Append(unknown).ToList() }).ToList();

            var read = RoundTrip(document with { Layers = layers });

            var readUnknown = Assert.IsType<UnknownElement>(read.Layers[1].Elements.Last());
            Assert.Equal("hologram", readUnknown.TypeName);
            Assert.Equal(new RectD(1, 2, 3, 4), readUnknown.Area);
            Assert.True(JsonElement.DeepEquals(json.RootElement, JsonDocument.Parse(readUnknown.Json).RootElement));
            Assert.Equal(resource.Data.ToArray(), read.Resources[resource.Id].Data.ToArray());
        }

        [Fact]
        public void ReadElements_ReadsElementsLikeDocumentJson()
        {
            var elements = GreenshotPackage.ReadElements("[{\"type\": \"rectangle\", \"id\": \"01a12eae-5fab-7ead-81e1-92243258dacb\", \"area\": {\"x\": 10, \"y\": 20, \"width\": 30, \"height\": 40}}," +
                "{\"type\": \"text\", \"area\": {\"x\": 1, \"y\": 2, \"width\": 3, \"height\": 4}, \"text\": \"Greenshot\"}]");

            Assert.Equal(new RectangleElement(Guid.Parse("01a12eae-5fab-7ead-81e1-92243258dacb"), new RectD(10, 20, 30, 40)), elements[0]);
            Assert.Equal("Greenshot", Assert.IsType<TextElement>(elements[1]).Text);
        }

        [Fact]
        public void Write_LeavesOutResourcesNoElementLinksTo()
        {
            var document = AllElements();
            var unused = new Resource(Guid.NewGuid(), "image/png", new byte[] { 1 });
            var resources = document.Resources.Values.Append(unused).ToDictionary(resource => resource.Id);

            var written = RoundTrip(document with { Resources = resources });

            Assert.False(written.Resources.ContainsKey(unused.Id));
            Assert.Equal(document.Resources.Count, written.Resources.Count);
        }

        [Fact]
        public void Write_DropsMembersWhichNoLongerExist_AndKeepsGaps()
        {
            var document = AllElements();
            var deleted = Guid.NewGuid();
            var steps = document.Groups[0];

            var written = RoundTrip(document with { Groups = new[] { steps with { Members = steps.Members.Append(deleted).ToList() } } });

            Assert.Equal(steps.Members, Assert.Single(written.Groups).Members);
        }

        [Fact]
        public void Write_RefusesALinkToAMissingResource()
        {
            var document = AllElements();
            var layers = document.Layers.Select(layer => layer with { Elements = layer.Elements.Append(new ImageElement(Guid.NewGuid(), new RectD(0, 0, 1, 1), Guid.NewGuid())).ToList() }).ToList();

            Assert.Throws<InvalidDataException>(() => RoundTrip(document with { Layers = layers }));
        }

        [Fact]
        public void Write_RefusesAnUnsupportedMediaType()
        {
            var document = AllElements();
            var resources = document.Resources.Values.Select(resource => resource.MediaType == "image/bmp" ? resource with { MediaType = "application/octet-stream" } : resource).ToDictionary(resource => resource.Id);

            Assert.Throws<InvalidDataException>(() => RoundTrip(document with { Resources = resources }));
        }

        [Fact]
        public void Read_RefusesANewerVersion()
        {
            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                Write(zip, "mimetype", GreenshotPackage.MediaType);
                Write(zip, "document.json", "{\"version\": 2}");
            }

            stream.Position = 0;

            Assert.Throws<InvalidDataException>(() => GreenshotPackage.Read(stream));
        }

        [Fact]
        public void Read_RefusesAnotherZip()
        {
            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                Write(zip, "mimetype", "application/epub+zip");
            }

            stream.Position = 0;

            Assert.Throws<InvalidDataException>(() => GreenshotPackage.Read(stream));
        }

        [Fact]
        public void Read_RefusesMoreThanTenThousandEntries()
        {
            using var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                Write(zip, "mimetype", "application/vnd.greenshot.document+zip");
                for (int i = 0; i < 10_000; i++)
                {
                    zip.CreateEntry($"resources/{i}.png");
                }
            }

            stream.Position = 0;

            Assert.Contains("entries", Assert.Throws<InvalidDataException>(() => GreenshotPackage.Read(stream)).Message);
        }

        private static void Write(ZipArchive zip, string path, string content)
        {
            using var writer = new StreamWriter(zip.CreateEntry(path).Open());
            writer.Write(content);
        }
    }
}
