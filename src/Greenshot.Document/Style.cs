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
using System.Linq;

namespace Greenshot.Document
{
    /// <summary>
    /// Where a line lies on the outline it follows: centred on it, within the shape or around it
    /// </summary>
    public enum StrokePosition
    {
        Center,
        Inside,
        Outside
    }

    /// <summary>
    /// How lines are drawn. Every value is optional: a value which isn't set is inherited.
    /// </summary>
    public sealed record StrokeStyle
    {
        /// <summary>
        /// false draws no line, even when the style below has one
        /// </summary>
        public bool? Enabled { get; init; }

        public Rgba? Color { get; init; }

        public double? Width { get; init; }

        /// <summary>
        /// Where the line lies on the outline, Center when not set. Open shapes (line, arrow, freehand) have no inside
        /// or outside, they draw it centred; a renderer which can't draw it draws it centred, the value is kept.
        /// </summary>
        public StrokePosition? Position { get; init; }

        /// <summary>
        /// A full stroke, a width of 0 draws no line
        /// </summary>
        public static StrokeStyle Of(Rgba color, double width) => new() { Enabled = width > 0, Color = color, Width = width };

        public StrokeStyle Over(StrokeStyle lower) => lower == null ? this : new()
        {
            Enabled = Enabled ?? lower.Enabled,
            Color = Color ?? lower.Color,
            Width = Width ?? lower.Width,
            Position = Position ?? lower.Position
        };
    }

    /// <summary>
    /// How an area is filled.
    /// </summary>
    public sealed record FillStyle
    {
        public bool? Enabled { get; init; }

        public Rgba? Color { get; init; }

        /// <summary>
        /// A full fill, a transparent color fills nothing
        /// </summary>
        public static FillStyle Of(Rgba color) => new() { Enabled = color.A != 0, Color = color };

        public FillStyle Over(FillStyle lower) => lower == null ? this : new()
        {
            Enabled = Enabled ?? lower.Enabled,
            Color = Color ?? lower.Color
        };
    }

    /// <summary>
    /// The drop shadow, what isn't set the renderer decides.
    /// </summary>
    public sealed record ShadowStyle
    {
        public bool? Enabled { get; init; }

        /// <summary>
        /// How far the shadow fades out, in canvas pixels
        /// </summary>
        public double? Size { get; init; }

        /// <summary>
        /// 0..1
        /// </summary>
        public double? Darkness { get; init; }

        public PointD? Offset { get; init; }

        public ShadowStyle Over(ShadowStyle lower) => lower == null ? this : new()
        {
            Enabled = Enabled ?? lower.Enabled,
            Size = Size ?? lower.Size,
            Darkness = Darkness ?? lower.Darkness,
            Offset = Offset ?? lower.Offset
        };
    }

    /// <summary>
    /// Where text lies in its area, start is left or top (right for right-to-left text)
    /// </summary>
    public enum TextAlignment
    {
        Start,
        Center,
        End
    }

    /// <summary>
    /// How text is drawn.
    /// </summary>
    public sealed record TextStyle
    {
        /// <summary>
        /// The generic families which can end the Family list
        /// </summary>
        public const string SansSerif = "sans-serif", Serif = "serif", Monospace = "monospace";

        public bool? Enabled { get; init; }

        /// <summary>
        /// Font families separated by commas, the first one which is available is used, e.g. "Segoe UI, Arial, sans-serif".
        /// A list can end with a generic family: sans-serif, serif or monospace.
        /// </summary>
        public string Family { get; init; }

        /// <summary>
        /// The size in canvas pixels (not points)
        /// </summary>
        public double? Size { get; init; }

        /// <summary>
        /// 100 - 900, 400 is normal, 700 is bold
        /// </summary>
        public int? Weight { get; init; }

        public bool? Italic { get; init; }

        public bool? Underline { get; init; }

        public bool? Strikeout { get; init; }

        public Rgba? Color { get; init; }

        public TextAlignment? Horizontal { get; init; }

        public TextAlignment? Vertical { get; init; }

        /// <summary>
        /// The distance of lines as a multiple of the font size, without it the font decides
        /// </summary>
        public double? LineHeight { get; init; }

        /// <summary>
        /// Text which is too long for its Area continues on the next line
        /// </summary>
        public bool? Wrap { get; init; }

        /// <summary>
        /// A line around each letter, so text on a screenshot stays readable on any background
        /// </summary>
        public StrokeStyle Outline { get; init; }

