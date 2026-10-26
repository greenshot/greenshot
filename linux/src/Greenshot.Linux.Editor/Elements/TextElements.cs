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

using System.Globalization;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Elements;

/// <summary>
/// TextContainer: a rectangle (border and fill) with text in the line color
/// </summary>
public class TextElement : RectangleElement
{
    private static readonly FieldType[] TextFields =
    [
        FieldType.LineThickness, FieldType.LineColor, FieldType.FillColor, FieldType.Shadow,
        FieldType.FontFamily, FieldType.FontSize, FieldType.FontBold, FieldType.FontItalic,
        FieldType.TextHorizontalAlignment, FieldType.TextVerticalAlignment,
    ];

    public string Text { get; set; } = string.Empty;

    public override string WindowsTypeName => "TextContainer";

    public override IReadOnlyList<FieldType> Fields => TextFields;

    public override bool HasText => true;

    public override SKSize? DefaultSize => new SKSize(150, 40);

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 2,
        LineColor = SKColors.Red,
        FillColor = SKColors.Transparent,
        Shadow = true,
        FontFamily = "Sans",
        FontSize = 11f,
        FontBold = false,
        FontItalic = false,
        TextHorizontalAlignment = TextHorizontalAlignment.Center,
        TextVerticalAlignment = TextVerticalAlignment.Center,
    };

    public override void Draw(RenderContext context)
    {
        // Like Windows: the box is drawn as a rectangle, the text gets a shadow only without a background
        var rect = NormalizedBounds;
        DrawRectangle(context.Canvas, rect, Style.LineThickness, Style.LineColor, Style.FillColor, Style.Shadow);
        DrawText(context.Canvas, rect, Text, Style, Style.Shadow && !Painting.IsVisible(Style.FillColor));
    }

    /// <summary>
    /// The text inside the border, with the shadow of the Windows editor when wanted
    /// </summary>
    internal static void DrawText(SKCanvas canvas, SKRect rect, string text, ElementStyle style, bool shadow)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int inset = style.LineThickness > 0 ? (int)Math.Ceiling(style.LineThickness / 2d) : 0;
        var textRect = rect;
        textRect.Inflate(-inset, -inset);
        using var font = TextLayout.CreateFont(style);
        if (shadow)
        {
            Painting.DrawShadow((alpha, step) =>
            {
                var shadowRect = textRect;
                shadowRect.Offset(step, step);
                TextLayout.Draw(canvas, text, shadowRect, font, Painting.ShadowColor(alpha), style.TextHorizontalAlignment, style.TextVerticalAlignment);
            });
        }

        TextLayout.Draw(canvas, text, textRect, font, style.LineColor, style.TextHorizontalAlignment, style.TextVerticalAlignment);
    }

    /// <summary>
    /// Text boxes are hit anywhere inside, so the text can be clicked
    /// </summary>
    public override bool HitTest(SKPoint point, float tolerance) => base.HitTest(point, tolerance) || NormalizedBounds.Contains(point);
}

/// <summary>
/// SpeechbubbleContainer: a rounded box with a tail pointing at <see cref="Target"/>
/// </summary>
public sealed class SpeechBubbleElement : TextElement
{
    private static readonly FieldType[] BubbleFields =
    [
        FieldType.LineThickness, FieldType.LineColor, FieldType.FillColor, FieldType.Shadow,
        FieldType.FontFamily, FieldType.FontSize, FieldType.FontBold, FieldType.FontItalic,
        FieldType.TextHorizontalAlignment, FieldType.TextVerticalAlignment,
    ];

    /// <summary>
    /// Where the tail points to
    /// </summary>
    public SKPoint Target { get; set; }

    public override string WindowsTypeName => "SpeechbubbleContainer";

    public override IReadOnlyList<FieldType> Fields => BubbleFields;

