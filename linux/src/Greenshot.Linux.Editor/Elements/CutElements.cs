/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Text.Json.Serialization;
using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Elements;

/// <summary>
/// An element which cuts a part out of the image: the cut edges of a crop out (<see cref="CutMarkElement"/>) and the
/// torn edges (<see cref="TornEdgeElement"/>). The cut part is transparent or the fill color, with the shadow of the torn
/// edge effect cast by what is left of the image; where cuts overlap the shadow is drawn once, by the cut drawn last.
/// The edges come from a stored seed, so they only change when new random edges are asked for. The CutShadow of Windows.
/// </summary>
public abstract class CutElement : DrawableElement, IDisposable
{
    private static readonly IReadOnlyList<FieldType> CutFields =
    [
        FieldType.CutMarkStyle, FieldType.ToothHeight, FieldType.ToothRange, FieldType.FillColor, FieldType.Shadow, FieldType.LineThickness, FieldType.LineColor,
    ];

    private SKBitmap? _shadowCache;
    private string? _shadowCacheKey;

    protected CutElement()
    {
        Seed = CutOut.NewSeed(0);
    }

    public int Seed { get; set; }

    public float ShadowDarkness { get; set; } = 0.6f;

    public int ShadowSize { get; set; } = 7;

    public int ShadowOffsetX { get; set; } = -1;

    public int ShadowOffsetY { get; set; } = -1;

    /// <summary>
    /// The size of the image, the cut spans it
    /// </summary>
    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    [JsonIgnore]
    public override IReadOnlyList<FieldType> Fields => CutFields;

    [JsonIgnore]
    public int ToothHeight => Math.Max(1, Style.ToothHeight);

    [JsonIgnore]
    public int ToothRange => Math.Max(2, Style.ToothRange);

