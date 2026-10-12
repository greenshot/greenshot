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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Emoji;
using Greenshot.Editor.Drawing.Fields;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Document.Editor
{
    /// <summary>
    /// Creates a GreenshotDocument from a Surface: the image of the surface goes on a capture layer, the elements on an
    /// annotations layer, in the same order. Containers which don't come from the editor (plugins) become PluginElements
    /// with a rendered image of the container as fallback, and the data from the converter of the plugin when there is one.
    /// Elements DocumentToSurface kept (plugins which aren't installed, unknown types) are written back unchanged.
    /// </summary>
    public static class SurfaceToDocument
    {
        public static GreenshotDocument ToDocument(ISurface surface)
        {
            var resources = new Dictionary<Guid, Resource>();
            var image = surface.Image;
            var kept = KeptElements.DocumentOf(surface);
            // While the editor has the image it got for a canvas which isn't just the capture, the capture layers and the canvas come back as they were
            bool standIn = kept.StandIn != null && ReferenceEquals(image, kept.StandIn);
            var captureResource = standIn ? Guid.Empty : AddImage(resources, image);
            var captureLayers = standIn
                ? kept.CaptureLayers.ToList()
                : new List<Layer>
                {
                    new(kept.CaptureLayerId, "Capture", LayerKind.Capture, new List<Element> { new ImageElement(kept.CaptureId, new RectD(0, 0, image.Width, image.Height), captureResource) })
                    {
                        Locked = true
                    }
                };
            if (standIn)
            {
                foreach (var resource in kept.CaptureResources)
                {
                    resources[resource.Id] = resource;
                }
            }

            var origin = standIn ? kept.Origin : default;
            var containers = surface.Elements.Cast<DrawableContainer>()
                .Select(container => (Container: container, Element: WithAnchors(ToElement(container, resources), container) is Element element ? Anchors.Move(element, origin.X, origin.Y) : null))
                .Where(pair => pair.Element != null).ToList();
            var elements = containers.Select(pair => pair.Element).ToList();
            elements.AddRange(kept.Elements.Select(keptElement => AddKept(resources, keptElement)));

            var annotationsLayer = new Layer(kept.AnnotationsLayerId, "Annotations", LayerKind.Annotations, elements);

            // The editor has one steps group, numbered in its own order of the step labels; the other groups are kept
            var steps = containers.Where(pair => pair.Container is StepLabelContainer)
                .OrderBy(pair => surface is Surface editorSurface ? editorSurface.CountStepLabels(pair.Container) : 0).Select(pair => (Guid?)pair.Element.Id).ToList();
            var keptSteps = kept.Groups.FirstOrDefault(group => group.Type == GroupTypes.Steps);
            var groups = kept.Groups.Where(group => group.Type != GroupTypes.Steps).ToList();
            if (steps.Count > 0)
            {
                groups.Insert(0, (keptSteps ?? new Group(Uuid7.CreateVersion7(), GroupTypes.Steps, steps)) with
                {
                    Members = steps,
                    Properties = new StepNumbering(surface.CounterStart, keptSteps == null ? StepFormat.Numbers : StepNumbering.Of(keptSteps).Format).ToProperties()
                });
            }

            var analysis = kept.Analysis.Where(pair => resources.ContainsKey(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!standIn && AnalysisOf(surface, kept) is { } captureAnalysis)
            {
                analysis[captureResource] = captureAnalysis;
            }

            return new GreenshotDocument(surface.ID, standIn ? kept.Canvas : new Canvas(), captureLayers.Append(annotationsLayer).ToList(), resources)
            {
                Groups = groups,
                Analysis = analysis,
                Extensions = kept.Extensions
            };
        }

        /// <summary>
        /// The element with the anchors and extensions of the element the container was made from, the editor doesn't know them
        /// </summary>
        private static Element WithAnchors(Element element, DrawableContainer container)
        {
            if (element == null || !KeptElements.Originals.TryGetValue(container, out var original))
            {
                return element;
            }

            element = element with { Anchor = original.Anchor, Extensions = original.Extensions, Style = element.Style with { Text = KeepText(element.Style.Text, original.Style.Text) } };
            return (element, original) switch
            {
                (LineElement line, LineElement from) => line with { StartAnchor = from.StartAnchor, EndAnchor = from.EndAnchor },
                (ArrowElement arrow, ArrowElement from) => arrow with { StartAnchor = from.StartAnchor, EndAnchor = from.EndAnchor },
                (SpeechBubbleElement bubble, SpeechBubbleElement from) => bubble with { TailAnchor = from.TailAnchor },
                // The editor gets a rotated image turned in its bitmap, the part of it is cut out there
                (ImageElement image, ImageElement from) => image with
                {
                    Source = from.Rotation == 0 ? from.Source : null,
                    Scaling = from.Scaling,
                    // Resized in the editor: the size is the one it has now
                    Scale = Math.Abs(image.Area.Width - from.Area.Width) < 1 && Math.Abs(image.Area.Height - from.Area.Height) < 1 ? from.Scale : null
                },
                _ => element
            };
        }

        // The editor has one family, bold or not and none of the other text values: they are kept from the original
        private static TextStyle KeepText(TextStyle text, TextStyle original)
        {
            if (text == null || original == null)
            {
                return text;
            }

            return text with
            {
                Family = original.Family != null && DocumentToSurface.FontFamilyOf(original.Family) == text.Family ? original.Family : text.Family,
                Weight = original.Weight != null && original.Weight >= 600 == text.Weight >= 600 ? original.Weight : text.Weight,
                Underline = original.Underline,
                Strikeout = original.Strikeout,
                LineHeight = original.LineHeight,
                Wrap = original.Wrap,
                Outline = original.Outline
            };
        }

        private static Element ToElement(DrawableContainer container, IDictionary<Guid, Resource> resources)
        {
            var id = KeptElements.Originals.TryGetValue(container, out var original) ? original.Id : Uuid7.CreateVersion7();
            var area = new RectD(container.Left, container.Top, container.Width, container.Height);
            var start = new PointD(container.Left, container.Top);
            var end = new PointD(container.Left + container.Width, container.Top + container.Height);

            Element element;
            if (KeptElements.ByContainer.TryGetValue(container, out var kept))
            {
                // Moved or resized, but otherwise as it was read
                return AddKept(resources, kept) is PluginElement plugin ? plugin with { Area = area } : kept.Element;
            }

            if (container.GetType().Assembly != typeof(DrawableContainer).Assembly)
            {
                var drawingBounds = container.DrawingBounds;
                var converter = SimpleServiceProvider.Current.GetAllInstances<IPluginElementConverter>().FirstOrDefault(pluginConverter => pluginConverter.CanConvert(container));
                var plugin = new PluginElement(id, new RectD(drawingBounds.X, drawingBounds.Y, drawingBounds.Width, drawingBounds.Height), converter?.Kind ?? container.GetType().FullName)
                {
                    Version = converter?.Version ?? 0,
                    Fallback = AddRendering(resources, container)
                };
                return converter == null ? plugin : converter.ToElement(container, plugin, resources);
            }
            else
            {
                element = container switch
                {
                    // Derived types first
                    SpeechbubbleContainer speechbubble => new SpeechBubbleElement(id, area, speechbubble.Text,
                        new PointD(speechbubble.TargetAdorner.Location.X, speechbubble.TargetAdorner.Location.Y)),
                    TextContainer text => new TextElement(id, area, text.Text),
                    HighlightContainer highlight => ToHighlight(id, area, highlight),
                    ObfuscateContainer obfuscate => (PreparedFilter)obfuscate.GetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE) == PreparedFilter.BLUR
                        ? new ObfuscateElement(id, area, ObfuscateMode.Blur, IntField(obfuscate, FieldType.BLUR_RADIUS))
                        : new ObfuscateElement(id, area, ObfuscateMode.Pixelate, IntField(obfuscate, FieldType.PIXEL_SIZE)),
                    RectangleContainer => new RectangleElement(id, area),
                    EllipseContainer => new EllipseElement(id, area),
                    ArrowContainer arrow => new ArrowElement(id, start, end, (ArrowHeads)Convert.ToInt32(arrow.GetFieldValue(FieldType.ARROWHEADS))),
                    LineContainer => new LineElement(id, start, end),
                    FreehandContainer freehand => new FreehandElement(id, freehand.CapturePoints.Select(p => new PointD(p.X, p.Y)).ToList()),
                    StepLabelContainer => new StepLabelElement(id, area),
                    // The file the image came from when it is known, otherwise PNG
                    ImageContainer imageContainer => new ImageElement(id, area, imageContainer.OriginalData == null
                        ? AddImage(resources, imageContainer.Image)
                        : AddResource(resources, imageContainer.OriginalMediaType, imageContainer.OriginalData)),
                    IconContainer icon => new ImageElement(id, area, AddIcon(resources, icon.Icon)),
                    SvgContainer svg => new ImageElement(id, area, AddResource(resources, "image/svg+xml", svg.SvgData)) { Rotation = svg.RotationAngle },
                    // The EMF/WMF file when it is known, GDI+ can't write EMF/WMF so otherwise the metafile is kept as PNG in its own size
                    MetafileContainer metafile => new ImageElement(id, area, metafile.MetafileData == null
                        ? AddImage(resources, metafile.Metafile)
                        : AddResource(resources, metafile.Metafile.GetMetafileHeader().IsWmf() ? "image/wmf" : "image/emf", metafile.MetafileData)) { Rotation = metafile.RotationAngle },
                    EmojiContainer emoji => new EmojiElement(id, area, emoji.Emoji) { Rotation = emoji.RotationAngle },
                    CursorContainer cursor => new CursorElement(id, area, AddImage(resources, cursor.Cursor.ColorLayer))
                    {
                        MaskResource = cursor.Cursor.MaskLayer == null ? null : AddImage(resources, cursor.Cursor.MaskLayer),
                        Hotspot = CursorElement.Fraction(cursor.Cursor.HotSpot.X, cursor.Cursor.HotSpot.Y, cursor.Cursor.Size.Width, cursor.Cursor.Size.Height)
                    },
                    CutMarkContainer cutMark => new CutMarkElement(id, area, cutMark.IsHorizontal, (CutStyle)Convert.ToInt32(cutMark.GetFieldValue(FieldType.CUT_MARK_STYLE)), cutMark.Seed)
                    {
                        ToothHeight = cutMark.ToothHeight,
                        ToothRange = cutMark.ToothRange
                    },
                    TornEdgeContainer tornEdge => new TornEdgeElement(id, area, (CutStyle)(int)tornEdge.Style, tornEdge.Seed)
                    {
                        Edges = ToEdges(tornEdge.Edges),
                        Margins = new Margins(tornEdge.Margins[0], tornEdge.Margins[1], tornEdge.Margins[2], tornEdge.Margins[3]),
                        ToothHeight = tornEdge.ToothHeight,
                        ToothRange = tornEdge.ToothRange
                    },
                    // The crop container is a tool, not part of the document
                    _ => null
                };
            }
            return element == null ? null : element with { Style = ToStyle(container) };
        }

        private static HighlightElement ToHighlight(Guid id, RectD area, HighlightContainer highlight) =>
            (PreparedFilter)highlight.GetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT) switch
            {
                PreparedFilter.AREA_HIGHLIGHT => new HighlightElement(id, area, HighlightMode.Area)
                {
                    Brightness = highlight.GetField(FieldType.BRIGHTNESS).Value is double brightness ? brightness : 0.9,
                    BlurRadius = IntField(highlight, FieldType.BLUR_RADIUS)
                },
                PreparedFilter.GRAYSCALE => new HighlightElement(id, area, HighlightMode.Grayscale),
                PreparedFilter.MAGNIFICATION => new HighlightElement(id, area, HighlightMode.Magnify) { Magnification = IntField(highlight, FieldType.MAGNIFICATION_FACTOR) },
                _ => new HighlightElement(id, area, HighlightMode.Text) { Color = ToRgba((Color)highlight.GetField(FieldType.FILL_COLOR).Value) }
            };

        /// <summary>
        /// The full style from the fields of the container (and its filters), the editor has no groups: a part is there
        /// when the container has its fields, so a stroke with width 0 keeps the color, which is also the color of the text.
        /// </summary>
        private static Style ToStyle(DrawableContainer container)
        {
            StrokeStyle stroke = container.HasField(FieldType.LINE_COLOR) && container.HasField(FieldType.LINE_THICKNESS)
                ? StrokeStyle.Of(ToRgba((Color)container.GetField(FieldType.LINE_COLOR).Value), IntField(container, FieldType.LINE_THICKNESS))
                : null;
            FillStyle fill = container is not HighlightContainer && container.HasField(FieldType.FILL_COLOR)
                ? FillStyle.Of(ToRgba((Color)container.GetField(FieldType.FILL_COLOR).Value))
                : null;
            var shadow = new ShadowStyle { Enabled = container.HasField(FieldType.SHADOW) && (bool)container.GetField(FieldType.SHADOW).Value };
            TextStyle text = container.HasField(FieldType.FONT_FAMILY)
                ? new TextStyle
                {
                    Enabled = true,
                    Family = (string)container.GetField(FieldType.FONT_FAMILY).Value,
                    Size = Convert.ToDouble(container.GetField(FieldType.FONT_SIZE).Value),
                    Weight = (bool)container.GetField(FieldType.FONT_BOLD).Value ? 700 : 400,
                    Italic = (bool)container.GetField(FieldType.FONT_ITALIC).Value,
                    Color = stroke?.Color ?? Rgba.Transparent,
                    Horizontal = (TextAlignment)Convert.ToInt32(container.GetField(FieldType.TEXT_HORIZONTAL_ALIGNMENT).Value),
                    Vertical = (TextAlignment)Convert.ToInt32(container.GetField(FieldType.TEXT_VERTICAL_ALIGNMENT).Value)
                }
                : null;
            return new Style { Stroke = stroke, Fill = fill, Shadow = shadow, Text = text };
        }

        // GetField of the container (not of AbstractFieldHolder) also finds the fields of its filters
        private static int IntField(DrawableContainer container, IFieldType fieldType) => Convert.ToInt32(container.GetField(fieldType).Value);

        private static Rgba ToRgba(Color color) => Rgba.FromArgb(color.ToArgb());

        private static Edges ToEdges(bool[] edges) =>
            (edges[0] ? Edges.Top : Edges.None) | (edges[1] ? Edges.Right : Edges.None) | (edges[2] ? Edges.Bottom : Edges.None) | (edges[3] ? Edges.Left : Edges.None);

        private static Element AddKept(IDictionary<Guid, Resource> resources, KeptElements.Kept kept)
        {
            foreach (var resource in kept.Resources)
            {
                resources[resource.Id] = resource;
            }

            return kept.Element;
        }

        private static Guid AddRendering(IDictionary<Guid, Resource> resources, DrawableContainer container)
        {
            var bounds = container.DrawingBounds;
            using var bitmap = new Bitmap(Math.Max(1, bounds.Width), Math.Max(1, bounds.Height), PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.TranslateTransform(-bounds.X, -bounds.Y);
                container.Draw(graphics, RenderMode.EXPORT);
            }
            return AddImage(resources, bitmap);
        }

        /// <summary>
        /// The OCR text and barcodes the editor has for its image, null when there are none
        /// </summary>
        private static ImageAnalysis AnalysisOf(ISurface surface, KeptElements.KeptDocument kept)
        {
            if (surface.CaptureDetails == null)
            {
                return kept.CaptureAnalysis;
            }

            List<IDetectedFeature> features;
            lock (surface.CaptureDetails.Features)
            {
                features = surface.CaptureDetails.Features.Where(feature => feature is IOcrLineFeature or IBarcodeFeature).ToList();
            }

            if (kept.CaptureAnalysis != null && features.SequenceEqual(kept.RestoredFeatures))
            {
                return kept.CaptureAnalysis;
            }

            var lines = features.OfType<IOcrLineFeature>()
                .Select(line => new OcrLine(line.Text, ToRectD(line.Bounds), line.Words.Select(word => new OcrWord(word.Text, ToRectD(word.Bounds))).ToList())).ToList();
            var barcodes = features.OfType<IBarcodeFeature>()
                .Select(barcode => new BarcodeResult(barcode.Format, barcode.RawText, BarcodeResult.Corners(ToRectD(barcode.Bounds)))).ToList();
            var analysis = (kept.CaptureAnalysis ?? new ImageAnalysis()) with
            {
                Ocr = lines.Count == 0 ? null : new OcrResult(lines),
                Barcodes = barcodes.Count == 0 ? null : barcodes
            };
            return analysis.IsEmpty ? null : analysis;
        }

        private static RectD ToRectD(NativeRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

        private static Guid AddImage(IDictionary<Guid, Resource> resources, Image image)
        {
            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            return AddResource(resources, "image/png", stream.ToArray());
        }

        private static Guid AddIcon(IDictionary<Guid, Resource> resources, Icon icon)
        {
            using var stream = new MemoryStream();
            icon.Save(stream);
            return AddResource(resources, "image/x-icon", stream.ToArray());
        }

        private static Guid AddResource(IDictionary<Guid, Resource> resources, string mediaType, byte[] data)
        {
            var resource = new Resource(Uuid7.CreateVersion7(), mediaType, data);
            resources.Add(resource.Id, resource);
            return resource.Id;
        }
    }
}