    public override SKSize? DefaultSize => new SKSize(200, 100);

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 2,
        LineColor = SKColors.Blue,
        FillColor = SKColors.White,
        Shadow = false,
        FontFamily = "Sans",
        FontSize = 20f,
        FontBold = true,
        FontItalic = false,
        TextHorizontalAlignment = TextHorizontalAlignment.Center,
        TextVerticalAlignment = TextVerticalAlignment.Center,
    };

    /// <summary>
    /// Like Windows: the tail starts where the mouse was pressed, the bubble is drawn away from it
    /// </summary>
    public override void BeginCreate(SKPoint point)
    {
        base.BeginCreate(point);
        Target = point;
    }

    public override void UpdateCreate(SKPoint point, bool constrain)
    {
        base.UpdateCreate(point, constrain);
        // Keep the tail 20 pixels outside the corner the drawing started at
        bool leftToRight = Bounds.Right >= Bounds.Left;
        bool topToBottom = Bounds.Bottom >= Bounds.Top;
        Target = new SKPoint(Bounds.Left + (leftToRight ? -20 : 20), Bounds.Top + (topToBottom ? -20 : 20));
    }

    public override bool EndCreate()
    {
        bool clicked = NormalizedBounds.Width < MinimumSize && NormalizedBounds.Height < MinimumSize;
        var result = base.EndCreate();
        if (clicked)
        {
            // A click puts the bubble right of and above the click, the tail points at the click
            Target = new SKPoint(Bounds.Left, Bounds.Top);
            var bounds = Bounds;
            bounds.Offset(20, -bounds.Height - 20);
            Bounds = bounds;
        }

        return result;
    }

    public override void Draw(RenderContext context)
    {
        var rect = NormalizedBounds;
        bool lineVisible = Style.LineThickness > 0 && Painting.IsVisible(Style.LineColor);
        using var path = CreateOutline(rect, Style.LineThickness);
        var canvas = context.Canvas;
        if (Style.Shadow && (lineVisible || Painting.IsVisible(Style.FillColor)))
        {
            Painting.DrawShadow((alpha, step) =>
            {
                if (step == 0)
                {
                    return;
                }

                using var shadowPath = new SKPath(path);
                shadowPath.Offset(step, step);
                using var shadowPaint = Painting.Stroke(Painting.ShadowColor(alpha), Math.Max(1, Style.LineThickness));
                canvas.DrawPath(shadowPath, shadowPaint);
            });
        }

        if (Painting.IsVisible(Style.FillColor))
        {
            using var fill = Painting.Fill(Style.FillColor);
            canvas.DrawPath(path, fill);
        }

        if (lineVisible)
        {
            using var pen = Painting.Stroke(Style.LineColor, Style.LineThickness);
            pen.StrokeJoin = SKStrokeJoin.Round;
            canvas.DrawPath(path, pen);
        }

        DrawText(canvas, rect, Text, Style, false);
    }

    /// <summary>
    /// The rounded box joined with the tail; the tail is a triangle from the middle of the box to the target
    /// </summary>
    internal SKPath CreateOutline(SKRect rect, int lineThickness)
    {
        float smaller = Math.Min(rect.Width, rect.Height);
        // GDI+ arcs take the diameter, Greenshot uses min(30, smaller / 2 - line) as diameter
        float diameter = Math.Min(30, smaller / 2 - lineThickness);
        using var bubble = new SKPath();
        if (diameter > 0)
        {
            bubble.AddRoundRect(rect, diameter / 2, diameter / 2);
        }
        else
        {
            bubble.AddRect(rect);
        }

        var center = new SKPoint(rect.MidX, rect.MidY);
        var toTarget = Target - center;
        float tailLength = toTarget.Length;
        float tailWidth = (Math.Abs(rect.Width) + Math.Abs(rect.Height)) / 20;
        tailWidth = Math.Min(Math.Min(Math.Abs(rect.Width) / 2, Math.Abs(rect.Height) / 2), tailWidth);
        if (tailLength <= 0 || tailWidth <= 0)
        {
            return new SKPath(bubble);
        }

        var normal = new SKPoint(-toTarget.Y / tailLength * tailWidth, toTarget.X / tailLength * tailWidth);
        using var tail = new SKPath();
        tail.MoveTo(center + normal);
        tail.LineTo(center - normal);
        tail.LineTo(Target);
        tail.Close();
        return bubble.Op(tail, SKPathOp.Union) ?? new SKPath(bubble);
    }

    public override bool HitTest(SKPoint point, float tolerance)
    {
        if (base.HitTest(point, tolerance))
        {
            return true;
        }

        using var outline = CreateOutline(NormalizedBounds, Style.LineThickness);
        return outline.Contains(point.X, point.Y);
    }

    public override IReadOnlyList<Handle> GetHandles() => [.. base.GetHandles(), new Handle(HandlePosition.Target, Target)];

    public override void DragHandle(HandlePosition handle, SKPoint point, bool keepAspect)
    {
        if (handle == HandlePosition.Target)
        {
            Target = point;
            return;
        }

        base.DragHandle(handle, point, keepAspect);
    }

    public override void MoveBy(float dx, float dy)
    {
        base.MoveBy(dx, dy);
        Target = new SKPoint(Target.X + dx, Target.Y + dy);
    }

    public override void Transform(SKMatrix matrix)
    {
        base.Transform(matrix);
        Target = matrix.MapPoint(Target);
    }
}

