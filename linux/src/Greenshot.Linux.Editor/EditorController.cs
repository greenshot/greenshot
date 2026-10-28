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

using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Serialization;
using SkiaSharp;

namespace Greenshot.Linux.Editor;

/// <summary>
/// The tools of the editor, with the keys of the Windows editor in the comments
/// </summary>
public enum EditorTool
{
    Select, // Esc
    Rectangle, // R
    Ellipse, // E
    Line, // L
    Arrow, // A
    Freehand, // F
    Text, // T
    SpeechBubble, // S
    StepLabel, // I
    Highlight, // H
    Obfuscate, // O
    Crop, // C
    Emoji, // M
}

[Flags]
public enum InputModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
}

/// <summary>
/// The mouse cursor the view should show
/// </summary>
public enum EditorCursor
{
    Default,
    Cross,
    Move,
    SizeNorthWestSouthEast,
    SizeNorthEastSouthWest,
    SizeNorthSouth,
    SizeWestEast,
    Hand,
}

/// <summary>
/// The interaction of the editor without a UI: the tools, the mouse and keys, the property bar and the image effects.
/// Positions are image pixels; the view converts with its zoom and tells the zoom here, so handles keep their size on screen.
/// </summary>
public sealed class EditorController
{
    private const float HandleScreenRadius = 6;
    private const float HitScreenTolerance = 3;

    private readonly EditorSettings? _settings;
    private readonly Dictionary<EditorTool, ElementStyle> _toolStyles = new();
    private EditorTool _tool = EditorTool.Select;
    private Drag _drag = Drag.None;
    private DrawableElement? _active;
    private HandlePosition _activeHandle;
    private SKPoint _lastPoint;
    private SKPoint _dragStart;
    private bool _checkpointPending;
    private DrawableElement? _textElementNew;
    private string? _pastedJson;
    private int _pasteCount;
    private CropMode _cropMode = CropMode.Default;
    private CutMarkStyle? _cropCutMarkStyle;

    private enum Drag
    {
        None,
        Creating,
        Moving,
        Resizing,
        RubberBand,
        Cropping,
        MovingCrop,
        ResizingCrop,
    }

    public EditorController(EditorSurface surface, EditorSettings? settings = null)
    {
        Surface = surface;
        _settings = settings;
        Surface.SelectionChanged += () => StateChanged?.Invoke();
    }

    public EditorSurface Surface { get; }

    /// <summary>
    /// The view zoom, 1 is 100%
    /// </summary>
    public float Zoom { get; set; } = 1;

    /// <summary>
    /// The tool, the crop area, the rubber band or the fields changed: the view updates its tool bar and overlay
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// A text element should be edited, the view shows a text box over it
    /// </summary>
    public event Action<DrawableElement>? TextEditRequested;

    /// <summary>
    /// The emoji of an element should be chosen, the view shows the emoji picker
    /// </summary>
    public event Action<EmojiElement>? EmojiPickRequested;

