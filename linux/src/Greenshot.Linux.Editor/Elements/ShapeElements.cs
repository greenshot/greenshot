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

using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Elements;

/// <summary>
/// RectangleContainer
/// </summary>
public class RectangleElement : DrawableElement
{
    private static readonly FieldType[] RectangleFields = [FieldType.LineThickness, FieldType.LineColor, FieldType.FillColor, FieldType.Shadow];

    public override string WindowsTypeName => "RectangleContainer";

    public override IReadOnlyList<FieldType> Fields => RectangleFields;

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 2,
        LineColor = SKColors.Red,
        FillColor = SKColors.Transparent,
        Shadow = true,
    };

    public override void Draw(RenderContext context) =>
        DrawRectangle(context.Canvas, NormalizedBounds, Style.LineThickness, Style.LineColor, Style.FillColor, Style.Shadow);

    public static void DrawRectangle(SKCanvas canvas, SKRect rect, int lineThickness, SKColor lineColor, SKColor fillColor, bool shadow)
    {
        bool lineVisible = lineThickness > 0 && Painting.IsVisible(lineColor);
        if (shadow && (lineVisible || Painting.IsVisible(fillColor)))
        {
            Painting.DrawShadow((alpha, step) =>
            {
                using var shadowPaint = Painting.Stroke(Painting.ShadowColor(alpha), Math.Max(1, lineThickness));
                var shadowRect = rect;
                shadowRect.Offset(step, step);
                canvas.DrawRect(shadowRect, shadowPaint);
            });
        }

        if (Painting.IsVisible(fillColor))
        {
            using var fill = Painting.Fill(fillColor);
            canvas.DrawRect(rect, fill);
        }

        if (lineVisible)
        {
            using var pen = Painting.Stroke(lineColor, lineThickness);
            canvas.DrawRect(rect, pen);
        }
    }

    public override bool HitTest(SKPoint point, float tolerance) =>
        RectangleHitTest(NormalizedBounds, Style.LineThickness, Style.FillColor, point, tolerance);

    /// <summary>
    /// Filled rectangles are hit inside, others only on the line (with 10 pixels extra like on Windows)
    /// </summary>
    public static bool RectangleHitTest(SKRect rect, int lineThickness, SKColor fillColor, SKPoint point, float tolerance)
    {
        if (Painting.IsVisible(fillColor) && rect.Contains(point))
        {
            return true;
        }

        float half = (lineThickness + 10) / 2f + tolerance;
        var outer = rect;
        outer.Inflate(half, half);
        var inner = rect;
        inner.Inflate(-half, -half);
        return outer.Contains(point) && (inner.Width <= 0 || inner.Height <= 0 || !inner.Contains(point));
    }
}

/// <summary>
/// EllipseContainer
/// </summary>
public class EllipseElement : DrawableElement
{
    private static readonly FieldType[] EllipseFields = [FieldType.LineThickness, FieldType.LineColor, FieldType.FillColor, FieldType.Shadow];

    public override string WindowsTypeName => "EllipseContainer";