    /// <summary>
    /// The cut part is (partly) transparent, the image needs an alpha channel
    /// </summary>
    [JsonIgnore]
    public bool HasTransparentCut => Style.FillColor.Alpha < 255;

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 0,
        LineColor = new SKColor(105, 105, 105),
        FillColor = SKColors.Transparent,
        Shadow = true,
        CutMarkStyle = CutMarkStyle.Torn,
        ToothHeight = 12,
        ToothRange = 20,
    };

    /// <summary>
    /// Make new random edges
    /// </summary>
    public void NewSeed() => Seed = CutOut.NewSeed(Seed);

    /// <summary>
    /// Take the tooth sizes, shadow and color of the settings
    /// </summary>
    public virtual void ApplySettings(TornEdgeSettings settings)
    {
        ShadowDarkness = settings.Darkness;
        ShadowSize = Math.Max(1, settings.ShadowSize);
        ShadowOffsetX = settings.ShadowOffset.X;
        ShadowOffsetY = settings.ShadowOffset.Y;
        Style.ToothHeight = Math.Max(1, settings.ToothHeight);
        Style.ToothRange = Math.Max(2, settings.HorizontalToothRange);
        Style.Shadow = settings.GenerateShadow;
        Style.FillColor = settings.BackgroundColor;
        if (settings.Seed != 0)
        {
            Seed = settings.Seed;
        }
    }

    /// <summary>
    /// The settings for the settings window
    /// </summary>
    public virtual TornEdgeSettings GetSettings() => new()
    {
        ToothHeight = ToothHeight,
        HorizontalToothRange = ToothRange,
        VerticalToothRange = ToothRange,
        Darkness = ShadowDarkness,
        ShadowSize = ShadowSize,
        ShadowOffset = new SKPointI(ShadowOffsetX, ShadowOffsetY),
        GenerateShadow = Style.Shadow,
        BackgroundColor = Style.FillColor,
        Seed = Seed,
    };

    /// <summary>
    /// The room needed on every side for the shadow of the settings
    /// </summary>
    public static int GetShadowMargin(TornEdgeSettings settings) =>
        settings.GenerateShadow ? Math.Max(1, settings.ShadowSize) + Math.Max(Math.Abs(settings.ShadowOffset.X), Math.Abs(settings.ShadowOffset.Y)) : 0;

    public override void FitToImage(int width, int height)
    {
        ImageWidth = width;
        ImageHeight = height;
        SpanImage();
    }

    /// <summary>
    /// Keep the bounds over the whole image along the cut
    /// </summary>
    protected abstract void SpanImage();

    /// <summary>
    /// The part cut out of the image, null when nothing is cut
    /// </summary>
    public abstract SKPath? CreateCutPath();

    /// <summary>
    /// The edges to draw a line along
    /// </summary>
    protected abstract IReadOnlyList<SKPoint[]> CreateEdgeLines();

    /// <summary>
    /// Changes when the cut part changes, for the shadow cache of all cuts
    /// </summary>
    [JsonIgnore]
    public abstract string ShapeKey { get; }

    public override void Draw(RenderContext context)
    {
        using var cutPath = CreateCutPath();
        if (cutPath == null)
        {
            return;
        }

        var canvas = context.Canvas;
        // Replace the pixels: transparent shows the checkerboard of the editor and is transparent when exported
        using (var fill = new SKPaint { Color = Style.FillColor, BlendMode = SKBlendMode.Src, IsAntialias = true })
        {
            canvas.DrawPath(cutPath, fill);
        }

        var cuts = context.Elements.OfType<CutElement>().ToList();
        if (!cuts.Contains(this))
        {
            cuts.Add(this);
        }

        if (Style.Shadow)
        {
            DrawShadow(canvas, cutPath, cuts);
        }

        if (Style.LineThickness > 0 && Style.LineColor.Alpha > 0)
        {
            canvas.Save();
            // The edges are part of the image, where another cut took the image away they don't show
            foreach (var other in cuts.Where(c => c != this))
            {
                using var otherPath = other.CreateCutPath();
                if (otherPath != null)
                {
                    canvas.ClipPath(otherPath, SKClipOperation.Difference, antialias: true);
                }
            }

            using var pen = Painting.Stroke(Style.LineColor, Style.LineThickness);
            pen.StrokeJoin = SKStrokeJoin.Round;
            foreach (var line in CreateEdgeLines())
            {
                using var path = new SKPath();
                path.AddPoly(line, close: false);
                canvas.DrawPath(path, pen);
            }

            canvas.Restore();
        }
    }

    /// <summary>
    /// The shadow of what is left of the image, in this cut minus the cuts drawn after it
    /// </summary>
    private void DrawShadow(SKCanvas canvas, SKPath cutPath, List<CutElement> cuts)
    {
        if (ImageWidth <= 0 || ImageHeight <= 0)
        {
            return;
        }

        string key = $"{ImageWidth}x{ImageHeight}|{ShadowDarkness}|{ShadowSize}|{ShadowOffsetX},{ShadowOffsetY}|{string.Join(";", cuts.Select(c => c.ShapeKey))}";
        if (_shadowCache == null || _shadowCacheKey != key)
        {
            ClearShadowCache();
            _shadowCache = CreateShadow(cuts);
            _shadowCacheKey = key;
        }

        canvas.Save();
        canvas.ClipPath(cutPath, antialias: true);
        for (int i = cuts.IndexOf(this) + 1; i < cuts.Count; i++)
        {
            using var later = cuts[i].CreateCutPath();
            if (later != null)
            {
                canvas.ClipPath(later, SKClipOperation.Difference, antialias: true);
            }
        }

        canvas.DrawBitmap(_shadowCache, 0, 0);
        canvas.Restore();
    }

    /// <summary>
    /// The mask of the image without all cuts, darkened and blurred like the drop shadow, at the offset of the shadow
    /// </summary>
    private SKBitmap CreateShadow(IEnumerable<CutElement> cuts)
    {
        using var mask = ImageEffects.NewBitmap(ImageWidth, ImageHeight);
        using (var maskCanvas = new SKCanvas(mask))
        {
            maskCanvas.Clear(SKColors.Black);
            using var clear = new SKPaint { Color = SKColors.Transparent, BlendMode = SKBlendMode.Src, IsAntialias = true };
            foreach (var cut in cuts)
            {
                using var path = cut.CreateCutPath();
                if (path != null)
                {
                    maskCanvas.DrawPath(path, clear);
                }
            }
        }

        var shadow = ImageEffects.NewBitmap(ImageWidth, ImageHeight);
        using var shadowCanvas = new SKCanvas(shadow);
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(
            [
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 0, ShadowDarkness, 0,
            ]),
            ImageFilter = SKImageFilter.CreateBlur(ShadowSize / 2f, ShadowSize / 2f),
        };
        // Where the drop shadow effect puts the shadow, relative to the image
        shadowCanvas.DrawBitmap(mask, 1 - ShadowOffsetX, 1 - ShadowOffsetY, paint);
        return shadow;
    }

    private void ClearShadowCache()
    {
        _shadowCache?.Dispose();
        _shadowCache = null;
        _shadowCacheKey = null;
    }

    public void Dispose()
    {
        ClearShadowCache();
        GC.SuppressFinalize(this);
    }

    protected static SKPath? GapPath(SKPoint[] firstEdge, SKPoint[] secondEdge)
    {
        var path = new SKPath();
        path.AddPoly(firstEdge.Concat(secondEdge.Reverse()).ToArray(), close: true);
        return path;
    }
}

