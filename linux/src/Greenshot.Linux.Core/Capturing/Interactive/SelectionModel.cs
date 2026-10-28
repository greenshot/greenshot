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

using SkiaSharp;

namespace Greenshot.Linux.Core.Capturing.Interactive;

/// <summary>
/// A window which can be picked on the capture screen, in image pixels of the snapshot
/// </summary>
public sealed record SelectableWindow(string Title, SKRectI Pixels);

/// <summary>
/// A line of text found on the screen for the text mode, with its words, in image pixels
/// </summary>
public sealed record TextLine(SKRectI Bounds, IReadOnlyList<SKRectI> Words);

/// <summary>
/// What was chosen on the capture screen
/// </summary>
public sealed record SelectionResult(SKRectI Pixels, CaptureMode Mode, string? Title)
{
    /// <summary>
    /// The QR codes found in the selection
    /// </summary>
    public IReadOnlyList<Barcodes.DetectedBarcode> Barcodes { get; init; } = [];
}

/// <summary>
/// The state of the capture screen, shared by the windows on all monitors: the mode, the cursor, the selection
/// and the helpers which are shown. All positions are image pixels of the snapshot.
/// The keys and mouse actions are the ones of the capture screen of Greenshot 1.4, see docs/vNext/capture-user-guide.md.
/// </summary>
public sealed class SelectionModel
{
    /// <summary>
    /// A press and release closer than this is a click, not a drag
    /// </summary>
    public const int ClickTolerance = 3;

    private readonly SKRectI _bounds;
    private readonly IReadOnlyList<SelectableWindow> _windows;
    private int _windowIndex = -1;

    public SelectionModel(SKRectI imageBounds, IReadOnlyList<SelectableWindow> windows, CaptureMode mode, SKPointI cursor)
    {
        _bounds = imageBounds;
        _windows = windows
            .Select(w => w with { Pixels = SKRectI.Intersect(w.Pixels, imageBounds) })
            .Where(w => !w.Pixels.IsEmpty)
            .ToList();
        Mode = mode == CaptureMode.Window && _windows.Count > 0 ? CaptureMode.Window : CaptureMode.Region;
        Cursor = Clamp(cursor);
        UpdateHoveredWindow();
    }

    public event Action? Changed;

    /// <summary>
    /// The capture is finished (Result set) or cancelled (Result null)
    /// </summary>
    public event Action<SelectionResult?>? Completed;

    /// <summary>
    /// The text mode started: the screen should be read, then <see cref="SetTextLines"/> called
    /// </summary>
    public event Action? TextRecognitionRequested;

    /// <summary>
    /// The lines of text on the screen, null while they are not known yet
    /// </summary>
    public IReadOnlyList<TextLine>? TextLines { get; private set; }

    /// <summary>
    /// Shown in the text mode: reading the screen, or why it can't
    /// </summary>
    public string? TextStatus { get; private set; }

    /// <summary>
    /// The line under the cursor in the text mode
    /// </summary>
    public TextLine? HoveredLine => Mode == CaptureMode.Text && !IsSelecting
        ? TextLines?.FirstOrDefault(l => Inflate(l.Bounds, 2).Contains(Cursor.X, Cursor.Y))
        : null;

    /// <summary>
    /// The words inside the selection while dragging in the text mode
    /// </summary>
    public IEnumerable<SKRectI> SelectedWords => Mode == CaptureMode.Text && IsSelecting && TextLines != null
        ? TextLines.SelectMany(l => l.Words).Where(w => w.IntersectsWith(Selection))
        : [];

    /// <summary>
    /// The QR codes found on the screen, shown as frames which can be clicked (the hotspots of Windows)
    /// </summary>
    public IReadOnlyList<Barcodes.DetectedBarcode> Hotspots { get; private set; } = [];

    /// <summary>
    /// The QR code under the cursor, while not selecting
    /// </summary>
    public Barcodes.DetectedBarcode? HoveredHotspot => IsSelecting || !IsCursorKnown || Mode == CaptureMode.Text
        ? null
        : Hotspots.FirstOrDefault(h => h.Bounds.Contains(Cursor.X, Cursor.Y));

