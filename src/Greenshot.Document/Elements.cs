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

namespace Greenshot.Document
{
    /// <summary>
    /// Something on a layer of the document. Elements are immutable, a change creates a new element with the same id.
    /// </summary>
    public abstract record Element(Guid Id)
    {
        public ElementStyle Style { get; init; } = ElementStyle.None;

        /// <summary>
        /// The area the element covers, without stroke width or shadow. Derived for elements defined by points.
        /// </summary>
        public abstract RectD Bounds { get; }
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
        public override RectD Bounds => RectD.FromPoints(Start, End);
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
        public override RectD Bounds => RectD.FromPoints(Start, End);
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
        public override RectD Bounds => Area;
    }

    /// <summary>
    /// A numbered label, the number is not stored: it follows from the order of the step labels and the CounterStart of the canvas.
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
    /// An image (bitmap, icon, svg, metafile) shown in Area, the bytes are in the resource with the id Resource.
    /// </summary>
    public sealed record ImageElement(Guid Id, RectD Area, Guid Resource) : Element(Id)
    {
        /// <summary>
        /// Rotation in degrees, clockwise.
        /// </summary>
        public int Rotation { get; init; }

        public override RectD Bounds => Area;
    }

    public sealed record EmojiElement(Guid Id, RectD Area, string Emoji) : Element(Id)
    {
        /// <summary>
        /// Rotation in degrees, clockwise.
        /// </summary>
        public int Rotation { get; init; }

        public override RectD Bounds => Area;
    }

    /// <summary>
    /// The captured mouse cursor: Resource is the color layer, MaskResource the optional mask layer.
    /// </summary>
    public sealed record CursorElement(Guid Id, RectD Area, Guid Resource) : Element(Id)
    {
        public Guid? MaskResource { get; init; }

        public PointD Hotspot { get; init; }

        public override RectD Bounds => Area;
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
    /// Torn (or otherwise cut) edges of the capture in Area, Margins is the room outside of the edges for the shadow.
    /// The shape of the edges is generated from Seed, so it stays the same.
    /// </summary>
    public sealed record TornEdgeElement(Guid Id, RectD Area, CutStyle CutStyle, int Seed) : Element(Id)
    {
        public Edges Edges { get; init; } = Edges.All;

        public Margins Margins { get; init; }

        public int ToothHeight { get; init; } = 12;

        public int ToothRange { get; init; } = 20;

        public override RectD Bounds => Area;
    }

    /// <summary>
    /// An element this version doesn't know, for example from a plugin which isn't installed.
    /// TypeName identifies it, Fallback is an optional resource with a rendered image of it.
    /// </summary>
    public sealed record UnknownElement(Guid Id, RectD Area, string TypeName) : Element(Id)
    {
        public Guid? Fallback { get; init; }

        public override RectD Bounds => Area;
    }
}
