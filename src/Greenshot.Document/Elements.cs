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
using System.Linq;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// Something on a layer of the document. Elements are immutable, a change creates a new element with the same id.
    /// </summary>
    public abstract record Element(Guid Id)
    {
        /// <summary>
        /// The values the element sets itself, over the style of its group and the default of its type
        /// </summary>
        public Style Style { get; init; } = Style.Empty;

        /// <summary>
        /// Values of plugins (keyed by plugin id), written back unchanged
        /// </summary>
        public RawJson Extensions { get; init; }

        /// <summary>
        /// Keeps the element at its place relative to another element (the capture when it has no target): for a
        /// box-shaped element its Area, for lines and freehand all their points. The stored position stays absolute.
        /// </summary>
        public Anchor Anchor { get; init; }

        /// <summary>
        /// The area the element covers, without stroke width or shadow. Derived for elements defined by points, a
        /// speech bubble includes its tail.
        /// </summary>
        public abstract RectD Bounds { get; }

        /// <summary>
        /// The point an anchor with the line Point refers to: the centre of Bounds, the end of a line or arrow, the tail
        /// of a speech bubble, the tip of a cursor. Derived, never stored.
        /// </summary>
        public virtual PointD AnchorPoint => new(Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Height / 2);
    }

    public sealed record RectangleElement(Guid Id, RectD Area) : Element(Id)
    {
        public override RectD Bounds => Area;
    }

    public sealed record EllipseElement(Guid Id, RectD Area) : Element(Id)
    {
        public override RectD Bounds => Area;
    }

    public sealed record LineElement(Guid Id, PointD Start, PointD End) : Element(Id)
    {
        public Anchor StartAnchor { get; init; }

        public Anchor EndAnchor { get; init; }

        public override RectD Bounds => RectD.FromPoints(Start, End);

        public override PointD AnchorPoint => End;
    }

    public enum ArrowHeads
    {
        None,
        Start,
        End,
        Both
    }

    public sealed record ArrowElement(Guid Id, PointD Start, PointD End, ArrowHeads Heads = ArrowHeads.End) : Element(Id)
    {
        public Anchor StartAnchor { get; init; }

        public Anchor EndAnchor { get; init; }

        public override RectD Bounds => RectD.FromPoints(Start, End);

        public override PointD AnchorPoint => End;
    }

    public sealed record FreehandElement(Guid Id, IReadOnlyList<PointD> Points) : Element(Id)
    {
        public override RectD Bounds =>
            Points.Count == 0
                ? default
                : RectD.FromPoints(new PointD(Points.Min(p => p.X), Points.Min(p => p.Y)), new PointD(Points.Max(p => p.X), Points.Max(p => p.Y)));
    }

    public sealed record TextElement(Guid Id, RectD Area, string Text) : Element(Id)
    {
        public override RectD Bounds => Area;
    }

    /// <summary>
    /// A text in a bubble, with a tail pointing to Tail.
    /// </summary>
    public sealed record SpeechBubbleElement(Guid Id, RectD Area, string Text, PointD Tail) : Element(Id)
    {
        /// <summary>
        /// Keeps the tail at its place relative to another element, without it the tail moves with the Area
        /// </summary>
        public Anchor TailAnchor { get; init; }

        public override RectD Bounds => RectD.Union(Area, new RectD(Tail.X, Tail.Y, 0, 0));

        public override PointD AnchorPoint => Tail;
    }

    /// <summary>
    /// A numbered label, the number is not stored: it follows from the place of the label in its steps group.
    /// </summary>
    public sealed record StepLabelElement(Guid Id, RectD Area) : Element(Id)
    {
        public override RectD Bounds => Area;
    }

    public enum HighlightMode
    {
        Text,
        Area,
        Grayscale,
        Magnify
    }

    /// <summary>
    /// Highlights an area: Text uses Color, Area darkens (Brightness) and blurs (BlurRadius) everything outside of it,
    /// Magnify uses Magnification.
    /// </summary>
    public sealed record HighlightElement(Guid Id, RectD Area, HighlightMode Mode) : Element(Id)
    {
        public Rgba Color { get; init; } = new(255, 255, 0, 255);

        public double Brightness { get; init; } = 0.9;

        public int BlurRadius { get; init; } = 3;

        public int Magnification { get; init; } = 2;

        public override RectD Bounds => Area;
    }

    public enum ObfuscateMode
    {
        Pixelate,
        Blur
    }

    /// <summary>
    /// Makes an area unreadable, Size is the pixel size or the blur radius.
    /// </summary>
    public sealed record ObfuscateElement(Guid Id, RectD Area, ObfuscateMode Mode, int Size) : Element(Id)
    {
        public override RectD Bounds => Area;
    }

    /// <summary>
    /// How an image is drawn when its size differs from the part of the resource it shows. A renderer maps it to its own modes.
    /// </summary>
    public enum ImageScaling
    {
        /// <summary>Bilinear or bicubic</summary>
        Smooth,

        /// <summary>Nearest neighbour, text and lines of 1 px stay crisp</summary>
        Pixelated
    }

    /// <summary>
    /// An image (bitmap, icon, svg, metafile) shown in Area, the bytes are in the resource with the id Resource.
    /// Several elements can show (parts of) the same resource.
    /// </summary>
    public sealed record ImageElement(Guid Id, RectD Area, Guid Resource) : Element(Id)
    {
        /// <summary>
        /// The part of the resource which is shown, stretched into Area, as a fraction (0..1) of its width and height,
        /// so it works without knowing the size of the image (also for svg and metafiles). Null shows the whole image.
        /// The pixels outside of it are still in the resource.
        /// </summary>
        public RectD? Source { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ImageScaling Scaling { get; init; }

        /// <summary>
        /// Shows the image at this factor of the size of the part it shows (0.5 is half the size), its Area is set to
        /// the result when anchors are applied. It needs the size of the resource.
        /// </summary>
        public double? Scale { get; init; }

        /// <summary>
        /// Rotation in degrees, clockwise, applied after Source is fitted into Area.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Rotation { get; init; }

        public override RectD Bounds => Area;
    }

    public sealed record EmojiElement(Guid Id, RectD Area, string Emoji) : Element(Id)
    {
        /// <summary>
        /// Rotation in degrees, clockwise.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Rotation { get; init; }

        public override RectD Bounds => Area;
    }

    /// <summary>
    /// The captured mouse cursor: Resource is the color layer, MaskResource the optional mask layer.
    /// </summary>
    public sealed record CursorElement(Guid Id, RectD Area, Guid Resource) : Element(Id)
    {
        public Guid? MaskResource { get; init; }

        /// <summary>
        /// The tip of the cursor as a fraction (0..1) of the width and height of Area, so it stays right when Area is resized
        /// </summary>
        public PointD Hotspot { get; init; }

        public override RectD Bounds => Area;

        public override PointD AnchorPoint => new(Area.X + Hotspot.X * Area.Width, Area.Y + Hotspot.Y * Area.Height);

        /// <summary>
        /// The Hotspot of a tip in the pixels of a cursor image of the given size
        /// </summary>
        public static PointD Fraction(double x, double y, double width, double height) =>
            new(width > 0 ? x / width : 0, height > 0 ? y / height : 0);
    }

    public enum CutStyle
    {
        None,
        Line,
        ZigZag,
        Wave,
        Torn
    }

    /// <summary>
    /// Marks where a band of the capture was cut out. The shape of the edge is generated from Seed, so it stays the same.
    /// </summary>
    public sealed record CutMarkElement(Guid Id, RectD Area, bool Horizontal, CutStyle CutStyle, int Seed) : Element(Id)
    {
        public int ToothHeight { get; init; } = 12;

        public int ToothRange { get; init; } = 20;

        public override RectD Bounds => Area;
    }

    [Flags]
    public enum Edges
    {
        None = 0,
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8,
        All = Top | Right | Bottom | Left
    }

    /// <summary>
    /// Room around the capture, in canvas pixels.
    /// </summary>
    public readonly record struct Margins(int Top, int Right, int Bottom, int Left);

    /// <summary>
    /// The frame of the capture in Area: the shape of its edges (straight, zig-zag, wave, torn), with a line and a
    /// shadow from the style. Margins is the room outside of the edges for the shadow. The shape of the edges is
    /// generated from Seed, so it stays the same.
    /// </summary>
    public sealed record FrameElement(Guid Id, RectD Area, CutStyle CutStyle, int Seed) : Element(Id)
    {
        public Edges Edges { get; init; } = Edges.All;

        public Margins Margins { get; init; }

        public int ToothHeight { get; init; } = 12;

        public int ToothRange { get; init; } = 20;

        public override RectD Bounds => Area;
    }

    /// <summary>
    /// An element of a plugin. Kind is a stable name the plugin owns (e.g. "greenshot.barcode"), Version the version of
    /// its Data, which is JSON only the plugin reads. Resources are the resources Data links to, Fallback is a rendered
    /// image of the element, so it can be shown and exported without the plugin.
    /// </summary>
    public sealed record PluginElement(Guid Id, RectD Area, string Kind) : Element(Id)
    {
        public int Version { get; init; }

        public string Data { get; init; }

        public IReadOnlyList<Guid> Resources { get; init; } = Array.Empty<Guid>();

        public Guid? Fallback { get; init; }

        public override RectD Bounds => Area;
    }

    /// <summary>
    /// An element of a type this version doesn't know, e.g. written by a newer Greenshot. Json is the element as it
    /// was read, it is written back unchanged.
    /// </summary>
    public sealed record UnknownElement(Guid Id, RectD Area, string TypeName, string Json) : Element(Id)
    {
        public override RectD Bounds => Area;
    }
}
