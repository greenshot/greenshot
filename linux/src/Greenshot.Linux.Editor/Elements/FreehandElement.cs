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
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Elements;

/// <summary>
/// FreehandContainer: a smoothed line through the points the mouse went
/// </summary>
public sealed class FreehandElement : DrawableElement
{
    private static readonly FieldType[] FreehandFields = [FieldType.LineThickness, FieldType.LineColor];

    public List<SKPoint> Points { get; set; } = new();

    /// <summary>
    /// The smaller, the more points are kept: FreehandSensitivity of the editor settings
    /// </summary>
    [JsonIgnore]
    public int Sensitivity { get; set; } = 3;

    public override string WindowsTypeName => "FreehandContainer";

    public override IReadOnlyList<FieldType> Fields => FreehandFields;

    public override ElementStyle CreateDefaultStyle() => new()
    {
        LineThickness = 3,
        LineColor = SKColors.Red,
        Shadow = false,
    };

    public override void Draw(RenderContext context)
    {
        if (Points.Count == 0 || Style.LineThickness <= 0)
        {
            return;
        }

        using var path = CreatePath();
        using var pen = Painting.Stroke(Style.LineColor, Style.LineThickness);
        pen.StrokeCap = SKStrokeCap.Round;
        pen.StrokeJoin = SKStrokeJoin.Round;
        if (Points.Count == 1)
        {
            context.Canvas.DrawPoint(Points[0], pen);
            return;
        }

        context.Canvas.DrawPath(path, pen);
    }

    /// <summary>
    /// Quadratic curves through the middle of each segment, which smooths the corners of the mouse path
    /// </summary>
    internal SKPath CreatePath()
    {
        var path = new SKPath();
        path.MoveTo(Points[0]);
        for (int i = 1; i < Points.Count - 1; i++)
        {
            var mid = new SKPoint((Points[i].X + Points[i + 1].X) / 2, (Points[i].Y + Points[i + 1].Y) / 2);
            path.QuadTo(Points[i], mid);
        }

        if (Points.Count > 1)
        {
            path.LineTo(Points[^1]);
        }

        return path;
    }

    public override bool HitTest(SKPoint point, float tolerance)
    {
        float limit = (Style.LineThickness + 10) / 2f + tolerance;
        if (Points.Count == 1)
        {
            return (point - Points[0]).Length <= limit;
        }

        for (int i = 1; i < Points.Count; i++)
        {
            if (Geometry.DistanceToSegment(point, Points[i - 1], Points[i]) <= limit)
            {
                return true;
            }
        }

        return false;
    }

    public override void BeginCreate(SKPoint point)
    {
        Points = [point];
        UpdateBounds();
    }

    public override void UpdateCreate(SKPoint point, bool constrain)
    {
        if ((point - Points[^1]).Length >= Sensitivity)
        {
            Points.Add(point);
            UpdateBounds();
        }
    }

    public override bool EndCreate()
    {
        UpdateBounds();
        return Points.Count > 0;
    }

    public override void MoveBy(float dx, float dy)
    {
        for (int i = 0; i < Points.Count; i++)
        {
            Points[i] = new SKPoint(Points[i].X + dx, Points[i].Y + dy);
        }

        UpdateBounds();
    }

    protected override void SetBoundsFromResize(SKRect oldBounds, SKRect newBounds)
    {
        float scaleX = oldBounds.Width > 0 ? newBounds.Width / oldBounds.Width : 1;
        float scaleY = oldBounds.Height > 0 ? newBounds.Height / oldBounds.Height : 1;
        for (int i = 0; i < Points.Count; i++)
        {
            Points[i] = new SKPoint(newBounds.Left + (Points[i].X - oldBounds.Left) * scaleX, newBounds.Top + (Points[i].Y - oldBounds.Top) * scaleY);
        }

        UpdateBounds();
    }

    public override void Transform(SKMatrix matrix)
    {
        for (int i = 0; i < Points.Count; i++)
        {
            Points[i] = matrix.MapPoint(Points[i]);
        }

        UpdateBounds();
    }

    private void UpdateBounds()
    {
        if (Points.Count == 0)
        {
            Bounds = SKRect.Empty;
            return;
        }

        float left = Points.Min(p => p.X), top = Points.Min(p => p.Y);
        float right = Points.Max(p => p.X), bottom = Points.Max(p => p.Y);
        Bounds = new SKRect(left, top, right, bottom);
    }
}