/// <summary>
/// Marks where a strip was cut out of the image (crop out vertically or horizontally): a band over the whole width
/// (horizontal) or height (vertical) with two edges in the cut edge style and a gap between them. It can only be moved
/// and sized across the cut. The CutMarkContainer of the Windows editor.
/// </summary>
public sealed class CutMarkElement : CutElement
{
    public bool Horizontal { get; set; } = true;

    public override string WindowsTypeName => "CutMarkContainer";

    /// <summary>
    /// The size of the band for a gap: two teeth and the gap
    /// </summary>
    public static int GetBandSize(int toothHeight, int gap) => 2 * Math.Max(1, toothHeight) + gap;

    public override string ShapeKey => $"{NormalizedBounds}|{Style.CutMarkStyle}|{Seed}|{ToothHeight}|{ToothRange}";

    public override void ApplySettings(TornEdgeSettings settings)
    {
        int oldToothHeight = ToothHeight;
        base.ApplySettings(settings);
        Style.ToothRange = Math.Max(2, Horizontal ? settings.HorizontalToothRange : settings.VerticalToothRange);
        KeepGap(oldToothHeight);
    }

    /// <summary>
    /// The gap keeps its size when the teeth change: the band grows or shrinks on both sides
    /// </summary>
    public override void SetField(FieldType field, object value)
    {
        int oldToothHeight = ToothHeight;
        base.SetField(field, value);
        if (field == FieldType.ToothHeight)
        {
            KeepGap(oldToothHeight);
        }
    }

    private void KeepGap(int oldToothHeight)
    {
        int delta = ToothHeight - oldToothHeight;
        if (delta == 0)
        {
            return;
        }

        var band = NormalizedBounds;
        Bounds = Horizontal
            ? new SKRect(band.Left, band.Top - delta, band.Right, band.Top - delta + Math.Max(2 * ToothHeight, band.Height + 2 * delta))
            : new SKRect(band.Left - delta, band.Top, band.Left - delta + Math.Max(2 * ToothHeight, band.Width + 2 * delta), band.Bottom);
    }

    protected override void SpanImage()
    {
        var band = NormalizedBounds;
        Bounds = Horizontal ? new SKRect(0, band.Top, ImageWidth, band.Bottom) : new SKRect(band.Left, 0, band.Right, ImageHeight);
    }

    public override void MoveBy(float dx, float dy)
    {
        base.MoveBy(Horizontal ? 0 : dx, Horizontal ? dy : 0);
        SpanImage();
    }

    public override void Transform(SKMatrix matrix)
    {
        base.Transform(matrix);
        Bounds = NormalizedBounds;
        SpanImage();
    }

    public override IReadOnlyList<Handle> GetHandles()
    {
        var r = NormalizedBounds;
        return Horizontal
            ? [new Handle(HandlePosition.Top, new SKPoint(r.MidX, r.Top)), new Handle(HandlePosition.Bottom, new SKPoint(r.MidX, r.Bottom))]
            : [new Handle(HandlePosition.Left, new SKPoint(r.Left, r.MidY)), new Handle(HandlePosition.Right, new SKPoint(r.Right, r.MidY))];
    }

