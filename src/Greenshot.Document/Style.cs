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

namespace Greenshot.Document
{
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
        /// A full stroke, a width of 0 draws no line
        /// </summary>
        public static StrokeStyle Of(Rgba color, double width) => new() { Enabled = width > 0, Color = color, Width = width };

        public StrokeStyle Over(StrokeStyle lower) => lower == null ? this : new()
        {
            Enabled = Enabled ?? lower.Enabled,
            Color = Color ?? lower.Color,
            Width = Width ?? lower.Width
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
    /// The drop shadow, the renderer decides how it looks.
    /// </summary>
    public sealed record ShadowStyle
    {
        public bool? Enabled { get; init; }

        public ShadowStyle Over(ShadowStyle lower) => lower == null ? this : new() { Enabled = Enabled ?? lower.Enabled };
    }

    public enum TextAlignment
    {
        Near,
        Center,
        Far
    }

    public sealed record TextStyle
    {
        public bool? Enabled { get; init; }

        public string Family { get; init; }

        public double? Size { get; init; }

        public bool? Bold { get; init; }

        public bool? Italic { get; init; }

        public Rgba? Color { get; init; }

        public TextAlignment? Horizontal { get; init; }

        public TextAlignment? Vertical { get; init; }

        public TextStyle Over(TextStyle lower) => lower == null ? this : new()
        {
            Enabled = Enabled ?? lower.Enabled,
            Family = Family ?? lower.Family,
            Size = Size ?? lower.Size,
            Bold = Bold ?? lower.Bold,
            Italic = Italic ?? lower.Italic,
            Color = Color ?? lower.Color,
            Horizontal = Horizontal ?? lower.Horizontal,
            Vertical = Vertical ?? lower.Vertical
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
        private const string SansSerif = "Microsoft Sans Serif";

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
                Enabled = true, Family = SansSerif, Size = size, Bold = bold, Italic = false, Color = style.Stroke.Color,
                Horizontal = TextAlignment.Center, Vertical = TextAlignment.Center
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
            CutMarkElement or TornEdgeElement => Lines(new Rgba(105, 105, 105, 255), 0, true, Transparent),
            _ => new Style { Shadow = new ShadowStyle { Enabled = false } }
        };
    }
}
