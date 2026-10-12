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

        /// <summary>
        /// A document with every element type, and values which aren't the defaults
        /// </summary>
        private static GreenshotDocument AllElements()
        {
            var red = new Rgba(255, 0, 0, 255);
            var image = new Resource(Guid.NewGuid(), "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 });
            var svg = new Resource(Guid.NewGuid(), "image/svg+xml", Encoding.UTF8.GetBytes("<svg/>"));
            var mask = new Resource(Guid.NewGuid(), "image/bmp", new byte[] { (byte)'B', (byte)'M', 0, 255 });
            var style = new ElementStyle(new Stroke(red, 2.5), new Fill(new Rgba(1, 2, 3, 128)), Shadow.Default,
                new TextStyle(new FontSpec("Segoe UI", 12.5, true, true), red, TextAlignment.Near, TextAlignment.Far));
            var area = new RectD(10.5, 20, 30, 40);
            var elements = new List<Element>
            {
                new RectangleElement(Guid.NewGuid(), area) { Style = style },
                new EllipseElement(Guid.NewGuid(), area),
                new LineElement(Guid.NewGuid(), new PointD(1, 2), new PointD(3, 4)),
                new ArrowElement(Guid.NewGuid(), new PointD(1, 2), new PointD(3, 4), ArrowHeads.Both),
                new FreehandElement(Guid.NewGuid(), new List<PointD> { new(1, 2), new(3, 4), new(5, 6) }),
                new TextElement(Guid.NewGuid(), area, "Text\nwith \"quotes\" and ü") { Style = style },
                new SpeechBubbleElement(Guid.NewGuid(), area, "Bubble", new PointD(100, 200)),
                new StepLabelElement(Guid.NewGuid(), area),
                new HighlightElement(Guid.NewGuid(), area, HighlightMode.Area) { Color = red, Brightness = 0.5, BlurRadius = 7, Magnification = 4 },
                new ObfuscateElement(Guid.NewGuid(), area, ObfuscateMode.Blur, 12),
                new ImageElement(Guid.NewGuid(), area, image.Id) { Rotation = 90 },
                new ImageElement(Guid.NewGuid(), area, svg.Id),
                new EmojiElement(Guid.NewGuid(), area, "👍") { Rotation = 45 },
                new CursorElement(Guid.NewGuid(), area, image.Id) { MaskResource = mask.Id, Hotspot = new PointD(3, 4) },
                new CutMarkElement(Guid.NewGuid(), area, true, CutStyle.ZigZag, 42) { ToothHeight = 5, ToothRange = 6 },
                new TornEdgeElement(Guid.NewGuid(), area, CutStyle.Torn, 43) { Edges = Edges.Top | Edges.Left, Margins = new Margins(1, 2, 3, 4), ToothHeight = 5, ToothRange = 6 },
                new PluginElement(Guid.NewGuid(), area, "greenshot.barcode") { Version = 2, Data = "{\"text\":\"Greenshot\",\"size\":[1,2]}", Resources = new[] { svg.Id }, Fallback = image.Id }
            };
            var layers = new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 800, 600), image.Id) }) { Locked = true },
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, elements) { Visible = false }
            };
            return new GreenshotDocument(Guid.NewGuid(), new Canvas(800, 600) { Background = red, CounterStart = 5 }, layers,
                new Dictionary<Guid, Resource> { [image.Id] = image, [svg.Id] = svg, [mask.Id] = mask });
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
            "image/emf" => "emf",
            "image/wmf" => "wmf",
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
                Assert.Equal(resource.Data, actual.Resources[resource.Id].Data);
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
            stream.Position = 0;

            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            Assert.Equal("mimetype", zip.Entries[0].FullName);
            using var reader = new StreamReader(zip.GetEntry("document.json").Open());
            string json = reader.ReadToEnd();
            Assert.Contains("\"type\": \"arrow\"", json);
            Assert.Contains("\"color\": \"#FF0000FF\"", json);
            Assert.DoesNotContain("Greenshot.Document", json);
            Assert.DoesNotContain("bounds", json);
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
            Assert.Equal(resource.Data, read.Resources[resource.Id].Data);
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

        private static void Write(ZipArchive zip, string path, string content)
        {
            using var writer = new StreamWriter(zip.CreateEntry(path).Open());
            writer.Write(content);
        }
    }
}
