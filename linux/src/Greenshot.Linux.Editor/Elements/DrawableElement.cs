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
/// Where a handle of a selected element is; the Positions and adorners of the Windows editor
/// </summary>
public enum HandlePosition
{
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,

    /// <summary>
    /// The start of a line or arrow
    /// </summary>
    LineStart,

    /// <summary>
    /// The end of a line or arrow
    /// </summary>
    LineEnd,

    /// <summary>
    /// The tip of the tail of a speech bubble
    /// </summary>
    Target,
}

public readonly record struct Handle(HandlePosition Position, SKPoint Location);

/// <summary>
/// Something drawn on the capture: the DrawableContainer of the Windows editor.
/// Positions are image pixels. <see cref="Bounds"/> keeps the direction it was drawn in (Left can be larger than Right),
/// lines and arrows use it as start (Left, Top) and end (Right, Bottom).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RectangleElement), "rectangle")]
[JsonDerivedType(typeof(EllipseElement), "ellipse")]
[JsonDerivedType(typeof(LineElement), "line")]
[JsonDerivedType(typeof(ArrowElement), "arrow")]
[JsonDerivedType(typeof(FreehandElement), "freehand")]
[JsonDerivedType(typeof(TextElement), "text")]
[JsonDerivedType(typeof(SpeechBubbleElement), "speechbubble")]
[JsonDerivedType(typeof(StepLabelElement), "steplabel")]
[JsonDerivedType(typeof(HighlightElement), "highlight")]
[JsonDerivedType(typeof(ObfuscateElement), "obfuscate")]
[JsonDerivedType(typeof(ImageElement), "image")]
[JsonDerivedType(typeof(CutMarkElement), "cutmark")]
[JsonDerivedType(typeof(TornEdgeElement), "tornedge")]
[JsonDerivedType(typeof(EmojiElement), "emoji")]
[JsonDerivedType(typeof(BarcodeElement), "barcode")]
public abstract class DrawableElement
{
    /// <summary>
    /// The smallest size a drawn element keeps; a click without dragging uses <see cref="DefaultSize"/> or creates nothing
    /// </summary>
    public const float MinimumSize = 3;

    public SKRect Bounds { get; set; }

    public ElementStyle Style { get; set; } = new();

    /// <summary>
    /// The name of the container in the Windows editor, the scope of the last used field values
    /// </summary>
    [JsonIgnore]
    public abstract string WindowsTypeName { get; }

    /// <summary>
    /// The fields this element uses, shown in the property bar
    /// </summary>
    [JsonIgnore]
    public abstract IReadOnlyList<FieldType> Fields { get; }

    [JsonIgnore]
    public SKRect NormalizedBounds => Bounds.Standardized;

    /// <summary>
    /// The size of an element created with a click, null when a click creates nothing
    /// </summary>
    [JsonIgnore]
    public virtual SKSize? DefaultSize => null;

    /// <summary>
    /// Can the text be edited (text box, speech bubble)?
    /// </summary>
    [JsonIgnore]
    public virtual bool HasText => false;

    /// <summary>
    /// The style an element of this kind starts with, the InitializeFields of Windows
    /// </summary>
    public abstract ElementStyle CreateDefaultStyle();

    public abstract void Draw(RenderContext context);

    /// <summary>
    /// Change a field from the property bar; elements which keep something in step with a field override it
    /// </summary>
    public virtual void SetField(FieldType field, object value) => Style.Set(field, value);

    /// <summary>
    /// The image has this size now (it was loaded, cropped, rotated, ...); elements which span the image follow it
    /// </summary>
    public virtual void FitToImage(int width, int height)
    {
    }

    /// <summary>
    /// Is the point on the element? tolerance is in image pixels, it grows when zoomed out
    /// </summary>
    public virtual bool HitTest(SKPoint point, float tolerance)
    {
        var rect = NormalizedBounds;
        rect.Inflate(tolerance, tolerance);
        return rect.Contains(point);
    }

    /// <summary>
    /// The handles to change the element when it is selected
    /// </summary>
    public virtual IReadOnlyList<Handle> GetHandles()
    {
        var r = NormalizedBounds;
        return
        [
            new Handle(HandlePosition.TopLeft, new SKPoint(r.Left, r.Top)),
            new Handle(HandlePosition.Top, new SKPoint(r.MidX, r.Top)),
            new Handle(HandlePosition.TopRight, new SKPoint(r.Right, r.Top)),
            new Handle(HandlePosition.Right, new SKPoint(r.Right, r.MidY)),
            new Handle(HandlePosition.BottomRight, new SKPoint(r.Right, r.Bottom)),
            new Handle(HandlePosition.Bottom, new SKPoint(r.MidX, r.Bottom)),
            new Handle(HandlePosition.BottomLeft, new SKPoint(r.Left, r.Bottom)),
            new Handle(HandlePosition.Left, new SKPoint(r.Left, r.MidY)),
        ];
    }

