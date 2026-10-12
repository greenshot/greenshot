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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Document;
using Greenshot.Document.Editor;
using Greenshot.Document.Legacy;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Document
{
    public class DocumentToSurfaceTests
    {
        /// <summary>
        /// Stands in for the container of a plugin which has a converter
        /// </summary>
        private sealed class TextPluginContainer : RectangleContainer
        {
            public TextPluginContainer(ISurface parent) : base(parent)
            {
            }

            public string Text { get; set; }
        }

        private sealed class TextPluginConverter : IPluginElementConverter
        {
            public string Kind => "greenshot.test.text";

            public int Version => 2;

            public bool CanConvert(IDrawableContainer container) => container is TextPluginContainer;

            public PluginElement ToElement(IDrawableContainer container, PluginElement element, IDictionary<Guid, Resource> resources) =>
                element with { Data = JsonSerializer.Serialize(new { text = ((TextPluginContainer)container).Text }) };

            public IDrawableContainer ToContainer(ISurface surface, PluginElement element, IReadOnlyDictionary<Guid, Resource> resources)
            {
                using var data = JsonDocument.Parse(element.Data);
                return new TextPluginContainer(surface)
                {
                    Text = data.RootElement.GetProperty("text").GetString(),
                    Left = (int)element.Area.X,
                    Top = (int)element.Area.Y,
                    Width = (int)element.Area.Width,
                    Height = (int)element.Area.Height
                };
            }
        }

        private static readonly object RegisterLock = new();
        private static bool _registered;

        private static readonly string TestDataDirectory = Path.Combine(AppContext.BaseDirectory, "TestData", "LegacyFormat");

        public DocumentToSurfaceTests()
        {
            TestEnvironment.EnsureInitialized();
            lock (RegisterLock)
            {
                if (!_registered)
                {
                    SimpleServiceProvider.Current.AddService<IPluginElementConverter>(new TextPluginConverter());
                    _registered = true;
                }
            }
        }

        private static Resource Png(int width, int height)
        {
            using var bitmap = new Bitmap(width, height);
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return new Resource(Guid.NewGuid(), "image/png", stream.ToArray());
        }

        private static GreenshotDocument WithElements(IReadOnlyDictionary<Guid, Resource> resources, params Element[] elements)
        {
            var capture = Png(200, 100);
            var all = resources.Values.Append(capture).ToDictionary(resource => resource.Id);
            return new GreenshotDocument(Guid.NewGuid(), new Canvas(200, 100), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 200, 100), capture.Id) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, elements)
            }, all);
        }

        [Fact]
        public void PluginElement_WithItsConverter_IsTheContainerOfThePlugin()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var container = new TextPluginContainer(surface) { Text = "Greenshot", Left = 10, Top = 20, Width = 30, Height = 40 };
            surface.AddElement(container, false, false);

            var document = SurfaceToDocument.ToDocument(surface);
            var plugin = Assert.IsType<PluginElement>(Assert.Single(document.Layers[1].Elements));
            Assert.Equal("greenshot.test.text", plugin.Kind);
            Assert.Equal(2, plugin.Version);
            Assert.Equal("image/png", document.Resources[plugin.Fallback.Value].MediaType);

            using var mappedSurface = DocumentToSurface.ToSurface(document);
            var mapped = Assert.IsType<TextPluginContainer>(Assert.Single(mappedSurface.Elements));
            Assert.Equal("Greenshot", mapped.Text);
        }

        [Fact]
        public void PluginElement_WithoutItsPlugin_IsWrittenBackUnchanged()
        {
            var fallback = Png(30, 40);
            var linked = Png(2, 2);
            var plugin = new PluginElement(Guid.NewGuid(), new RectD(10, 20, 30, 40), "greenshot.test.missing")
            {
                Version = 3,
                Data = "{\"value\":42}",
                Resources = new[] { linked.Id },
                Fallback = fallback.Id
            };
            // A newer version than the converter reads is the same as a missing plugin
            var newer = new PluginElement(Guid.NewGuid(), new RectD(50, 20, 30, 40), "greenshot.test.text") { Version = 3, Data = "{}", Fallback = fallback.Id };
            var document = WithElements(new Dictionary<Guid, Resource> { [fallback.Id] = fallback, [linked.Id] = linked }, plugin, newer);

            using var surface = DocumentToSurface.ToSurface(document);
            var containers = surface.Elements.Cast<DrawableContainer>().ToList();
            Assert.Equal(2, containers.Count);
            Assert.All(containers, container => Assert.IsType<ImageContainer>(container));
            containers[0].Left += 5;
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            var mapped = Assert.IsType<PluginElement>(mappedBack.Layers[1].Elements[0]);
            Assert.Equal(plugin with { Area = new RectD(15, 20, 30, 40), Resources = null }, mapped with { Resources = null });
            Assert.Equal(plugin.Resources, mapped.Resources);
            Assert.Equal(linked.Data, mappedBack.Resources[linked.Id].Data);
            Assert.Equal(fallback.Data, mappedBack.Resources[fallback.Id].Data);
            Assert.Equal(newer, mappedBack.Layers[1].Elements[1]);
        }

        [Fact]
        public void ElementIds_SurviveTheEditor()
        {
            var document = WithElements(new Dictionary<Guid, Resource>(),
                new RectangleElement(Uuid7.CreateVersion7(), new RectD(1, 2, 30, 40)),
                new TextElement(Uuid7.CreateVersion7(), new RectD(10, 20, 30, 40), "Greenshot"));

            using var surface = DocumentToSurface.ToSurface(document);
            surface.AddElement(new EllipseContainer(surface), false, false);
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            Assert.Equal(document.Id, mappedBack.Id);
            var ids = mappedBack.Layers[1].Elements.Select(element => element.Id).ToList();
            Assert.Equal(document.Layers[1].Elements.Select(element => element.Id), ids.Take(2));
            // A new element gets a new id
            Assert.DoesNotContain(ids[2], ids.Take(2));
        }

        [Fact]
        public void UnknownElement_IsKeptWithTheSurface()
        {
            var linked = Png(2, 2);
            var unknown = new UnknownElement(Guid.NewGuid(), new RectD(1, 2, 3, 4), "hologram", $"{{\"type\":\"hologram\",\"source\":\"{linked.Id}\"}}");
            var document = WithElements(new Dictionary<Guid, Resource> { [linked.Id] = linked }, unknown);

            using var surface = DocumentToSurface.ToSurface(document);
            Assert.Empty(surface.Elements);
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            Assert.Equal(unknown, Assert.Single(mappedBack.Layers[1].Elements));
            Assert.Equal(linked.Data, mappedBack.Resources[linked.Id].Data);
        }

        public static IEnumerable<object[]> GreenshotFiles() => Directory.GetFiles(Path.Combine(TestDataDirectory, "Greenshotfile"), "*.greenshot", SearchOption.AllDirectories)
            .Select(file => new object[] { file.Substring(TestDataDirectory.Length + 1) });

        /// <summary>
        /// Ids are new on every mapping and the image bytes are encoded again: compare without them, but with the media type
        /// </summary>
        private static object Comparable(GreenshotDocument document, Element element) => element switch
        {
            ImageElement image => (image with { Id = Guid.Empty, Resource = Guid.Empty }, document.Resources[image.Resource].MediaType),
            CursorElement cursor => cursor with { Id = Guid.Empty, Resource = Guid.Empty, MaskResource = cursor.MaskResource.HasValue ? Guid.Empty : (Guid?)null },
            FreehandElement freehand => (freehand with { Id = Guid.Empty, Points = null }, string.Join(";", freehand.Points)),
            _ => element with { Id = Guid.Empty }
        };

        [Theory]
        [MemberData(nameof(GreenshotFiles))]
        public void ToSurface_AndBack_KeepsTheElements(string relativePath)
        {
            using var stream = File.OpenRead(Path.Combine(TestDataDirectory, relativePath));
            var document = LegacyToDocument.ReadGreenshotFile(stream);

            using var surface = DocumentToSurface.ToSurface(document);
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            Assert.Equal(document.Id, mappedBack.Id);
            Assert.Equal(document.Canvas, mappedBack.Canvas);
            Assert.Equal(document.Layers[1].Elements.Select(element => Comparable(document, element)),
                mappedBack.Layers[1].Elements.Select(element => Comparable(mappedBack, element)));
            Assert.Equal(document.Groups.Select(group => (group.Type, group.Properties, string.Join(",", group.Members))),
                mappedBack.Groups.Select(group => (group.Type, group.Properties, string.Join(",", group.Members))));
        }

        [Fact]
        public void StepLabels_AreNumberedInTheOrderOfTheirSeries()
        {
            var first = new StepLabelElement(Guid.NewGuid(), new RectD(10, 10, 30, 30));
            var second = new StepLabelElement(Guid.NewGuid(), new RectD(50, 10, 30, 30));
            var document = WithElements(new Dictionary<Guid, Resource>(), first, second) with
            {
                Groups = new[] { new Group(Guid.NewGuid(), GroupTypes.Steps, new Guid?[] { second.Id, first.Id }) { Properties = new StepNumbering(7).ToProperties() } }
            };

            using var surface = DocumentToSurface.ToSurface(document);
            var containers = surface.Elements.Cast<StepLabelContainer>().ToList();
            Assert.Equal(8, surface.CountStepLabels(containers[0]));
            Assert.Equal(7, surface.CountStepLabels(containers[1]));

            var steps = Assert.Single(SurfaceToDocument.ToDocument(surface).Groups);
            Assert.Equal(7, StepNumbering.Of(steps).Start);
            Assert.Equal(new Guid?[] { second.Id, first.Id }, steps.Members);
        }

        [Fact]
        public void GroupStyle_IsDrawnByTheEditor_UnderTheOwnValues()
        {
            var group = new Group(Guid.NewGuid(), GroupTypes.Style, new Guid?[] { Guid.Empty })
            {
                Style = new Style { Stroke = new StrokeStyle { Color = new Rgba(0, 0, 255, 255), Width = 4 }, Fill = FillStyle.Of(new Rgba(0, 255, 0, 255)) }
            };
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 30, 30)) { Style = new Style { Stroke = new StrokeStyle { Width = 6 } } };
            var document = WithElements(new Dictionary<Guid, Resource>(), rectangle) with { Groups = new[] { group with { Members = new Guid?[] { rectangle.Id } } } };

            using var surface = DocumentToSurface.ToSurface(document);
            var container = (DrawableContainer)Assert.Single(surface.Elements);
            Assert.Equal(6, container.GetFieldValueAsInt(FieldType.LINE_THICKNESS));
            Assert.Equal(Color.FromArgb(255, 0, 0, 255).ToArgb(), ((Color)container.GetFieldValue(FieldType.LINE_COLOR)).ToArgb());
            Assert.Equal(Color.FromArgb(255, 0, 255, 0).ToArgb(), ((Color)container.GetFieldValue(FieldType.FILL_COLOR)).ToArgb());
            // The default of a rectangle
            Assert.True((bool)container.GetFieldValue(FieldType.SHADOW));
        }

        [Fact]
        public void ToImage_DrawsTheElementsOnTheCapture()
        {
            using var white = new Bitmap(100, 50);
            using (var graphics = Graphics.FromImage(white))
            {
                graphics.Clear(Color.White);
            }

            using var png = new MemoryStream();
            white.Save(png, System.Drawing.Imaging.ImageFormat.Png);
            var capture = new Resource(Guid.NewGuid(), "image/png", png.ToArray());
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 20, 20)) { Style = new Style { Fill = FillStyle.Of(new Rgba(255, 0, 0, 255)) } };
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(100, 50), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 100, 50), capture.Id) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, new List<Element> { rectangle })
            }, new Dictionary<Guid, Resource> { [capture.Id] = capture });

            using var image = (Bitmap)DocumentToSurface.ToImage(document);

            Assert.Equal(new Size(100, 50), image.Size);
            Assert.Equal(Color.Red.ToArgb(), image.GetPixel(20, 20).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(60, 40).ToArgb());
        }

        /// <summary>
        /// An EMF with a rectangle, recorded with GDI+
        /// </summary>
        private static byte[] CreateEmf()
        {
            using var stream = new MemoryStream();
            using (var reference = Graphics.FromHwnd(IntPtr.Zero))
            {
                var hdc = reference.GetHdc();
                try
                {
                    using var metafile = new Metafile(stream, hdc, EmfType.EmfPlusDual);
                    using var graphics = Graphics.FromImage(metafile);
                    graphics.FillRectangle(Brushes.Blue, 10, 10, 80, 40);
                }
                finally
                {
                    reference.ReleaseHdc(hdc);
                }
            }

            return stream.ToArray();
        }

        [Fact]
        public void Metafile_KeepsTheEmfFile()
        {
            byte[] emf = CreateEmf();
            using var surface = new Surface(new Bitmap(200, 100));
            var container = Assert.IsType<MetafileContainer>(Assert.Single(new MetaFileFormatHandler().LoadDrawablesFromStream(new MemoryStream(emf), ".emf", surface)));
            surface.AddElement(container, false, false);

            var document = SurfaceToDocument.ToDocument(surface);
            var image = Assert.IsType<ImageElement>(Assert.Single(document.Layers[1].Elements));
            Assert.Equal("image/emf", document.Resources[image.Resource].MediaType);
            Assert.Equal(emf, document.Resources[image.Resource].Data);

            using var mappedSurface = DocumentToSurface.ToSurface(document);
            var mapped = Assert.IsType<MetafileContainer>(Assert.Single(mappedSurface.Elements));
            var mappedBack = SurfaceToDocument.ToDocument(mappedSurface);
            var mappedImage = (ImageElement)Assert.Single(mappedBack.Layers[1].Elements);
            Assert.Equal(emf, mappedBack.Resources[mappedImage.Resource].Data);
            Assert.Equal(new Rectangle(container.Left, container.Top, container.Width, container.Height), new Rectangle(mapped.Left, mapped.Top, mapped.Width, mapped.Height));
        }

        [Theory]
        [InlineData(".jpg", "image/jpeg")]
        [InlineData(".gif", "image/gif")]
        [InlineData(".bmp", "image/bmp")]
        public void Image_KeepsTheFile(string extension, string mediaType)
        {
            byte[] file;
            using (var bitmap = new Bitmap(40, 30))
            using (var stream = new MemoryStream())
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.CornflowerBlue);
                }

                bitmap.Save(stream, extension == ".jpg" ? ImageFormat.Jpeg : extension == ".gif" ? ImageFormat.Gif : ImageFormat.Bmp);
                file = stream.ToArray();
            }

            using var surface = new Surface(new Bitmap(200, 100));
            var container = Assert.IsType<ImageContainer>(Assert.Single(new DefaultFileFormatHandler().LoadDrawablesFromStream(new MemoryStream(file), extension, surface)));
            surface.AddElement(container, false, false);

            var document = SurfaceToDocument.ToDocument(surface);
            var image = Assert.IsType<ImageElement>(Assert.Single(document.Layers[1].Elements));
            Assert.Equal(mediaType, document.Resources[image.Resource].MediaType);
            Assert.Equal(file, document.Resources[image.Resource].Data);

            using var packageStream = new MemoryStream();
            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>();
            GreenshotPackage.Write(document, packageStream, media => registry.GetByMimeType(media)?.PreferredExtension);
            packageStream.Position = 0;
            using var mappedSurface = DocumentToSurface.ToSurface(GreenshotPackage.Read(packageStream));
            Assert.IsType<ImageContainer>(Assert.Single(mappedSurface.Elements));
            var mappedBack = SurfaceToDocument.ToDocument(mappedSurface);
            var mappedImage = (ImageElement)Assert.Single(mappedBack.Layers[1].Elements);
            Assert.Equal(mediaType, mappedBack.Resources[mappedImage.Resource].MediaType);
            Assert.Equal(file, mappedBack.Resources[mappedImage.Resource].Data);
        }
    }
}
