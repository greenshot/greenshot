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
/// What was chosen on the capture screen
/// </summary>
public sealed record SelectionResult(SKRectI Pixels, CaptureMode Mode, string? Title);

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
    public SKRectI Highlight => IsSelecting ? Selection : HoveredWindow?.Pixels ?? SKRectI.Empty;

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
        if (IsCompleted || Mode != CaptureMode.Region || Anchor is not { } anchor)
        {
            return;
        }

        bool isClick = Math.Abs(anchor.X - Cursor.X) < ClickTolerance && Math.Abs(anchor.Y - Cursor.Y) < ClickTolerance;
        if (!isClick)
        {
            Complete(new SelectionResult(Selection, CaptureMode.Region, null));
            return;
        }

        // Like on Windows: a click in the region mode captures the window under the cursor
        if (HoveredWindow != null)
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
            Complete(new SelectionResult(Selection, CaptureMode.Region, null));
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

        Mode = Mode == CaptureMode.Window ? CaptureMode.Region : CaptureMode.Window;
        Anchor = null;
        UpdateHoveredWindow();
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
        Completed?.Invoke(result);
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