    /// <summary>
    /// A frame of a QR code was clicked: its menu should be shown instead of selecting
    /// </summary>
    public event Action<Barcodes.DetectedBarcode>? HotspotClicked;

    /// <summary>
    /// The scan of the screen found these QR codes (it runs in the background while selecting)
    /// </summary>
    public void SetHotspots(IReadOnlyList<Barcodes.DetectedBarcode> hotspots)
    {
        Hotspots = hotspots.Where(h => !h.Bounds.IsEmpty).ToList();
        Changed?.Invoke();
    }

    public void SetTextStatus(string status)
    {
        TextStatus = status;
        Changed?.Invoke();
    }

    public void SetTextLines(IReadOnlyList<TextLine> lines, string? status = null)
    {
        TextLines = lines;
        TextStatus = status;
        Changed?.Invoke();
    }

    private static SKRectI Inflate(SKRectI rect, int size) => new(rect.Left - size, rect.Top - size, rect.Right + size, rect.Bottom + size);

    public CaptureMode Mode { get; private set; }

    public bool CanSelectWindows => _windows.Count > 0;

    public SKPointI Cursor { get; private set; }

    /// <summary>
    /// Wayland doesn't tell where the pointer is until it moves; until then the cursor is only a starting point
    /// for the arrow keys and the crosshair and zoomer are not shown
    /// </summary>
    public bool IsCursorKnown { get; private set; }

    /// <summary>
    /// Where the region selection started, null while not selecting
    /// </summary>
    public SKPointI? Anchor { get; private set; }

    public bool IsSelecting => Anchor != null;

    public bool ShowZoomer { get; private set; } = true;

    public bool ShowHelp { get; private set; }

    public bool ShowInfo { get; private set; }

    public bool IsCompleted { get; private set; }

    public SelectableWindow? HoveredWindow => _windowIndex >= 0 ? _windows[_windowIndex] : null;

    /// <summary>
    /// The rectangle between the anchor and the cursor, both pixels included
    /// </summary>
    public SKRectI Selection
    {
        get
        {
            if (Anchor is not { } anchor)
            {
                return SKRectI.Empty;
            }

            return new SKRectI(Math.Min(anchor.X, Cursor.X), Math.Min(anchor.Y, Cursor.Y),
                Math.Max(anchor.X, Cursor.X) + 1, Math.Max(anchor.Y, Cursor.Y) + 1);
        }
    }

    /// <summary>
    /// What would be captured right now: the selection, or the window under the cursor
    /// </summary>
    public SKRectI Highlight => IsSelecting
        ? Selection
        : Mode == CaptureMode.Text ? HoveredLine?.Bounds ?? SKRectI.Empty : HoveredWindow?.Pixels ?? SKRectI.Empty;

    /// <summary>
    /// Region and text select a rectangle, window picks a window
    /// </summary>
    public bool SelectsRectangle => Mode is CaptureMode.Region or CaptureMode.Text;

    public void SetZoomer(bool show)
    {
        ShowZoomer = show;
        Changed?.Invoke();
    }

    public void MoveTo(SKPointI point)
    {
        var clamped = Clamp(point);
        if (clamped == Cursor && IsCursorKnown)
        {
            return;
        }

        IsCursorKnown = true;
        Cursor = clamped;
        UpdateHoveredWindow();
        Changed?.Invoke();
    }

    public void MoveBy(int dx, int dy) => MoveTo(new SKPointI(Cursor.X + dx, Cursor.Y + dy));

    public void Press()
    {
        if (IsCompleted)
        {
            return;
        }

        if (HoveredHotspot is { } hotspot)
        {
            HotspotClicked?.Invoke(hotspot);
            return;
        }

        if (Mode == CaptureMode.Window)
        {
            CompleteWithWindow();
            return;
        }

        Anchor = Cursor;
        Changed?.Invoke();
    }

