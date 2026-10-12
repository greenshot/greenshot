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
using System.Linq;

namespace Greenshot.Document.Legacy
{
    /// <summary>
    /// Creates a GreenshotDocument from a legacy .greenshot file, or the elements of a legacy .gst template.
    /// The fields of the old containers become typed styles, the same way as the editor maps a Surface (Greenshot.Document.Editor).
    /// </summary>
    public static class LegacyToDocument
    {
        /// <summary>
        /// Read a .greenshot file of Greenshot 1.2 - 1.4: the PNG goes on a capture layer, the elements on an annotations layer
        /// </summary>
        /// <param name="stream">Seekable stream with the complete file</param>
        /// <exception cref="LegacyFormatException">When the stream isn't a legacy .greenshot file or the elements can't be read</exception>
        public static GreenshotDocument ReadGreenshotFile(Stream stream)
        {
            if (!LegacyGreenshotFile.TryLocate(stream, out var file))
            {
                throw new LegacyFormatException("This is not a .greenshot file of Greenshot 1.2 - 1.4.");
            }

            var png = new byte[file.ElementsOffset];
            stream.Seek(0, SeekOrigin.Begin);
            if (stream.Read(png, 0, png.Length) != png.Length || !IsPng(png))
            {
                throw new LegacyFormatException("The .greenshot file doesn't start with a PNG.");
            }

            var legacyElements = file.ReadElements(stream);
            var resources = new Dictionary<Guid, Resource>();
            // Width and height of the PNG are in the IHDR chunk, big endian
            int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            var captureLayer = new Layer(Guid.NewGuid(), "Capture", LayerKind.Capture,
                new List<Element> { new ImageElement(Guid.NewGuid(), new RectD(0, 0, width, height), AddResource(resources, png)) })
            {
                Locked = true
            };
            var annotationsLayer = new Layer(Guid.NewGuid(), "Annotations", LayerKind.Annotations, ToElements(legacyElements, resources));
            int counterStart = legacyElements.OfType<LegacyStepLabelElement>().Select(stepLabel => stepLabel.CounterStart).FirstOrDefault();

            return new GreenshotDocument(Guid.NewGuid(), new Canvas(width, height) { CounterStart = counterStart == 0 ? 1 : counterStart },
                new List<Layer> { captureLayer, annotationsLayer }, resources);
        }

        /// <summary>
        /// Read the elements of a .gst template of Greenshot 1.2 - 1.4, the resources they reference are added to resources
        /// </summary>
        /// <exception cref="LegacyFormatException">When the elements can't be read</exception>
        public static IReadOnlyList<Element> ReadTemplate(Stream stream, IDictionary<Guid, Resource> resources) =>
            ToElements(LegacyElementReader.Read(stream), resources);

        private static IReadOnlyList<Element> ToElements(IEnumerable<LegacyElement> legacyElements, IDictionary<Guid, Resource> resources) =>
            legacyElements.Select(legacyElement => ToElement(legacyElement, resources)).Where(element => element != null).ToList();