/// <summary>
/// StepLabelContainer: a numbered circle, the numbers follow the order of the labels on the image
/// </summary>
public sealed class StepLabelElement : DrawableElement
{
    private static readonly FieldType[] StepFields = [FieldType.FillColor, FieldType.LineColor, FieldType.LineThickness, FieldType.Shadow];

    public override string WindowsTypeName => "StepLabelContainer";

    public override IReadOnlyList<FieldType> Fields => StepFields;

    public override SKSize? DefaultSize => new SKSize(30, 30);

    public override ElementStyle CreateDefaultStyle() => new()
    {
        FillColor = new SKColor(0x8B, 0x00, 0x00),
        LineColor = SKColors.White,
        LineThickness = 0,
        Shadow = false,
    };

    /// <summary>
    /// A label is placed with a click, centered on it; dragging moves it like on Windows
    /// </summary>
    public override void BeginCreate(SKPoint point) => Bounds = SKRect.Create(point.X - 15, point.Y - 15, 30, 30);

    public override void UpdateCreate(SKPoint point, bool constrain)
    {
        var size = NormalizedBounds.Size;
        Bounds = SKRect.Create(point.X - size.Width / 2, point.Y - size.Height / 2, size.Width, size.Height);
    }

    public override bool EndCreate()
    {
        Bounds = NormalizedBounds;
        return true;
    }

    public override void Draw(RenderContext context)
    {
        var rect = NormalizedBounds;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        EllipseElement.DrawEllipse(context.Canvas, rect, Style.LineThickness, Style.LineColor, Style.FillColor, Style.Shadow);
        var text = context.StepNumber(this).ToString(CultureInfo.InvariantCulture);
        // The text fills 70% of the circle, like on Windows
        using var measure = TextLayout.CreateFont("Sans", 100, bold: true, italic: false);
        float textWidth = Math.Max(measure.MeasureText(text), 1);
        float size = Math.Min(rect.Width * 0.7f * 100 / textWidth, rect.Height * 0.7f);
        using var font = TextLayout.CreateFont("Sans", size, bold: true, italic: false);
        using var paint = new SKPaint { Color = Style.LineColor, IsAntialias = true };
        var metrics = font.Metrics;
        float baseline = rect.MidY - (metrics.Ascent + metrics.Descent) / 2;
        context.Canvas.DrawText(text, rect.MidX, baseline, SKTextAlign.Center, font, paint);
    }

    public override bool HitTest(SKPoint point, float tolerance)
    {
        var rect = NormalizedBounds;
        rect.Inflate(tolerance, tolerance);
        return EllipseElement.EllipseHitTest(rect, 0, SKColors.Black, point, 0);
    }
}