    public EditorTool Tool
    {
        get => _tool;
        set
        {
            if (_tool == value)
            {
                return;
            }

            if (_tool == EditorTool.Crop)
            {
                CancelCrop();
            }

            _tool = value;
            _drag = Drag.None;
            if (value != EditorTool.Select)
            {
                Surface.ClearSelection();
            }

            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// The area to crop, while the crop tool is used
    /// </summary>
    public SKRect? CropArea { get; private set; }

    /// <summary>
    /// Keep the area, or cut out its columns (vertical) or rows (horizontal)
    /// </summary>
    public CropMode CropMode
    {
        get => _cropMode;
        set
        {
            _cropMode = value;
            if (CropArea is { } crop)
            {
                CropArea = SpanCrop(crop);
            }

            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// The edges where the crop cut the image: none, or the style of the edges (the last used one is remembered)
    /// </summary>
    public CutMarkStyle CropCutMarkStyle
    {
        get => _cropCutMarkStyle ??= _settings?.GetLastUsed(CropTypeName, FieldType.CutMarkStyle) is CutMarkStyle style ? style : CutMarkStyle.None;
        set
        {
            _cropCutMarkStyle = value;
            _settings?.RememberField(CropTypeName, FieldType.CutMarkStyle, value);
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// The crop tool's name for the last used values, the CropContainer of Windows
    /// </summary>
    private const string CropTypeName = "CropContainer";

    /// <summary>
    /// The settings of the torn edges, also for the cut edges of a crop
    /// </summary>
    public TornEdgeSettings TornEdgeSettings => _settings?.TornEdgeEffectSettings ?? new TornEdgeSettings();

    /// <summary>
    /// The rectangle while selecting with the mouse
    /// </summary>
    public SKRect? RubberBand { get; private set; }

    public float HandleRadius => HandleScreenRadius / Zoom;

    private float Tolerance => HitScreenTolerance / Zoom;

    /// <summary>
    /// A new element of the type the tool draws, null for select and crop
    /// </summary>
    public static DrawableElement? CreateElement(EditorTool tool) => tool switch
    {
        EditorTool.Rectangle => new RectangleElement(),
        EditorTool.Ellipse => new EllipseElement(),
        EditorTool.Line => new LineElement(),
        EditorTool.Arrow => new ArrowElement(),
        EditorTool.Freehand => new FreehandElement(),
        EditorTool.Text => new TextElement(),
        EditorTool.SpeechBubble => new SpeechBubbleElement(),
        EditorTool.StepLabel => new StepLabelElement(),
        EditorTool.Highlight => new HighlightElement(),
        EditorTool.Obfuscate => new ObfuscateElement(),
        EditorTool.Emoji => new EmojiElement(),
        _ => null,
    };

    /// <summary>
    /// The style new elements of the tool get: the defaults with the last used values
    /// </summary>
    public ElementStyle ToolStyle(EditorTool tool)
    {
        if (!_toolStyles.TryGetValue(tool, out var style))
        {
            var prototype = CreateElement(tool);
            style = prototype == null ? new ElementStyle() : _settings?.StyleFor(prototype) ?? prototype.CreateDefaultStyle();
            _toolStyles[tool] = style;
        }

        return style;
    }

    #region Property bar

    /// <summary>
    /// The fields to show: the ones all selected elements have, or the fields of the tool
    /// </summary>
    public IReadOnlyList<FieldType> VisibleFields
    {
        get
        {
            if (Surface.Selection.Count > 0)
            {
                IEnumerable<FieldType> fields = Surface.Selection[0].Fields;
                foreach (var element in Surface.Selection.Skip(1))
                {
                    fields = fields.Intersect(element.Fields);
                }

                return fields.ToList();
            }

            if (_tool == EditorTool.Crop)
            {
                return [FieldType.CropMode, FieldType.CutMarkStyle];
            }

            var prototype = CreateElement(_tool);
            if (prototype == null)
            {
                return [];
            }

            prototype.Style = ToolStyle(_tool);
            return prototype.Fields;
        }
    }

    public object GetFieldValue(FieldType field) => field switch
    {
        FieldType.CropMode => CropMode,
        FieldType.CutMarkStyle when Surface.Selection.Count == 0 && _tool == EditorTool.Crop => CropCutMarkStyle,
        _ => Surface.Selection.Count > 0 ? Surface.Selection[0].Style.Get(field) : ToolStyle(_tool).Get(field),
    };

    /// <summary>
    /// Change a field of the selected elements, or of the tool when nothing is selected; the value is remembered
    /// </summary>
    public void SetFieldValue(FieldType field, object value)
    {
        if (field == FieldType.CropMode)
        {
            CropMode = (CropMode)value;
            return;
        }

        if (field == FieldType.CutMarkStyle && Surface.Selection.Count == 0 && _tool == EditorTool.Crop)
        {
            CropCutMarkStyle = (CutMarkStyle)value;
            return;
        }

        if (Surface.Selection.Count > 0)
        {
            Surface.Checkpoint();
            foreach (var element in Surface.Selection.Where(e => e.Fields.Contains(field)))
            {
                element.SetField(field, value);
                _settings?.RememberField(element, field, value);
                var tool = ToolFor(element);
                if (tool != null)
                {
                    ToolStyle(tool.Value).Set(field, value);
                }
            }

            Surface.Invalidate();
        }
        else if (CreateElement(_tool) is { } prototype)
        {
            ToolStyle(_tool).Set(field, value);
            _settings?.RememberField(prototype, field, value);
        }

        if (value is SKColor color && color.Alpha > 0)
        {
            _settings?.AddRecentColor(color);
        }

        StateChanged?.Invoke();
    }

    private static EditorTool? ToolFor(DrawableElement element) => element switch
    {
        SpeechBubbleElement => EditorTool.SpeechBubble,
        TextElement => EditorTool.Text,
        RectangleElement => EditorTool.Rectangle,
        EllipseElement => EditorTool.Ellipse,
        ArrowElement => EditorTool.Arrow,
        LineElement => EditorTool.Line,
        FreehandElement => EditorTool.Freehand,
        StepLabelElement => EditorTool.StepLabel,
        HighlightElement => EditorTool.Highlight,
        ObfuscateElement => EditorTool.Obfuscate,
        EmojiElement => EditorTool.Emoji,
        _ => null,
    };

    #endregion

    #region Mouse

    public void PointerPressed(SKPoint point, InputModifiers modifiers, int clickCount = 1)
    {
        _lastPoint = point;
        _dragStart = point;
        bool shift = modifiers.HasFlag(InputModifiers.Shift);
        bool control = modifiers.HasFlag(InputModifiers.Control);

        if (_tool == EditorTool.Crop)
        {
            PressCrop(point);
            return;
        }

        // The handles of the selection win over everything, in every tool
        if (FindHandle(point) is { } hit)
        {
            _active = hit.Element;
            _activeHandle = hit.Handle;
            _drag = Drag.Resizing;
            _checkpointPending = true;
            return;
        }

        var element = Surface.ElementAt(point, Tolerance);
        if (clickCount >= 2 && element is { HasText: true })
        {
            Surface.Select(element);
            TextEditRequested?.Invoke(element);
            return;
        }

        if (clickCount >= 2 && element is EmojiElement emoji)
        {
            Surface.Select(emoji);
            EmojiPickRequested?.Invoke(emoji);
            return;
        }

        if (_tool == EditorTool.Select)
        {
            if (element != null)
            {
                if (shift || control)
                {
                    Surface.ToggleSelection(element);
                }
                else if (!Surface.Selection.Contains(element))
                {
                    Surface.Select(element);
                }

                _drag = Surface.Selection.Count > 0 ? Drag.Moving : Drag.None;
                _checkpointPending = true;
            }
            else
            {
                if (!shift)
                {
                    Surface.ClearSelection();
                }

                _drag = Drag.RubberBand;
                RubberBand = new SKRect(point.X, point.Y, point.X, point.Y);
                StateChanged?.Invoke();
            }

            return;
        }

        StartCreating(point);
    }

    public void PointerMoved(SKPoint point, InputModifiers modifiers)
    {
        bool shift = modifiers.HasFlag(InputModifiers.Shift);
        float dx = point.X - _lastPoint.X;
        float dy = point.Y - _lastPoint.Y;
        _lastPoint = point;

        switch (_drag)
        {
            case Drag.Creating when _active != null:
                _active.UpdateCreate(point, shift);
                Surface.Invalidate();
                break;
            case Drag.Moving:
                if (dx == 0 && dy == 0)
                {
                    return;
                }

                CheckpointOnce();
                foreach (var element in Surface.Selection)
                {
                    element.MoveBy(dx, dy);
                }

                Surface.Invalidate();
                break;
            case Drag.Resizing when _active != null:
                CheckpointOnce();
                _active.DragHandle(_activeHandle, point, shift);
                Surface.Invalidate();
                break;
            case Drag.RubberBand:
                RubberBand = new SKRect(_dragStart.X, _dragStart.Y, point.X, point.Y);
                StateChanged?.Invoke();
                break;
            case Drag.Cropping:
                CropArea = SpanCrop(ClampToImage(new SKRect(_dragStart.X, _dragStart.Y, point.X, point.Y).Standardized));
                StateChanged?.Invoke();
                break;
            case Drag.MovingCrop when CropArea is { } crop:
                crop.Offset(_cropMode == CropMode.Horizontal ? 0 : dx, _cropMode == CropMode.Vertical ? 0 : dy);
                CropArea = crop;
                StateChanged?.Invoke();
                break;
            case Drag.ResizingCrop when CropArea is { } crop:
                var resized = new RectangleElement { Bounds = crop };
                resized.DragHandle(_activeHandle, point, shift);
                CropArea = SpanCrop(ClampToImage(resized.NormalizedBounds));
                StateChanged?.Invoke();
                break;
        }
    }

    public void PointerReleased(SKPoint point, InputModifiers modifiers)
    {
        var drag = _drag;
        _drag = Drag.None;
        switch (drag)
        {
            case Drag.Creating when _active != null:
                FinishCreating(_active);
                break;
            case Drag.RubberBand when RubberBand is { } band:
                var area = band.Standardized;
                if (area.Width > 2 || area.Height > 2)
                {
                    var selected = Surface.Elements.Where(e => e.NormalizedBounds.IntersectsWith(area));
                    Surface.SelectMany(modifiers.HasFlag(InputModifiers.Shift) ? Surface.Selection.Concat(selected).Distinct().ToList() : selected);
                }

                RubberBand = null;
                StateChanged?.Invoke();
                break;
            case Drag.Cropping when CropArea is { } crop && (_cropMode == CropMode.Horizontal ? crop.Height < 2 : crop.Width < 2 || (_cropMode == CropMode.Default && crop.Height < 2)):
                CropArea = null;
                StateChanged?.Invoke();
                break;
        }

        _active = null;
        _checkpointPending = false;
    }

    public EditorCursor CursorAt(SKPoint point)
    {
        if (_tool == EditorTool.Crop)
        {
            if (CropArea is { } crop)
            {
                var handle = FindCropHandle(crop, point);
                if (handle != null)
                {
                    return CursorFor(handle.Value);
                }

                if (crop.Contains(point))
                {
                    return EditorCursor.Move;
                }
            }

            return EditorCursor.Cross;
        }

        if (FindHandle(point) is { } hit)
        {
            return CursorFor(hit.Handle);
        }

        if (_tool == EditorTool.Select)
        {
            var element = Surface.ElementAt(point, Tolerance);
            return element != null ? EditorCursor.Move : EditorCursor.Default;
        }

        return EditorCursor.Cross;
    }

    private void StartCreating(SKPoint point)
    {
        var element = CreateElement(_tool);
        if (element == null)
        {
            return;
        }

        element.Style = ToolStyle(_tool).Clone();
        if (element is FreehandElement freehand && _settings != null)
        {
            freehand.Sensitivity = _settings.FreehandSensitivity;
        }

        if (element is EmojiElement emoji)
        {
            emoji.Emoji = _settings?.RecentEmoji ?? EmojiElement.DefaultEmoji;
        }

        Surface.Checkpoint();
        element.BeginCreate(point);
        Surface.ClearSelection();
        Surface.Add(element, select: false);
        _active = element;
        _drag = Drag.Creating;
    }

    private void FinishCreating(DrawableElement element)
    {
        if (!element.EndCreate())
        {
            Surface.Remove([element]);
            Surface.DiscardCheckpoint();
            return;
        }

        Surface.Select(element);
        if (element.HasText)
        {
            _textElementNew = element;
            TextEditRequested?.Invoke(element);
        }
        else if (element is EmojiElement emoji)
        {
            // Choose the emoji right after placing it, like on Windows
            EmojiPickRequested?.Invoke(emoji);
        }
    }

    private void CheckpointOnce()
    {
        if (_checkpointPending)
        {
            Surface.Checkpoint();
            _checkpointPending = false;
        }
    }

    private (DrawableElement Element, HandlePosition Handle)? FindHandle(SKPoint point)
    {
        // The handles of the top most selected element first
        foreach (var element in Surface.Selection.Reverse())
        {
            foreach (var handle in element.GetHandles())
            {
                if ((handle.Location - point).Length <= HandleRadius)
                {
                    return (element, handle.Position);
                }
            }
        }

        return null;
    }

    private static EditorCursor CursorFor(HandlePosition handle) => handle switch
    {
        HandlePosition.TopLeft or HandlePosition.BottomRight => EditorCursor.SizeNorthWestSouthEast,
        HandlePosition.TopRight or HandlePosition.BottomLeft => EditorCursor.SizeNorthEastSouthWest,
        HandlePosition.Top or HandlePosition.Bottom => EditorCursor.SizeNorthSouth,
        HandlePosition.Left or HandlePosition.Right => EditorCursor.SizeWestEast,
        _ => EditorCursor.Hand,
    };

    #endregion

    #region Text

    /// <summary>
    /// The text box of the view is done: an empty new text box is removed, like on Windows
    /// </summary>
    public void CommitText(DrawableElement element, string text)
    {
        bool isNew = element == _textElementNew;
        _textElementNew = null;
        var current = element switch
        {
            TextElement textElement => textElement.Text,
            _ => null,
        };
        if (current == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(text) && isNew && element is not SpeechBubbleElement)
        {
            Surface.Remove([element]);
            Surface.DiscardCheckpoint();
            return;
        }

        if (text == current)
        {
            return;
        }

        // A new element is in the undo history with its creation, the text is part of it
        if (!isNew)
        {
            Surface.Checkpoint();
        }

        ((TextElement)element).Text = text;
        Surface.Invalidate();
    }

    #endregion

    #region Crop

    private void PressCrop(SKPoint point)
    {
        if (CropArea is { } crop)
        {
            if (FindCropHandle(crop, point) is { } handle)
            {
                _activeHandle = handle;
                _drag = Drag.ResizingCrop;
                return;
            }

            if (crop.Contains(point))
            {
                _drag = Drag.MovingCrop;
                return;
            }
        }

        CropArea = SpanCrop(new SKRect(point.X, point.Y, point.X, point.Y));
        _drag = Drag.Cropping;
        StateChanged?.Invoke();
    }

    private HandlePosition? FindCropHandle(SKRect crop, SKPoint point)
    {
        var handles = CropHandlesOf(crop);
        foreach (var handle in handles)
        {
            if ((handle.Location - point).Length <= HandleRadius)
            {
                return handle.Position;
            }
        }

        return null;
    }

    public IReadOnlyList<Handle> CropHandles => CropArea is { } crop ? CropHandlesOf(crop) : [];

    /// <summary>
    /// The handles of the crop area: a cut out can only be sized across the cut
    /// </summary>
    private IReadOnlyList<Handle> CropHandlesOf(SKRect crop) => new RectangleElement { Bounds = crop }.GetHandles()
        .Where(h => _cropMode switch
        {
            CropMode.Vertical => h.Position is HandlePosition.Left or HandlePosition.Right,
            CropMode.Horizontal => h.Position is HandlePosition.Top or HandlePosition.Bottom,
            _ => true,
        })
        .ToList();

    /// <summary>
    /// A cut out spans the whole image along the cut
    /// </summary>
    private SKRect SpanCrop(SKRect crop) => _cropMode switch
    {
        CropMode.Vertical => new SKRect(crop.Left, 0, crop.Right, Surface.Height),
        CropMode.Horizontal => new SKRect(0, crop.Top, Surface.Width, crop.Bottom),
        _ => crop,
    };

    /// <summary>
    /// C again while cropping: the next crop mode
    /// </summary>
    public void CycleCropMode() => CropMode = _cropMode switch
    {
        CropMode.Default => CropMode.Vertical,
        CropMode.Vertical => CropMode.Horizontal,
        _ => CropMode.Default,
    };

    /// <summary>
    /// Enter or the confirm button: crop to the area
    /// </summary>
    public bool ConfirmCrop()
    {
        if (CropArea is not { } crop)
        {
            return false;
        }

        var area = SKRectI.Round(crop);
        area.Intersect(new SKRectI(0, 0, Surface.Width, Surface.Height));
        CropArea = null;
        if (area.Width < 1 || area.Height < 1)
        {
            StateChanged?.Invoke();
            return false;
        }

        bool done = _cropMode switch
        {
            CropMode.Vertical => Surface.ApplyCutOut(area, horizontal: false, CropCutMarkStyle, TornEdgeSettings),
            CropMode.Horizontal => Surface.ApplyCutOut(area, horizontal: true, CropCutMarkStyle, TornEdgeSettings),
            _ => Surface.ApplyCrop(area, CropCutMarkStyle, TornEdgeSettings),
        };
        Tool = EditorTool.Select;
        return done;
    }

    public void CancelCrop()
    {
        if (CropArea != null)
        {
            CropArea = null;
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Remove the border of one color around the image
    /// </summary>
    public bool AutoCrop()
    {
        var area = ImageEffects.FindAutoCropArea(Surface.Image);
        if (area == null)
        {
            return false;
        }

        ApplyEffect(image => ImageEffects.Crop(image, area.Value));
        return true;
    }

    private SKRect ClampToImage(SKRect rect)
    {
        rect.Intersect(new SKRect(0, 0, Surface.Width, Surface.Height));
        return rect;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Run an effect on the image; the elements move with it
    /// </summary>
    public void ApplyEffect(Func<SKBitmap, EffectResult> effect)
    {
        var result = effect(Surface.Image);
        Surface.Checkpoint();
        Surface.ReplaceImage(result.Image, result.ElementTransform);
    }

    /// <summary>
    /// The emoji chosen in the picker for the element; it is remembered for the next one
    /// </summary>
    public void SetEmoji(EmojiElement element, string emoji)
    {
        if (_settings != null)
        {
            _settings.RecentEmoji = emoji;
        }

        if (element.Emoji == emoji)
        {
            return;
        }

        Surface.Checkpoint();
        element.Emoji = emoji;
        Surface.Invalidate();
    }

    /// <summary>
    /// The drop shadow effect with its settings
    /// </summary>
    public void DropShadow() => ApplyEffect(image => ImageEffects.DropShadow(image, _settings?.DropShadowEffectSettings ?? new DropShadowSettings()));

    /// <summary>
    /// Torn edges as an element, which can be changed or removed later
    /// </summary>
    public void TornEdges()
    {
        Tool = EditorTool.Select;
        Surface.AddTornEdges(TornEdgeSettings);
    }

    /// <summary>
    /// The selected cut edges or torn edges, null when none is selected
    /// </summary>
    public IReadOnlyList<CutElement> SelectedCuts => Surface.Selection.OfType<CutElement>().ToList();

    /// <summary>
    /// New random edges for the selected cuts
    /// </summary>
    public void NewRandomEdges()
    {
        if (SelectedCuts.Count == 0)
        {
            return;
        }

        Surface.Checkpoint();
        foreach (var cut in SelectedCuts)
        {
            cut.NewSeed();
        }

        Surface.Invalidate();
    }

    /// <summary>
    /// Change a cut with the settings of the settings window
    /// </summary>
    public void ApplyCutSettings(CutElement cut, TornEdgeSettings settings)
    {
        Surface.Checkpoint();
        cut.ApplySettings(settings);
        Surface.Invalidate();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Draw the selected cuts into the image
    /// </summary>
    public void ApplyCutsToImage()
    {
        foreach (var cut in SelectedCuts)
        {
            Surface.ApplyElementToImage(cut);
        }

        StateChanged?.Invoke();
    }

    public void SelectAll()
    {
        Tool = EditorTool.Select;
        Surface.SelectMany(Surface.Elements);
    }

    public void DeleteSelection()
    {
        if (Surface.Selection.Count == 0)
        {
            return;
        }

        Surface.Checkpoint();
        Surface.Remove(Surface.Selection.ToList());
    }

    /// <summary>
    /// Arrow keys move the selection, 10 pixels with Shift or Control
    /// </summary>
    public bool MoveSelection(float dx, float dy)
    {
        if (Surface.Selection.Count == 0)
        {
            return false;
        }

        Surface.Checkpoint();
        foreach (var element in Surface.Selection)
        {
            element.MoveBy(dx, dy);
        }

        Surface.Invalidate();
        return true;
    }

    /// <summary>
    /// The selected elements as JSON, for the clipboard; null when nothing is selected
    /// </summary>
    public string? CopySelection()
    {
        if (Surface.Selection.Count == 0)
        {
            return null;
        }

        var ordered = Surface.Elements.Where(Surface.Selection.Contains);
        _pastedJson = ElementSerializer.Serialize(ordered, Surface.CounterStart);
        _pasteCount = 0;
        return _pastedJson;
    }

    public string? CutSelection()
    {
        var json = CopySelection();
        if (json != null)
        {
            DeleteSelection();
            // Cut elements come back at their place
            _pasteCount = -1;
        }

        return json;
    }

    /// <summary>
    /// Paste elements; pasting the same elements again moves them 10 pixels further each time
    /// </summary>
    public void PasteElements(string json)
    {
        var (elements, _) = ElementSerializer.Deserialize(json);
        if (elements.Count == 0)
        {
            return;
        }

        if (json == _pastedJson)
        {
            _pasteCount++;
        }
        else
        {
            _pastedJson = json;
            _pasteCount = 1;
        }

        Surface.Checkpoint();
        float offset = 10 * Math.Max(0, _pasteCount);
        foreach (var element in elements)
        {
            element.MoveBy(offset, offset);
            Surface.Add(element, select: false);
        }

        Tool = EditorTool.Select;
        Surface.SelectMany(elements);
    }

    /// <summary>
    /// Paste an image from the clipboard as element, in the middle of the visible area or the image
    /// </summary>
    public void PasteImage(SKBitmap bitmap, SKPoint? center = null)
    {
        var middle = center ?? new SKPoint(Surface.Width / 2f, Surface.Height / 2f);
        var element = ImageElement.FromBitmap(bitmap, new SKPoint(middle.X - bitmap.Width / 2f, middle.Y - bitmap.Height / 2f));
        Surface.Checkpoint();
        Tool = EditorTool.Select;
        Surface.Add(element);
    }

    public void Duplicate()
    {
        var json = CopySelection();
        if (json != null)
        {
            PasteElements(json);
        }
    }

    /// <summary>
    /// Esc: stop cropping, else deselect and go back to the select tool
    /// </summary>
    public void Escape()
    {
        if (_tool == EditorTool.Crop && CropArea != null)
        {
            CancelCrop();
            return;
        }

        Surface.ClearSelection();
        Tool = EditorTool.Select;
    }

    #endregion
}