        private static Element ToElement(LegacyElement legacy, IDictionary<Guid, Resource> resources)
        {
            var id = Guid.NewGuid();
            var area = new RectD(legacy.Left, legacy.Top, legacy.Width, legacy.Height);
            var start = new PointD(legacy.Left, legacy.Top);
            var end = new PointD(legacy.Left + legacy.Width, legacy.Top + legacy.Height);

            Element element = legacy switch
            {
                LegacySpeechbubbleElement speechbubble => new SpeechBubbleElement(id, area, speechbubble.Text,
                    new PointD(speechbubble.TargetGripperLocation.X, speechbubble.TargetGripperLocation.Y)),
                LegacyTextElement text => new TextElement(id, area, text.Text),
                LegacyStepLabelElement => new StepLabelElement(id, area),
                LegacyFreehandElement freehand => new FreehandElement(id, freehand.Points.Select(p => new PointD(p.X, p.Y)).ToList()),
                LegacyImageElement image => new ImageElement(id, area, AddResource(resources, image.ImageData)),
                LegacyIconElement icon => new ImageElement(id, area, AddResource(resources, icon.IconData)),
                LegacySvgElement svg => new ImageElement(id, area, AddResource(resources, svg.SvgData, "image/svg+xml")) { Rotation = svg.RotationAngle },
                LegacyMetafileElement metafile => new ImageElement(id, area, AddResource(resources, metafile.ImageData)) { Rotation = metafile.RotationAngle },
                LegacyEmojiElement emoji => new EmojiElement(id, area, emoji.Emoji) { Rotation = emoji.RotationAngle },
                // 1.3 stored a Cursor which can't be read, those have no color layer
                LegacyCursorElement { ColorLayerData: not null } cursor => new CursorElement(id, area, AddResource(resources, cursor.ColorLayerData))
                {
                    MaskResource = cursor.MaskLayerData == null ? null : AddResource(resources, cursor.MaskLayerData),
                    Hotspot = new PointD(cursor.HotspotX, cursor.HotspotY)
                },
                LegacyCutMarkElement cutMark => new CutMarkElement(id, area, cutMark.Horizontal, (CutStyle)Int(legacy, "CUT_MARK_STYLE", (int)CutStyle.Torn), cutMark.Seed)
                {
                    ToothHeight = Int(legacy, "TOOTH_HEIGHT", 12),
                    ToothRange = Int(legacy, "TOOTH_RANGE", 20)
                },
                LegacyTornEdgeElement tornEdge => new TornEdgeElement(id, area, (CutStyle)Int(legacy, "CUT_MARK_STYLE", (int)CutStyle.Torn), tornEdge.Seed)
                {
                    Edges = ToEdges(tornEdge.Edges),
                    Margins = tornEdge.Margins is { Count: 4 } margins ? new Margins(margins[0], margins[1], margins[2], margins[3]) : default,
                    ToothHeight = Int(legacy, "TOOTH_HEIGHT", 12),
                    ToothRange = Int(legacy, "TOOTH_RANGE", 20)
                },
                _ => legacy.Kind switch
                {
                    LegacyElementKind.Rectangle => new RectangleElement(id, area),
                    LegacyElementKind.Ellipse => new EllipseElement(id, area),
                    LegacyElementKind.Line => new LineElement(id, start, end),
                    LegacyElementKind.Arrow => new ArrowElement(id, start, end, (ArrowHeads)Int(legacy, "ARROWHEADS", (int)ArrowHeads.End)),
                    LegacyElementKind.Highlight => ToHighlight(id, area, legacy),
                    LegacyElementKind.Obfuscate => Int(legacy, "PREPARED_FILTER_OBFUSCATE", PreparedFilterPixelize) == PreparedFilterBlur
                        ? new ObfuscateElement(id, area, ObfuscateMode.Blur, Int(legacy, "BLUR_RADIUS", 3))
                        : new ObfuscateElement(id, area, ObfuscateMode.Pixelate, Int(legacy, "PIXEL_SIZE", 5)),
                    // Plugin data can't be read without the plugin's types, only which type it was
                    LegacyElementKind.Unknown => new PluginElement(id, area, legacy.TypeName),
                    _ => null
                }
            };
            return element == null ? null : element with { Style = ToStyle(legacy) };
        }

        // Values of FilterContainer.PreparedFilter
        private const int PreparedFilterBlur = 0;
        private const int PreparedFilterPixelize = 1;
        private const int PreparedFilterAreaHighlight = 3;
        private const int PreparedFilterGrayscale = 4;
        private const int PreparedFilterMagnification = 5;

        private static HighlightElement ToHighlight(Guid id, RectD area, LegacyElement legacy) =>
            Int(legacy, "PREPARED_FILTER_HIGHLIGHT", 2) switch
            {
                PreparedFilterAreaHighlight => new HighlightElement(id, area, HighlightMode.Area)
                {
                    Brightness = Find(legacy, "BRIGHTNESS") is IConvertible brightness ? Convert.ToDouble(brightness) : 0.9,
                    BlurRadius = Int(legacy, "BLUR_RADIUS", 3)
                },
                PreparedFilterGrayscale => new HighlightElement(id, area, HighlightMode.Grayscale),
                PreparedFilterMagnification => new HighlightElement(id, area, HighlightMode.Magnify) { Magnification = Int(legacy, "MAGNIFICATION_FACTOR", 2) },
                _ => new HighlightElement(id, area, HighlightMode.Text) { Color = Find(legacy, "FILL_COLOR") is LegacyColor color ? ToRgba(color) : new Rgba(255, 255, 0, 255) }
            };