    public override IReadOnlyList<FieldType> Fields => EllipseFields;

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 2,
        LineColor = SKColors.Red,
        FillColor = SKColors.Transparent,
        Shadow = true,
    };

    public override void Draw(RenderContext context) =>
        DrawEllipse(context.Canvas, NormalizedBounds, Style.LineThickness, Style.LineColor, Style.FillColor, Style.Shadow);

    public static void DrawEllipse(SKCanvas canvas, SKRect rect, int lineThickness, SKColor lineColor, SKColor fillColor, bool shadow)
    {
        bool lineVisible = lineThickness > 0 && Painting.IsVisible(lineColor);
        if (shadow && (lineVisible || Painting.IsVisible(fillColor)))
        {
            Painting.DrawShadow((alpha, step) =>
            {
                using var shadowPaint = Painting.Stroke(Painting.ShadowColor(alpha), Math.Max(1, lineThickness));
                var shadowRect = rect;
                shadowRect.Offset(step, step);
                canvas.DrawOval(shadowRect, shadowPaint);
            });
        }

        if (Painting.IsVisible(fillColor))
        {
            using var fill = Painting.Fill(fillColor);
            canvas.DrawOval(rect, fill);
        }

        if (lineVisible)
        {
            using var pen = Painting.Stroke(lineColor, lineThickness);
            canvas.DrawOval(rect, pen);
        }
    }

    public override bool HitTest(SKPoint point, float tolerance) =>
        EllipseHitTest(NormalizedBounds, Style.LineThickness, Style.FillColor, point, tolerance);

    public static bool EllipseHitTest(SKRect rect, int lineThickness, SKColor fillColor, SKPoint point, float tolerance)
    {
        float rx = rect.Width / 2, ry = rect.Height / 2;
        if (rx <= 0 || ry <= 0)
        {
            return false;
        }

        float dx = point.X - rect.MidX, dy = point.Y - rect.MidY;
        // The distance from the center, 1 is on the ellipse
        double distance = Math.Sqrt(dx * dx / (rx * rx) + dy * dy / (ry * ry));
        if (Painting.IsVisible(fillColor) && distance <= 1)
        {
            return true;
        }

        double band = ((lineThickness + 10) / 2.0 + tolerance) / Math.Min(rx, ry);
        return Math.Abs(distance - 1) <= band;
    }
}

/// <summary>
/// LineContainer. The line goes from (Bounds.Left, Bounds.Top) to (Bounds.Right, Bounds.Bottom).
/// </summary>
public class LineElement : DrawableElement
{
    private static readonly FieldType[] LineFields = [FieldType.LineThickness, FieldType.LineColor, FieldType.Shadow];

    public override string WindowsTypeName => "LineContainer";

    public override IReadOnlyList<FieldType> Fields => LineFields;

    public SKPoint Start => new(Bounds.Left, Bounds.Top);

