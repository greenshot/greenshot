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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
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
            var captureLayer = new Layer(Uuid7.CreateVersion7(), "Capture", LayerKind.Capture,
                new List<Element> { new ImageElement(Uuid7.CreateVersion7(), new RectD(0, 0, image.Width, image.Height), AddImage(resources, image)) })
            {
                Locked = true
            };
            var containers = surface.Elements.Cast<DrawableContainer>().Select(container => (Container: container, Element: ToElement(container, resources)))
                .Where(pair => pair.Element != null).ToList();
            var elements = containers.Select(pair => pair.Element).ToList();
            if (KeptElements.BySurface.TryGetValue(surface, out var kept))
            {
                elements.AddRange(kept.Select(keptElement => AddKept(resources, keptElement)));
            }

            var annotationsLayer = new Layer(Uuid7.CreateVersion7(), "Annotations", LayerKind.Annotations, elements);

            // The editor has one steps group, numbered in its own order of the step labels
            var steps = containers.Where(pair => pair.Container is StepLabelContainer)
                .OrderBy(pair => surface is Surface editorSurface ? editorSurface.CountStepLabels(pair.Container) : 0).Select(pair => (Guid?)pair.Element.Id).ToList();

            return new GreenshotDocument(surface.ID, new Canvas(image.Width, image.Height), new List<Layer> { captureLayer, annotationsLayer }, resources)
            {
                Groups = steps.Count == 0
                    ? Array.Empty<Group>()
                    : new[] { new Group(Uuid7.CreateVersion7(), GroupTypes.Steps, steps) { Properties = new StepNumbering(surface.CounterStart).ToProperties() } }
            };
        }

        private static Element ToElement(DrawableContainer container, IDictionary<Guid, Resource> resources)
        {
            var id = KeptElements.Ids.TryGetValue(container, out var knownId) ? knownId.Value : Uuid7.CreateVersion7();
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
                        Hotspot = new PointD(cursor.Cursor.HotSpot.X, cursor.Cursor.HotSpot.Y)
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
                    Bold = (bool)container.GetField(FieldType.FONT_BOLD).Value,
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