        /// <summary>
        /// The same rules as the editor mapping: a part is there when the element (or one of its filters) has its fields
        /// </summary>
        private static ElementStyle ToStyle(LegacyElement legacy)
        {
            // Like the editor, which adds a missing line thickness of 0 to step labels of older files
            Stroke stroke = Find(legacy, "LINE_COLOR") is LegacyColor lineColor
                ? new Stroke(ToRgba(lineColor), Int(legacy, "LINE_THICKNESS", 0))
                : null;
            Fill fill = legacy.Kind != LegacyElementKind.Highlight && Find(legacy, "FILL_COLOR") is LegacyColor fillColor ? new Fill(ToRgba(fillColor)) : null;
            Shadow shadow = Find(legacy, "SHADOW") is true ? Shadow.Default : null;
            TextStyle text = Find(legacy, "FONT_FAMILY") is string family
                ? new TextStyle(new FontSpec(family, Find(legacy, "FONT_SIZE") is IConvertible size ? Convert.ToDouble(size) : 11, Find(legacy, "FONT_BOLD") is true, Find(legacy, "FONT_ITALIC") is true),
                    stroke?.Color ?? Rgba.Transparent,
                    (TextAlignment)Int(legacy, "TEXT_HORIZONTAL_ALIGNMENT", (int)TextAlignment.Center),
                    (TextAlignment)Int(legacy, "TEXT_VERTICAL_ALIGNMENT", (int)TextAlignment.Center))
                : null;
            return new ElementStyle(stroke, fill, shadow, text);
        }

        /// <summary>
        /// The value of the field of the element, or of one of its filters. Some files have a field twice, like the
        /// editor the last one wins.
        /// </summary>
        private static object Find(LegacyElement legacy, string fieldTypeName) =>
            (legacy.Fields.LastOrDefault(field => field.FieldTypeName == fieldTypeName)
             ?? legacy.Filters.SelectMany(filter => filter.Fields).LastOrDefault(field => field.FieldTypeName == fieldTypeName))?.Value;

        private static int Int(LegacyElement legacy, string fieldTypeName, int defaultValue) =>
            Find(legacy, fieldTypeName) switch
            {
                LegacyEnumValue enumValue => (int)enumValue.Value,
                IConvertible value and not string => Convert.ToInt32(value),
                _ => defaultValue
            };

        private static Rgba ToRgba(LegacyColor color) =>
            color.IsArgb ? Rgba.FromArgb(color.ToArgb())
            : color.IsKnownColor && color.KnownColor > 0 && color.KnownColor <= KnownColorNames.Length
                ? Rgba.FromArgb(System.Drawing.Color.FromName(KnownColorNames[color.KnownColor - 1]).ToArgb())
            : color.IsNamedColor ? Rgba.FromArgb(System.Drawing.Color.FromName(color.Name).ToArgb())
            : Rgba.Transparent;