    public SKPoint End => new(Bounds.Right, Bounds.Bottom);

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 2,
        LineColor = SKColors.Red,
        Shadow = true,
    };

    public override void Draw(RenderContext context) => DrawLine(context.Canvas, ArrowHeads.None);

    protected void DrawLine(SKCanvas canvas, ArrowHeads heads)
    {
        int thickness = Style.LineThickness;
        if (thickness <= 0)
        {
            return;
        }

        if (Style.Shadow)
        {
            Painting.DrawShadow((alpha, step) =>
                DrawLineWithHeads(canvas, Painting.ShadowColor(alpha), thickness, new SKPoint(Start.X + step, Start.Y + step),
                    new SKPoint(End.X + step, End.Y + step), heads));
        }

        DrawLineWithHeads(canvas, Style.LineColor, thickness, Start, End, heads);
    }

    /// <summary>
    /// The arrow head is the AdjustableArrowCap(4, 6) of Windows: 4 line widths wide and 6 long, filled
    /// </summary>
    internal static void DrawLineWithHeads(SKCanvas canvas, SKColor color, int thickness, SKPoint start, SKPoint end, ArrowHeads heads)
    {
        var direction = end - start;
        float length = direction.Length;
        float headLength = 6 * thickness;
        float headHalfWidth = 2 * thickness;
        bool startHead = heads is ArrowHeads.Start or ArrowHeads.Both;
        bool endHead = heads is ArrowHeads.End or ArrowHeads.Both;

        var lineStart = start;
        var lineEnd = end;
        if (length > 0)
        {
            var unit = new SKPoint(direction.X / length, direction.Y / length);
            // The line stops inside the head so the tip stays sharp
            if (endHead)
            {
                lineEnd = end - new SKPoint(unit.X * headLength * 0.8f, unit.Y * headLength * 0.8f);
            }

            if (startHead)
            {
                lineStart = start + new SKPoint(unit.X * headLength * 0.8f, unit.Y * headLength * 0.8f);
            }
        }

        using (var pen = Painting.Stroke(color, thickness))
        {
            pen.StrokeCap = heads == ArrowHeads.None ? SKStrokeCap.Round : SKStrokeCap.Butt;
            canvas.DrawLine(lineStart, lineEnd, pen);
        }

        if (length <= 0)
        {
            return;
        }

        using var fill = Painting.Fill(color);
        if (endHead)
        {
            canvas.DrawPath(ArrowHead(end, direction, length, headLength, headHalfWidth), fill);
        }

        if (startHead)
        {
            canvas.DrawPath(ArrowHead(start, new SKPoint(-direction.X, -direction.Y), length, headLength, headHalfWidth), fill);
        }
    }

    private static SKPath ArrowHead(SKPoint tip, SKPoint direction, float length, float headLength, float headHalfWidth)
    {
        var unit = new SKPoint(direction.X / length, direction.Y / length);
        var normal = new SKPoint(-unit.Y, unit.X);
        var basePoint = tip - new SKPoint(unit.X * headLength, unit.Y * headLength);
        var path = new SKPath();
        path.MoveTo(tip);
        path.LineTo(basePoint + new SKPoint(normal.X * headHalfWidth, normal.Y * headHalfWidth));
        path.LineTo(basePoint - new SKPoint(normal.X * headHalfWidth, normal.Y * headHalfWidth));
        path.Close();
        return path;
    }

    public override bool HitTest(SKPoint point, float tolerance) =>
        Geometry.DistanceToSegment(point, Start, End) <= (Style.LineThickness + 10) / 2f + tolerance;

    public override IReadOnlyList<Handle> GetHandles() =>
    [
        new Handle(HandlePosition.LineStart, Start),
        new Handle(HandlePosition.LineEnd, End),
    ];

    public override void DragHandle(HandlePosition handle, SKPoint point, bool keepAspect)
    {
        if (keepAspect)
        {
            point = Geometry.SnapAngle(handle == HandlePosition.LineStart ? End : Start, point);
        }

        Bounds = handle switch
        {
            HandlePosition.LineStart => new SKRect(point.X, point.Y, Bounds.Right, Bounds.Bottom),
            HandlePosition.LineEnd => new SKRect(Bounds.Left, Bounds.Top, point.X, point.Y),
            _ => Bounds,
        };
    }

    /// <summary>
    /// Shift keeps the line at multiples of 15 degrees, the LineAngleRoundBehavior of Windows
    /// </summary>
    public override void UpdateCreate(SKPoint point, bool constrain)
    {
        if (constrain)
        {
            point = Geometry.SnapAngle(Start, point);
        }

        Bounds = new SKRect(Bounds.Left, Bounds.Top, point.X, point.Y);
    }

    /// <summary>
    /// A line keeps its direction, it is not normalized
    /// </summary>
    public override bool EndCreate() => (End - Start).Length >= MinimumSize;
}

/// <summary>
/// ArrowContainer
/// </summary>
public sealed class ArrowElement : LineElement
{
    private static readonly FieldType[] ArrowFields = [FieldType.LineThickness, FieldType.LineColor, FieldType.Shadow, FieldType.ArrowHeads];

    public override string WindowsTypeName => "ArrowContainer";

    public override IReadOnlyList<FieldType> Fields => ArrowFields;

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 2,
        LineColor = SKColors.Red,
        Shadow = true,
        ArrowHeads = ArrowHeads.End,
    };

    public override void Draw(RenderContext context) => DrawLine(context.Canvas, Style.ArrowHeads);
}

internal static class Geometry
{
    public static float DistanceToSegment(SKPoint point, SKPoint a, SKPoint b)
    {
        var ab = b - a;
        float lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
        if (lengthSquared == 0)
        {
            return (point - a).Length;
        }

        float t = Math.Clamp(((point.X - a.X) * ab.X + (point.Y - a.Y) * ab.Y) / lengthSquared, 0, 1);
        var projection = new SKPoint(a.X + t * ab.X, a.Y + t * ab.Y);
        return (point - projection).Length;
    }

    /// <summary>
    /// Rotate the point around the origin to the nearest multiple of 15 degrees
    /// </summary>
    public static SKPoint SnapAngle(SKPoint origin, SKPoint point)
    {
        var delta = point - origin;
        double length = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        double angle = Math.Atan2(delta.Y, delta.X);
        const double step = Math.PI / 12;
        angle = Math.Round(angle / step) * step;
        return new SKPoint(origin.X + (float)(Math.Cos(angle) * length), origin.Y + (float)(Math.Sin(angle) * length));
    }
}