    public void Release()
    {
        if (IsCompleted || !SelectsRectangle || Anchor is not { } anchor)
        {
            return;
        }

        bool isClick = Math.Abs(anchor.X - Cursor.X) < ClickTolerance && Math.Abs(anchor.Y - Cursor.Y) < ClickTolerance;
        if (!isClick)
        {
            Complete(new SelectionResult(Selection, Mode, null));
            return;
        }

        // Like on Windows: a click in the text mode captures the line under the cursor
        if (Mode == CaptureMode.Text && TextLines?.FirstOrDefault(l => Inflate(l.Bounds, 2).Contains(Cursor.X, Cursor.Y)) is { } line)
        {
            Complete(new SelectionResult(Inflate(line.Bounds, 3), CaptureMode.Text, null));
            return;
        }

        // Like on Windows: a click in the region mode captures the window under the cursor
        if (Mode == CaptureMode.Region && HoveredWindow != null)
        {
            CompleteWithWindow();
            return;
        }

        Anchor = null;
        Changed?.Invoke();
    }

    /// <summary>
    /// Enter: start or finish the selection in the region mode, capture the window in the window mode
    /// </summary>
    public void Confirm()
    {
        if (IsCompleted)
        {
            return;
        }

        if (Mode == CaptureMode.Window)
        {
            CompleteWithWindow();
        }
        else if (Anchor == null)
        {
            Anchor = Cursor;
            Changed?.Invoke();
        }
        else
        {
            Complete(new SelectionResult(Selection, Mode, null));
        }
    }

    public void Cancel()
    {
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;
        Completed?.Invoke(null);
    }

    /// <summary>
    /// Space: switch between the region and the window mode
    /// </summary>
    public void ToggleMode()
    {
        if (!CanSelectWindows)
        {
            return;
        }

        // Like on Windows: from the text mode Space goes back to the region mode
        Mode = Mode == CaptureMode.Region ? CaptureMode.Window : CaptureMode.Region;
        Anchor = null;
        UpdateHoveredWindow();
        Changed?.Invoke();
    }

    /// <summary>
    /// T: capture text, again T goes back to the region mode
    /// </summary>
    public void ToggleTextMode()
    {
        Mode = Mode == CaptureMode.Text ? CaptureMode.Region : CaptureMode.Text;
        Anchor = null;
        if (Mode == CaptureMode.Text && TextLines == null && TextStatus == null)
        {
            // The screen is read once, the first time the text mode starts
            TextStatus = string.Empty;
            TextRecognitionRequested?.Invoke();
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Tab / Shift+Tab in the window mode: the next or previous window
    /// </summary>
    public void SelectNextWindow(bool backwards)
    {
        if (Mode != CaptureMode.Window || _windows.Count == 0)
        {
            return;
        }

        _windowIndex = _windowIndex < 0
            ? 0
            : (_windowIndex + (backwards ? _windows.Count - 1 : 1)) % _windows.Count;
        var pixels = _windows[_windowIndex].Pixels;
        Cursor = new SKPointI(pixels.MidX, pixels.MidY);
        Changed?.Invoke();
    }

    public void ToggleZoomer() => SetZoomer(!ShowZoomer);

    public void ToggleHelp()
    {
        ShowHelp = !ShowHelp;
        Changed?.Invoke();
    }

    public void ToggleInfo()
    {
        ShowInfo = !ShowInfo;
        Changed?.Invoke();
    }

    private void CompleteWithWindow()
    {
        if (HoveredWindow is { } window)
        {
            Complete(new SelectionResult(window.Pixels, CaptureMode.Window, window.Title));
        }
    }

    private void Complete(SelectionResult result)
    {
        if (result.Pixels.IsEmpty)
        {
            return;
        }

        IsCompleted = true;
        var pixels = result.Pixels;
        Completed?.Invoke(result with
        {
            Barcodes = Hotspots.Where(h => pixels.Contains(h.Bounds.MidX, h.Bounds.MidY)).ToList(),
        });
    }

    /// <summary>
    /// The top most window under the cursor; the windows are ordered top most first
    /// </summary>
    private void UpdateHoveredWindow()
    {
        _windowIndex = -1;
        for (int i = 0; i < _windows.Count; i++)
        {
            if (_windows[i].Pixels.Contains(Cursor.X, Cursor.Y))
            {
                _windowIndex = i;
                return;
            }
        }
    }

    private SKPointI Clamp(SKPointI point) => new(
        Math.Clamp(point.X, _bounds.Left, _bounds.Right - 1),
        Math.Clamp(point.Y, _bounds.Top, _bounds.Bottom - 1));
}