        /// <summary>
        /// The names of System.Drawing.KnownColor 1 - 174 in the order of the enum, the enum isn't part of netstandard2.0
        /// </summary>
        private static readonly string[] KnownColorNames =
        {
            "ActiveBorder", "ActiveCaption", "ActiveCaptionText", "AppWorkspace", "Control", "ControlDark", "ControlDarkDark", "ControlLight",
            "ControlLightLight", "ControlText", "Desktop", "GrayText", "Highlight", "HighlightText", "HotTrack", "InactiveBorder", "InactiveCaption",
            "InactiveCaptionText", "Info", "InfoText", "Menu", "MenuText", "ScrollBar", "Window", "WindowFrame", "WindowText", "Transparent",
            "AliceBlue", "AntiqueWhite", "Aqua", "Aquamarine", "Azure", "Beige", "Bisque", "Black", "BlanchedAlmond", "Blue", "BlueViolet", "Brown",
            "BurlyWood", "CadetBlue", "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "Crimson", "Cyan", "DarkBlue", "DarkCyan",
            "DarkGoldenrod", "DarkGray", "DarkGreen", "DarkKhaki", "DarkMagenta", "DarkOliveGreen", "DarkOrange", "DarkOrchid", "DarkRed", "DarkSalmon",
            "DarkSeaGreen", "DarkSlateBlue", "DarkSlateGray", "DarkTurquoise", "DarkViolet", "DeepPink", "DeepSkyBlue", "DimGray", "DodgerBlue",
            "Firebrick", "FloralWhite", "ForestGreen", "Fuchsia", "Gainsboro", "GhostWhite", "Gold", "Goldenrod", "Gray", "Green", "GreenYellow",
            "Honeydew", "HotPink", "IndianRed", "Indigo", "Ivory", "Khaki", "Lavender", "LavenderBlush", "LawnGreen", "LemonChiffon", "LightBlue",
            "LightCoral", "LightCyan", "LightGoldenrodYellow", "LightGray", "LightGreen", "LightPink", "LightSalmon", "LightSeaGreen", "LightSkyBlue",
            "LightSlateGray", "LightSteelBlue", "LightYellow", "Lime", "LimeGreen", "Linen", "Magenta", "Maroon", "MediumAquamarine", "MediumBlue",
            "MediumOrchid", "MediumPurple", "MediumSeaGreen", "MediumSlateBlue", "MediumSpringGreen", "MediumTurquoise", "MediumVioletRed",
            "MidnightBlue", "MintCream", "MistyRose", "Moccasin", "NavajoWhite", "Navy", "OldLace", "Olive", "OliveDrab", "Orange", "OrangeRed",
            "Orchid", "PaleGoldenrod", "PaleGreen", "PaleTurquoise", "PaleVioletRed", "PapayaWhip", "PeachPuff", "Peru", "Pink", "Plum", "PowderBlue",
            "Purple", "Red", "RosyBrown", "RoyalBlue", "SaddleBrown", "Salmon", "SandyBrown", "SeaGreen", "SeaShell", "Sienna", "Silver", "SkyBlue",
            "SlateBlue", "SlateGray", "Snow", "SpringGreen", "SteelBlue", "Tan", "Teal", "Thistle", "Tomato", "Turquoise", "Violet", "Wheat", "White",
            "WhiteSmoke", "Yellow", "YellowGreen", "ButtonFace", "ButtonHighlight", "ButtonShadow", "GradientActiveCaption", "GradientInactiveCaption",
            "MenuBar", "MenuHighlight"
        };

        private static Edges ToEdges(IReadOnlyList<bool> edges) =>
            edges is not { Count: 4 }
                ? Edges.All
                : (edges[0] ? Edges.Top : Edges.None) | (edges[1] ? Edges.Right : Edges.None) | (edges[2] ? Edges.Bottom : Edges.None) | (edges[3] ? Edges.Left : Edges.None);

        private static bool StartsWith(byte[] data, params byte[] prefix) => data.Length > prefix.Length && !prefix.Where((value, index) => data[index] != value).Any();

        private static bool IsPng(byte[] data) => data.Length > 24 && data[0] == 0x89 && data[1] == (byte)'P' && data[2] == (byte)'N' && data[3] == (byte)'G';

        /// <summary>
        /// Add the bytes as resource, the media type is taken from the bytes when it isn't given
        /// </summary>
        private static Guid AddResource(IDictionary<Guid, Resource> resources, byte[] data, string mediaType = null)
        {
            mediaType ??= data == null ? "application/octet-stream"
                : IsPng(data) ? "image/png"
                : data.Length > 4 && data[0] == 0 && data[1] == 0 && data[2] == 1 && data[3] == 0 ? "image/x-icon"
                : StartsWith(data, 0xFF, 0xD8, 0xFF) ? "image/jpeg"
                : StartsWith(data, (byte)'G', (byte)'I', (byte)'F', (byte)'8') ? "image/gif"
                : StartsWith(data, (byte)'B', (byte)'M') ? "image/bmp"
                : StartsWith(data, (byte)'I', (byte)'I', 42, 0) || StartsWith(data, (byte)'M', (byte)'M', 0, 42) ? "image/tiff"
                : "application/octet-stream";
            var resource = new Resource(Guid.NewGuid(), mediaType, data);
            resources.Add(resource.Id, resource);
            return resource.Id;
        }
    }
}