    public override void DragHandle(HandlePosition handle, SKPoint point, bool keepAspect)
    {
        var r = NormalizedBounds;
        float minimum = 2 * ToothHeight;
        Bounds = handle switch
        {
            HandlePosition.Top => new SKRect(r.Left, Math.Min(point.Y, r.Bottom - minimum), r.Right, r.Bottom),
            HandlePosition.Bottom => new SKRect(r.Left, r.Top, r.Right, Math.Max(point.Y, r.Top + minimum)),
            HandlePosition.Left => new SKRect(Math.Min(point.X, r.Right - minimum), r.Top, r.Right, r.Bottom),
            HandlePosition.Right => new SKRect(r.Left, r.Top, Math.Max(point.X, r.Left + minimum), r.Bottom),
            _ => r,
        };
    }

    public override bool HitTest(SKPoint point, float tolerance)
    {
        var band = NormalizedBounds;
        return Horizontal ? point.Y >= band.Top - tolerance && point.Y <= band.Bottom + tolerance : point.X >= band.Left - tolerance && point.X <= band.Right + tolerance;
    }

    /// <summary>
    /// The two edges: the first one goes into the part before the cut, the second one into the part after it
    /// </summary>
    private bool CreateEdges(out SKPoint[] firstEdge, out SKPoint[] secondEdge)
    {
        firstEdge = secondEdge = [];
        var band = SKRectI.Round(NormalizedBounds);
        if (Style.CutMarkStyle == CutMarkStyle.None || band.Width <= 0 || band.Height <= 0)
        {
            return false;
        }

        int length = Horizontal ? band.Width : band.Height;
        int thickness = Horizontal ? band.Height : band.Width;
        int toothHeight = Math.Max(1, Math.Min(ToothHeight, thickness / 2));
        var random = new Random(Seed);
        var first = CutOut.CreateEdge(Style.CutMarkStyle, length, toothHeight, ToothRange, random);
        var second = CutOut.CreateEdge(Style.CutMarkStyle, length, toothHeight, ToothRange, random);

        // along the cut, across the cut
        SKPoint Map(float along, float across) => Horizontal ? new SKPoint(band.Left + along, band.Top + across) : new SKPoint(band.Left + across, band.Top + along);
        firstEdge = first.Select(p => Map(p.X, toothHeight - p.Y)).ToArray();
        secondEdge = second.Select(p => Map(p.X, thickness - toothHeight + p.Y)).ToArray();
        return true;
    }

    public override SKPath? CreateCutPath() => CreateEdges(out var first, out var second) ? GapPath(first, second) : null;

    protected override IReadOnlyList<SKPoint[]> CreateEdgeLines() => CreateEdges(out var first, out var second) ? [first, second] : [];
}

/// <summary>
/// The torn edge effect as an element: it tears the edges of the whole image and can be changed or removed later. A crop
/// with a cut edge style uses it for the sides it cut off, in that style. The image keeps its size, the shadow falls into
/// the torn off parts; Margins is the room for the shadow outside of the edges. The TornEdgeContainer of the Windows editor.
/// </summary>
public sealed class TornEdgeElement : CutElement
{
    private static readonly IReadOnlyList<Handle> NoHandles = [];

    /// <summary>
    /// Which sides are torn: top, right, bottom, left
    /// </summary>
    public bool[] Edges { get; set; } = [true, true, true, true];

    /// <summary>
    /// The room for the shadow outside of the top, right, bottom and left side, the canvas was made bigger by it
    /// </summary>
    public int[] Margins { get; set; } = [0, 0, 0, 0];

    public override string WindowsTypeName => "TornEdgeContainer";

    public override string ShapeKey => $"{ImageWidth}x{ImageHeight}|{string.Join(",", Margins)}|{Style.CutMarkStyle}|{Seed}|{string.Join(",", Edges)}|{ToothHeight}|{ToothRange}";

    public override void ApplySettings(TornEdgeSettings settings)
    {
        base.ApplySettings(settings);
        Edges = settings.Edges is { Length: 4 } edges ? (bool[])edges.Clone() : [true, true, true, true];
    }

    public override TornEdgeSettings GetSettings()
    {
        var settings = base.GetSettings();
        settings.Edges = (bool[])Edges.Clone();
        return settings;
    }

