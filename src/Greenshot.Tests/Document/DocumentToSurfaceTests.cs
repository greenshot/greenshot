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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
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
            return new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>
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
            Assert.Equal(linked.Data.ToArray(), mappedBack.Resources[linked.Id].Data.ToArray());
            Assert.Equal(fallback.Data.ToArray(), mappedBack.Resources[fallback.Id].Data.ToArray());
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
        public void TheIdsOfTheCaptureAndTheLayers_SurviveTheEditor()
        {
            var document = WithElements(new Dictionary<Guid, Resource>());

            using var surface = DocumentToSurface.ToSurface(document);
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            Assert.Equal(document.Layers.Select(layer => layer.Id), mappedBack.Layers.Select(layer => layer.Id));
            Assert.Equal(document.Layers[0].Elements[0].Id, mappedBack.Layers[0].Elements[0].Id);
            Assert.Equal(Anchors.CaptureId(document), Anchors.CaptureId(mappedBack));

            // A surface which didn't come from a document keeps the ids it got first
            using var capture = new Surface(new Bitmap(20, 10));
            Assert.Equal(Anchors.CaptureId(SurfaceToDocument.ToDocument(capture)), Anchors.CaptureId(SurfaceToDocument.ToDocument(capture)));
        }

        [Fact]
        public void AnchorsExtensionsAndGroups_SurviveTheEditor()
        {
            var box = new RectangleElement(Uuid7.CreateVersion7(), new RectD(10, 10, 30, 30))
            {
                Anchor = new Anchor { X = new AxisAnchor(AnchorLine.End, AnchorLine.End, -10) },
                Extensions = new RawJson("{\"test\":1}")
            };
            var arrow = new ArrowElement(Uuid7.CreateVersion7(), new PointD(50, 50), new PointD(90, 60)) { EndAnchor = new Anchor { Target = box.Id, X = new AxisAnchor(AnchorLine.Start) } };
            var style = new Group(Uuid7.CreateVersion7(), GroupTypes.Style, new Guid?[] { box.Id, arrow.Id }) { Name = "Red", Style = new Style { Stroke = StrokeStyle.Of(new Rgba(255, 0, 0, 255), 3) } };
            var png = Png(40, 20);
            var image = new ImageElement(Uuid7.CreateVersion7(), new RectD(100, 10, 40, 40), png.Id) { Source = new RectD(0.5, 0, 0.5, 1), Scaling = ImageScaling.Pixelated };
            var document = WithElements(new Dictionary<Guid, Resource> { [png.Id] = png }, box, arrow, image) with { Groups = new[] { style } };

            using var surface = DocumentToSurface.ToSurface(document);
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            var elements = mappedBack.Layers[1].Elements;
            Assert.Equal(box.Anchor, elements[0].Anchor);
            Assert.Equal(box.Extensions, elements[0].Extensions);
            Assert.Equal(arrow.EndAnchor, Assert.IsType<ArrowElement>(elements[1]).EndAnchor);
            var keptImage = Assert.IsType<ImageElement>(elements[2]);
            Assert.Equal(image.Source, keptImage.Source);
            Assert.Equal(image.Scaling, keptImage.Scaling);
            var keptStyle = Assert.Single(mappedBack.Groups);
            Assert.Equal(style with { Members = null }, keptStyle with { Members = null });
            Assert.Equal(style.Members, keptStyle.Members);
        }

        [Fact]
        public void Place_PutsTheElementsOfARecipeRelativeToTheCapture()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var label = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 50, 20))
            {
                Anchor = new Anchor { X = new AxisAnchor(AnchorLine.End, AnchorLine.End, -10), Y = new AxisAnchor(AnchorLine.End, AnchorLine.End, -10) }
            };

            var placed = Assert.IsType<RectangleElement>(Assert.Single(DocumentToSurface.Place(surface, new[] { label })));

            Assert.Equal(new RectD(140, 70, 50, 20), placed.Area);
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
            Assert.Equal(linked.Data.ToArray(), mappedBack.Resources[linked.Id].Data.ToArray());
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
            var document = new GreenshotDocument(Guid.NewGuid(), new Canvas(), new List<Layer>
            {
                new(Guid.NewGuid(), "Capture", LayerKind.Capture, new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, 100, 50), capture.Id) }),
                new(Guid.NewGuid(), "Annotations", LayerKind.Annotations, new List<Element> { rectangle })
            }, new Dictionary<Guid, Resource> { [capture.Id] = capture });

            using var image = (Bitmap)DocumentToSurface.ToImage(document);

            Assert.Equal(new Size(100, 50), image.Size);
            Assert.Equal(Color.Red.ToArgb(), image.GetPixel(20, 20).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(60, 40).ToArgb());
        }

        [Fact]
        public void ACanvasAroundTheCapture_GoesToTheEditorAsOneImage_AndComesBackAsItWas()
        {
            var png = Png(200, 100);
            var captureLayer = new Layer(Uuid7.CreateVersion7(), "Capture", LayerKind.Capture, new List<Element>
            {
                new ImageElement(Uuid7.CreateVersion7(), new RectD(0, 0, 100, 50), png.Id) { Scale = 0.5 }
            }) { Locked = true };
            var footer = new TextElement(Uuid7.CreateVersion7(), new RectD(0, 0, 80, 20), "Figure 1")
            {
                Anchor = new Anchor { X = new AxisAnchor(AnchorLine.Center, AnchorLine.Center), Y = new AxisAnchor(AnchorLine.End, AnchorLine.Start, 10) }
            };
            // The editor can't show it, it comes back where it was
            var rounded = new FrameElement(Uuid7.CreateVersion7(), new RectD(0, 0, 50, 25), 1) { CornerRadius = 4 };
            var document = new GreenshotDocument(Uuid7.CreateVersion7(), new Canvas { Fit = CanvasFit.Content, Padding = 20, Background = new Rgba(255, 255, 255, 255) }, new List<Layer>
            {
                captureLayer,
                new(Uuid7.CreateVersion7(), "Annotations", LayerKind.Annotations, new List<Element> { footer, rounded })
            }, new Dictionary<Guid, Resource> { [png.Id] = png });

            using var surface = DocumentToSurface.ToSurface(document);
            var mappedBack = SurfaceToDocument.ToDocument(surface);

            // The canvas is the capture and the footer below it, with 20 px around them
            Assert.Equal(new Size(140, 120), surface.Image.Size);
            var container = surface.Elements.Cast<DrawableContainer>().Single();
            Assert.Equal((30, 80, 80, 20), (container.Left, container.Top, container.Width, container.Height));
            Assert.Equal(document.Canvas, mappedBack.Canvas);
            Assert.Equal(captureLayer with { Elements = null }, mappedBack.Layers[0] with { Elements = null });
            Assert.Equal(captureLayer.Elements, mappedBack.Layers[0].Elements);
            Assert.Equal(new RectD(10, 60, 80, 20), mappedBack.Layers[1].Elements[0].Bounds);
            Assert.Equal(rounded, mappedBack.Layers[1].Elements[1]);
            Assert.Equal(png.Data.ToArray(), mappedBack.Resources[png.Id].Data.ToArray());
        }

        [Fact]
        public void TheAnalysisOfTheCapture_IsShownAsFeatures_AndComesBack()
        {
            var document = WithElements(new Dictionary<Guid, Resource>());
            var capture = (ImageElement)document.Layers[0].Elements[0];
            var analysis = new ImageAnalysis
            {
                Ocr = new OcrResult(new[] { new OcrLine("Save", new RectD(10, 20, 40, 12), new[] { new OcrWord("Save", new RectD(10, 20, 40, 12)) }) })
                {
                    Engine = "Windows.Media.Ocr", Language = "en-US"
                },
                Barcodes = new[] { new BarcodeResult("QR_CODE", "https://getgreenshot.org", new[] { new PointD(100, 10), new PointD(150, 15), new PointD(145, 65), new PointD(95, 60) }) }
            };
            document = document with { Analysis = new Dictionary<Guid, ImageAnalysis> { [capture.Resource] = analysis } };

            using var surface = DocumentToSurface.ToSurface(document);
            var features = surface.CaptureDetails.Features;
            Assert.Equal(("Save", new NativeRect(10, 20, 40, 12)), features.OfType<IOcrLineFeature>().Select(line => (line.Text, line.Bounds)).Single());
            Assert.Equal("https://getgreenshot.org", features.OfType<IBarcodeFeature>().Single().RawText);

            // Unchanged in the editor: the analysis comes back as it was, with engine and corner points
            var unchanged = SurfaceToDocument.ToDocument(surface);
            Assert.Same(analysis, unchanged.Analysis[((ImageElement)unchanged.Layers[0].Elements[0]).Resource]);

            // OCR again in the editor: the new text, the barcode from its bounds
            lock (features)
            {
                features.RemoveAll(feature => feature is IOcrLineFeature);
                features.Add(new DetectedOcrLine(new NativeRect(1, 2, 3, 4), "Cancel", new List<OcrWordInfo>()));
            }

            var changed = SurfaceToDocument.ToDocument(surface).Analysis.Values.Single();
            Assert.Equal("Cancel", changed.Ocr.Lines.Single().Text);
            Assert.Null(changed.Ocr.Engine);
            Assert.Equal(new RectD(95, 10, 55, 55), changed.Barcodes.Single().Bounds);
        }

        [Fact]
        public void AFrame_IsTheEditorsTornEdge_AndComesBack_OneWithRoundedCornersIsKept()
        {
            var frame = new FrameElement(Guid.NewGuid(), new RectD(10, 5, 180, 90), 7)
            {
                Shape = new EdgeShape(EdgeStyle.Wave) { Height = 6, Range = 16 },
                Sides = FrameSides.Except(Edges.Bottom | Edges.Right),
                Style = new Style { Shadow = new ShadowStyle { Enabled = true, Size = 4, Darkness = 0.5, Offset = new PointD(2, 3) } }
            };
            var rounded = new FrameElement(Guid.NewGuid(), new RectD(10, 5, 180, 90), 8) { CornerRadius = 8 };

            using var surface = DocumentToSurface.ToSurface(WithElements(new Dictionary<Guid, Resource>(), frame, rounded));
            var container = Assert.IsType<TornEdgeContainer>(Assert.Single(surface.Elements));
            Assert.Equal(new[] { 5, 10, 5, 10 }, container.Margins);
            Assert.Equal(new[] { false, true, true, false }, container.Edges);
            Assert.Equal(CutMarkStyle.Wave, container.Style);
            Assert.Equal(4, container.GetSettings().ShadowSize);

            var elements = SurfaceToDocument.ToDocument(surface).Layers[1].Elements;
            var back = Assert.IsType<FrameElement>(elements[0]);
            Assert.Equal(frame.Area, back.Area);
            Assert.Equal(frame.Shape, back.Shape);
            Assert.Equal(frame.Sides, back.Sides);
            Assert.Equal(7, back.Seed);
            Assert.Equal(new PointD(2, 3), back.Style.Shadow.Offset);
            Assert.Equal(0.5, back.Style.Shadow.Darkness.Value, 3);
            Assert.Equal(rounded, elements[1]);
        }

        [Fact]
        public void AnUnchangedCapture_ComesBackAsItWas_AChangedOneIsEncoded()
        {
            var document = WithElements(new Dictionary<Guid, Resource>());
            var capture = document.Resources[((ImageElement)document.Layers[0].Elements[0]).Resource];

            using var surface = DocumentToSurface.ToSurface(document);
            var unchanged = SurfaceToDocument.ToDocument(surface);
            Assert.Same(capture, unchanged.Resources[((ImageElement)unchanged.Layers[0].Elements[0]).Resource]);

            surface.Image = new Bitmap(200, 100);
            var changed = SurfaceToDocument.ToDocument(surface);
            Assert.NotEqual(capture.Id, ((ImageElement)changed.Layers[0].Elements[0]).Resource);
        }

        [Fact]
        public void TheMetadata_GivesTheEditorItsTitle_AndComesBack()
        {
            var metadata = new DocumentMetadata
            {
                Title = "Checkout dialog",
                Created = new DateTimeOffset(2026, 10, 12, 16, 20, 0, TimeSpan.FromHours(2)),
                Session = new SessionInfo(Guid.NewGuid()) { Name = "PROD-1042" }
            };
            var document = WithElements(new Dictionary<Guid, Resource>()) with { Metadata = metadata };

            using var surface = DocumentToSurface.ToSurface(document);
            Assert.Equal("Checkout dialog", surface.CaptureDetails.Title);
            Assert.Equal(metadata.Created.Value.LocalDateTime, surface.CaptureDetails.DateTime);

            surface.CaptureDetails.Title = "Payment dialog";
            var written = SurfaceToDocument.ToDocument(surface).Metadata;
            Assert.Equal("Payment dialog", written.Title);
            Assert.Equal(metadata.Created, written.Created);
            Assert.Same(metadata.Session, written.Session);
            Assert.StartsWith("Greenshot ", written.Generator);
        }

        [Fact]
        public void TheCaptureDetails_BecomeTheMetadata_OfADocumentWithoutAny()
        {
            using var surface = DocumentToSurface.ToSurface(WithElements(new Dictionary<Guid, Resource>()));
            surface.CaptureDetails.CaptureMode = CaptureMode.Window;
            surface.CaptureDetails.Title = "Checkout";

            var metadata = SurfaceToDocument.ToDocument(surface).Metadata;
            Assert.Equal("Checkout", metadata.Title);
            Assert.Equal(SourceTypes.Capture, metadata.Source.Type);
            Assert.Equal("window", metadata.Source.Mode);
            Assert.Equal("Checkout", metadata.Source.Window.Title);
            Assert.Equal(new DateTimeOffset(surface.CaptureDetails.DateTime), metadata.Source.Date);

            surface.CaptureDetails.CaptureMode = CaptureMode.File;
            surface.CaptureDetails.AddMetaData("file", Path.Combine("C:", "Users", "someone", "checkout.png"));
            Assert.Equal("checkout.png", SurfaceToDocument.ToDocument(surface).Metadata.Source.Name);
        }

        [Fact]
        public void ToImage_DrawsThePartOfTheResource_Pixelated()
        {
            // Two pixels, red and blue
            using var bitmap = new Bitmap(2, 1);
            bitmap.SetPixel(0, 0, Color.Red);
            bitmap.SetPixel(1, 0, Color.Blue);
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            var pixels = new Resource(Guid.NewGuid(), "image/png", stream.ToArray());
            var whole = new ImageElement(Guid.NewGuid(), new RectD(10, 10, 40, 20), pixels.Id) { Scaling = ImageScaling.Pixelated };
            var blue = new ImageElement(Guid.NewGuid(), new RectD(60, 10, 40, 20), pixels.Id) { Source = new RectD(0.5, 0, 0.5, 1), Scaling = ImageScaling.Pixelated };
            var document = WithElements(new Dictionary<Guid, Resource> { [pixels.Id] = pixels }, whole, blue);

            using var image = (Bitmap)DocumentToSurface.ToImage(document);

            Assert.Equal(Color.Red.ToArgb(), image.GetPixel(15, 20).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), image.GetPixel(45, 20).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), image.GetPixel(65, 20).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), image.GetPixel(95, 20).ToArgb());
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
        public void Metafile_BecomesAPng()
        {
            using var surface = new Surface(new Bitmap(200, 100));
            var container = Assert.IsType<MetafileContainer>(Assert.Single(new MetaFileFormatHandler().LoadDrawablesFromStream(new MemoryStream(CreateEmf()), ".emf", surface)));
            surface.AddElement(container, false, false);

            var document = SurfaceToDocument.ToDocument(surface);
            var image = Assert.IsType<ImageElement>(Assert.Single(document.Layers[1].Elements));
            Assert.Equal("image/png", document.Resources[image.Resource].MediaType);

            using var mappedSurface = DocumentToSurface.ToSurface(document);
            var mapped = Assert.IsType<ImageContainer>(Assert.Single(mappedSurface.Elements));
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
            Assert.Equal(file, document.Resources[image.Resource].Data.ToArray());

            using var packageStream = new MemoryStream();
            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>();
            GreenshotPackage.Write(document, packageStream, media => registry.GetByMimeType(media)?.PreferredExtension);
            packageStream.Position = 0;
            using var mappedSurface = DocumentToSurface.ToSurface(GreenshotPackage.Read(packageStream));
            Assert.IsType<ImageContainer>(Assert.Single(mappedSurface.Elements));
            var mappedBack = SurfaceToDocument.ToDocument(mappedSurface);
            var mappedImage = (ImageElement)Assert.Single(mappedBack.Layers[1].Elements);
            Assert.Equal(mediaType, mappedBack.Resources[mappedImage.Resource].MediaType);
            Assert.Equal(file, mappedBack.Resources[mappedImage.Resource].Data.ToArray());
        }
    }
}