    public virtual void MoveBy(float dx, float dy)
    {
        var bounds = Bounds;
        bounds.Offset(dx, dy);
        Bounds = bounds;
    }

    /// <summary>
    /// Drag a handle to the point; keepAspect (Shift) keeps the proportions when dragging a corner
    /// </summary>
    public virtual void DragHandle(HandlePosition handle, SKPoint point, bool keepAspect)
    {
        var r = NormalizedBounds;
        float left = r.Left, top = r.Top, right = r.Right, bottom = r.Bottom;
        switch (handle)
        {
            case HandlePosition.TopLeft:
                left = point.X;
                top = point.Y;
                break;
            case HandlePosition.Top:
                top = point.Y;
                break;
            case HandlePosition.TopRight:
                right = point.X;
                top = point.Y;
                break;
            case HandlePosition.Right:
                right = point.X;
                break;
            case HandlePosition.BottomRight:
                right = point.X;
                bottom = point.Y;
                break;
            case HandlePosition.Bottom:
                bottom = point.Y;
                break;
            case HandlePosition.BottomLeft:
                left = point.X;
                bottom = point.Y;
                break;
            case HandlePosition.Left:
                left = point.X;
                break;
            default:
                return;
        }

        var resized = new SKRect(left, top, right, bottom);
        if (keepAspect && r.Width > 0 && r.Height > 0 && handle is HandlePosition.TopLeft or HandlePosition.TopRight
                or HandlePosition.BottomLeft or HandlePosition.BottomRight)
        {
            resized = KeepAspect(r, resized, handle);
        }

        SetBoundsFromResize(r, resized.Standardized);
    }

    /// <summary>
    /// Called with the old and the new bounds after a handle was dragged, elements with points scale them here
    /// </summary>
    protected virtual void SetBoundsFromResize(SKRect oldBounds, SKRect newBounds) => Bounds = newBounds;

    /// <summary>
    /// Map the element through the matrix: used when the image is rotated, cropped or gets a border
    /// </summary>
    public virtual void Transform(SKMatrix matrix)
    {
        var start = matrix.MapPoint(new SKPoint(Bounds.Left, Bounds.Top));
        var end = matrix.MapPoint(new SKPoint(Bounds.Right, Bounds.Bottom));
        Bounds = new SKRect(start.X, start.Y, end.X, end.Y);
    }

    public virtual void BeginCreate(SKPoint point) => Bounds = new SKRect(point.X, point.Y, point.X, point.Y);

    /// <summary>
    /// The mouse moves while the element is drawn; constrain (Shift) makes squares and circles
    /// </summary>
    public virtual void UpdateCreate(SKPoint point, bool constrain)
    {
        var bounds = Bounds;
        float width = point.X - bounds.Left;
        float height = point.Y - bounds.Top;
        if (constrain)
        {
            float size = Math.Max(Math.Abs(width), Math.Abs(height));
            width = Math.Sign(width == 0 ? 1 : width) * size;
            height = Math.Sign(height == 0 ? 1 : height) * size;
        }

        Bounds = new SKRect(bounds.Left, bounds.Top, bounds.Left + width, bounds.Top + height);
    }

    /// <summary>
    /// The drawing is finished, false when the element is too small and should be removed
    /// </summary>
    public virtual bool EndCreate()
    {
        var r = NormalizedBounds;
        if (r.Width >= MinimumSize || r.Height >= MinimumSize)
        {
            Bounds = r;
            return true;
        }

        if (DefaultSize is { } size)
        {
            Bounds = SKRect.Create(Bounds.Left, Bounds.Top, size.Width, size.Height);
            return true;
        }

        return false;
    }

    private static SKRect KeepAspect(SKRect original, SKRect resized, HandlePosition handle)
    {
        float ratio = original.Width / original.Height;
        float width = Math.Abs(resized.Width);
        float height = Math.Abs(resized.Height);
        if (width / Math.Max(height, 0.001f) > ratio)
        {
            height = width / ratio;
        }
        else
        {
            width = height * ratio;
        }

        return handle switch
        {
            HandlePosition.TopLeft => new SKRect(original.Right - width, original.Bottom - height, original.Right, original.Bottom),
            HandlePosition.TopRight => new SKRect(original.Left, original.Bottom - height, original.Left + width, original.Bottom),
            HandlePosition.BottomLeft => new SKRect(original.Right - width, original.Top, original.Right, original.Top + height),
            _ => new SKRect(original.Left, original.Top, original.Left + width, original.Top + height),
        };
    }
}