        /// <summary>
        /// The families of Family, in order
        /// </summary>
        public static string[] Families(string family) =>
            (family ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(name => name.Trim().Trim('"', '\'')).Where(name => name.Length > 0).ToArray();

        public TextStyle Over(TextStyle lower) => lower == null ? this : new()
        {
            Enabled = Enabled ?? lower.Enabled,
            Family = Family ?? lower.Family,
            Size = Size ?? lower.Size,
            Weight = Weight ?? lower.Weight,
            Italic = Italic ?? lower.Italic,
            Underline = Underline ?? lower.Underline,
            Strikeout = Strikeout ?? lower.Strikeout,
            Color = Color ?? lower.Color,
            Horizontal = Horizontal ?? lower.Horizontal,
            Vertical = Vertical ?? lower.Vertical,
            LineHeight = LineHeight ?? lower.LineHeight,
            Wrap = Wrap ?? lower.Wrap,
            Outline = Outline?.Over(lower.Outline) ?? lower.Outline
        };
    }

    /// <summary>
    /// The style of an element or of a group. Every part and value is optional: what isn't set is inherited, from the
    /// group and then from the built-in default of the element type. Only a whole part can be switched off (Enabled = false).
    /// </summary>
    public sealed record Style
    {
        public static readonly Style Empty = new();

        public StrokeStyle Stroke { get; init; }

        public FillStyle Fill { get; init; }

        public ShadowStyle Shadow { get; init; }

        public TextStyle Text { get; init; }

        /// <summary>
        /// Values of plugins (keyed by plugin id), written back unchanged
        /// </summary>
        public RawJson Extensions { get; init; }

        /// <summary>
        /// This style, with every value it doesn't set taken from the lower style
        /// </summary>
        public Style Over(Style lower) => lower == null ? this : new()
        {
            Stroke = Stroke?.Over(lower.Stroke) ?? lower.Stroke,
            Fill = Fill?.Over(lower.Fill) ?? lower.Fill,
            Shadow = Shadow?.Over(lower.Shadow) ?? lower.Shadow,
            Text = Text?.Over(lower.Text) ?? lower.Text,
            Extensions = Extensions ?? lower.Extensions
        };
    }

    /// <summary>
    /// The built-in style of each element type, the lowest level when a style is resolved. Part of the format: a value
    /// changes only with a new version of document.json, whose reader writes the old value into older documents.
    /// These are the defaults of the editor of Greenshot 1.3.
    /// </summary>
    public static class StyleDefaults
    {
        private static readonly Rgba Red = new(255, 0, 0, 255);
        private static readonly Rgba Transparent = Rgba.Transparent;
        private const string SansSerif = "Microsoft Sans Serif, sans-serif";

        private static Style Lines(Rgba color, double width, bool shadow, Rgba? fill = null) => new()
        {
            Stroke = StrokeStyle.Of(color, width),
            Fill = fill.HasValue ? FillStyle.Of(fill.Value) : null,
            Shadow = new ShadowStyle { Enabled = shadow }
        };

        private static Style WithText(Style style, double size, bool bold) => style with
        {
            Text = new TextStyle
            {
                Enabled = true, Family = SansSerif, Size = size, Weight = bold ? 700 : 400, Italic = false, Underline = false, Strikeout = false,
                Color = style.Stroke.Color, Horizontal = TextAlignment.Center, Vertical = TextAlignment.Center, Wrap = true, Outline = new StrokeStyle { Enabled = false }
            }
        };

        public static Style For(Element element) => element switch
        {
            RectangleElement or EllipseElement or ArrowElement => Lines(Red, 2, true, Transparent),
            LineElement => Lines(Red, 2, true),
            FreehandElement => Lines(Red, 3, false),
            TextElement => WithText(Lines(Red, 2, true, Transparent), 11, false),
            SpeechBubbleElement => WithText(Lines(new Rgba(0, 0, 255, 255), 2, false, new Rgba(255, 255, 255, 255)), 20, true),
            // The number is drawn in the line color
            StepLabelElement => Lines(new Rgba(255, 255, 255, 255), 0, false, new Rgba(139, 0, 0, 255)) with { Text = new TextStyle { Color = new Rgba(255, 255, 255, 255) } },
            HighlightElement or ObfuscateElement => Lines(Red, 0, false),
            CutMarkElement or FrameElement => Lines(new Rgba(105, 105, 105, 255), 0, true, Transparent),
            _ => new Style { Shadow = new ShadowStyle { Enabled = false } }
        };
    }
}