    protected override void SpanImage() => Bounds = new SKRect(0, 0, ImageWidth, ImageHeight);

    public override IReadOnlyList<Handle> GetHandles() => NoHandles;

    public override void MoveBy(float dx, float dy) => SpanImage();

    public override void DragHandle(HandlePosition handle, SKPoint point, bool keepAspect) => SpanImage();

    public override void Transform(SKMatrix matrix) => SpanImage();

    /// <summary>
    /// Only the torn edges can be clicked, not the rest of the image
    /// </summary>
    public override bool HitTest(SKPoint point, float tolerance)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= ImageWidth || point.Y >= ImageHeight)
        {
            return false;
        }

        float reach = ToothHeight + 4 + tolerance;
        return (Edges[0] && point.Y < Margins[0] + reach) || (Edges[1] && point.X >= ImageWidth - Margins[1] - reach) ||
               (Edges[2] && point.Y >= ImageHeight - Margins[2] - reach) || (Edges[3] && point.X < Margins[3] + reach);
    }

    private bool HasEdges(out int innerWidth, out int innerHeight)
    {
        innerWidth = ImageWidth - Margins[1] - Margins[3];
        innerHeight = ImageHeight - Margins[0] - Margins[2];
        return Style.CutMarkStyle != CutMarkStyle.None && innerWidth > 2 && innerHeight > 2 && Edges.Any(edge => edge);
    }

    /// <summary>
    /// The outline of the part which stays, clockwise from the top left, and the points of every torn side.
    /// A side which isn't torn is a straight line along the image.
    /// </summary>
    private SKPoint[] CreateOutline(int width, int height, out List<SKPoint[]> tornSides)
    {
        int toothHeight = Math.Max(1, Math.Min(ToothHeight, Math.Min(width, height) / 3));
        var random = new Random(Seed);
        var outline = new List<SKPoint>();
        var sides = new List<SKPoint[]>();
        var style = Style.CutMarkStyle;
        bool top = Edges[0], right = Edges[1], bottom = Edges[2], left = Edges[3];

        void AddSide(bool torn, bool tornBefore, bool tornAfter, int length, SKPoint cornerStart, SKPoint cornerEnd, Func<float, float, SKPoint> map)
        {
            if (!torn)
            {
                outline.Add(cornerStart);
                outline.Add(cornerEnd);
                return;
            }

            // Leave the corner to the torn side next to it
            int insetStart = tornBefore ? toothHeight : 0;
            int insetEnd = tornAfter ? toothHeight : 0;
            int sideLength = Math.Max(1, length - insetStart - insetEnd);
            var side = CutOut.CreateEdge(style, sideLength, toothHeight, ToothRange, random).Select(p => map(insetStart + p.X, p.Y)).ToArray();
            outline.AddRange(side);
            sides.Add(side);
        }

        AddSide(top, left, right, width, new SKPoint(0, 0), new SKPoint(width, 0), (along, depth) => new SKPoint(along, depth));
        AddSide(right, top, bottom, height, new SKPoint(width, 0), new SKPoint(width, height), (along, depth) => new SKPoint(width - depth, along));
        AddSide(bottom, right, left, width, new SKPoint(width, height), new SKPoint(0, height), (along, depth) => new SKPoint(width - along, height - depth));
        AddSide(left, bottom, top, height, new SKPoint(0, height), new SKPoint(0, 0), (along, depth) => new SKPoint(depth, height - along));

        // The edges are inside the room for the shadow
        var shift = new SKPoint(Margins[3], Margins[0]);
        tornSides = sides.Select(side => side.Select(p => p + shift).ToArray()).ToList();
        return outline.Select(p => p + shift).ToArray();
    }

    public override SKPath? CreateCutPath()
    {
        if (!HasEdges(out int width, out int height))
        {
            return null;
        }

        // The image with the part which stays as hole
        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        path.AddRect(new SKRect(0, 0, ImageWidth, ImageHeight));
        path.AddPoly(CreateOutline(width, height, out _), close: true);
        return path;
    }

    protected override IReadOnlyList<SKPoint[]> CreateEdgeLines()
    {
        if (!HasEdges(out int width, out int height))
        {
            return [];
        }

        CreateOutline(width, height, out var sides);
        return sides;
    }
}
