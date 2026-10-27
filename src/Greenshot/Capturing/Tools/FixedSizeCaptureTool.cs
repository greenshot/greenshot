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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.Capturing.Tools
{
    /// <summary>
    /// A rectangle of a chosen size follows the mouse, a click or Enter captures it. The cursor holds it at a corner (or in the middle,
    /// C changes it, FixedCaptureAnchor), so the zoomer shows that corner. Its edges snap to the edges of the windows under the cursor
    /// and of the monitor, with Alt held it is placed freely. The sizes are those of the settings (FixedCaptureSizes), followed by the sizes
    /// of the last region captures (RecentCaptureAreas); a panel lists them, 1-9 and 0 pick one, PageUp and PageDown go up and down the list.
    /// An entry can have a place (a configured one, or where a recent capture was, FixedCaptureRemembers): on a monitor, or in the windows
    /// of a program. Picking the entry moves the cursor so the area is there (FixedCaptureMovesCursor), the places are drawn as outlines,
    /// and the area snaps onto one when it comes close. When a place on a monitor isn't on the screens (e.g. its monitor is switched off),
    /// the same place on the monitor with the cursor is used, e.g. for a full screen window.
    /// S starts typing a size: digits, X to the height, Backspace, Enter takes it, S again stops.
    /// </summary>
    public class FixedSizeCaptureTool : CaptureTool
    {
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        // The number of remembered sizes of region captures
        private const int RecentSizesCount = 5;
        // The number keys 1-9 and 0 pick the first ten entries
        private const int NumberKeysCount = 10;
        // An edge closer than this (device independent units) to the edge of a window snaps to it
        private const double SnapDistance = 8;
        // The area closer than this (device independent units) to the place of its entry snaps onto it
        private const double PlaceSnapDistance = 24;

        private List<(FixedCaptureArea Area, bool Recent)> _sizes = new List<(FixedCaptureArea Area, bool Recent)>();
        private int _index;
        // The places of the current entry in capture coordinates which fit, the one the cursor goes to first
        private List<NativeRect> _places = new List<NativeRect>();
        // The program names of the windows, read once per activation
        private readonly Dictionary<IntPtr, string> _processNames = new Dictionary<IntPtr, string>();
        // Windows of the shell (desktop, taskbar) aren't windows of a program to remember a place in
        private static readonly string[] ShellWindowClasses = { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };
        private NativeRect _selection = NativeRect.Empty;
        // Typing a size (S): the digits of the width and the height, null when not typing
        private string _typedWidth;
        private string _typedHeight;
        private bool _typingHeight;
        // The largest width or height which can be typed
        private const int TypedDigitsCount = 5;

        private bool IsTyping => _typedWidth != null;

        public override CaptureMode Mode => CaptureMode.Region;

        public override string Id => "FixedSize";

        /// <summary>
        /// F switches to this tool from every other tool
        /// </summary>
        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterKey(this, Key.F, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSize, () => host.ActivateTool(this));
            host.RegisterToolKey(this, Key.Return, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeAccept, AcceptSelection);
            for (int i = 0; i < NumberKeysCount; i++)
            {
                int index = i;
                host.RegisterToolKey(this, GetNumberKey(i), ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizePick, () => PickSize(index));
            }
            host.RegisterToolKey(this, Key.PageUp, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeStep, () => StepSize(-1));
            host.RegisterToolKey(this, Key.PageDown, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeStep, () => StepSize(1));
            host.RegisterToolKey(this, Key.C, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeCorner, NextAnchor);
            host.RegisterToolKey(this, Key.S, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeType, ToggleTyping);
            host.RegisterToolKey(this, Key.X, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeTyping, TypeHeight);
            host.RegisterToolKey(this, Key.Back, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeTyping, DeleteTypedDigit);
        }

        public override void Activate(ICaptureToolHost host)
        {
            base.Activate(host);
            _sizes = GetSizes(Conf.FixedCaptureSizes, Conf.RecentCaptureAreas);
            // The entry of the last fixed size capture again, or one of its size
            var last = FixedCaptureArea.Parse(Conf.FixedCaptureArea);
            _index = _sizes.FindIndex(entry => entry.Area == last);
            if (_index < 0)
            {
                _index = Math.Max(0, _sizes.FindIndex(entry => entry.Area.Size == last.Size));
            }
            _processNames.Clear();
            _typedWidth = _typedHeight = null;
            _places = GetPlaces(_sizes.Count == 0 ? default : _sizes[_index].Area);
            _selection = NativeRect.Empty;
            OnMouseMove();
            // Once the window is shown, when it starts with this tool: then the monitor and its DPI are known
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (Host.ActiveTool == this)
                {
                    ShowSizes();
                    MoveCursorToPlace();
                }
            }), DispatcherPriority.ContextIdle);
        }

        public override void Deactivate()
        {
            Host.HidePanel(this);
            Host.HideSelection();
            Host.ClearLabels();
        }

        public override void OnMouseMove()
        {
            if (_sizes.Count == 0)
            {
                return;
            }
            var rect = Place(Host.CursorPosition, _sizes[_index].Area.Size, Conf.FixedCaptureAnchor);
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
            {
                int placeDistance = Scale(PlaceSnapDistance);
                var places = _places.Where(place => IsClose(rect, place, placeDistance)).ToList();
                rect = places.Count > 0 ? places[0] : Snap(rect, GetSnapBounds(), Scale(SnapDistance));
            }
            var captureBounds = new NativeRect(0, 0, Host.ScreenBounds.Width, Host.ScreenBounds.Height);
            // At a place the area may be on more than one monitor, it fits on the screens
            _selection = _places.Contains(rect)
                ? rect
                : KeepInside(KeepInside(rect, Host.GetMonitorBounds()), captureBounds).Intersect(captureBounds);
            Host.ShowSelection(_selection);
            Host.ShowLabels(_selection, _selection.Size);
        }

        public override void OnMouseUp() => AcceptSelection();

        private void AcceptSelection()
        {
            if (IsTyping)
            {
                // Enter takes the typed size, the capture follows with the next Enter or click
                TakeTypedSize();
                return;
            }
            if (_selection.IsEmpty)
            {
                return;
            }
            Conf.FixedCaptureArea = _sizes[_index].Area.ToString();
            RememberArea(_selection.Offset(Host.ScreenBounds.X, Host.ScreenBounds.Y), Host.Windows);
            Host.Accept(_selection);
        }

        /// <summary>
        /// A number key: the size at that place in the list, when there is one
        /// </summary>
        private void PickSize(int index)
        {
            if (IsTyping)
            {
                TypeDigit(index < 9 ? index + 1 : 0);
                return;
            }
            if (index < _sizes.Count)
            {
                _index = index;
                _places = GetPlaces(_sizes[_index].Area);
                ShowSizes();
                Host.Redraw();
                MoveCursorToPlace();
                OnMouseMove();
            }
        }

        /// <summary>
        /// S: start typing a size, or stop it without changing the size
        /// </summary>
        private void ToggleTyping()
        {
            if (IsTyping)
            {
                _typedWidth = _typedHeight = null;
            }
            else
            {
                _typedWidth = _typedHeight = string.Empty;
                _typingHeight = false;
            }
            ShowSizes();
        }

        private void TypeDigit(int digit)
        {
            if (_typingHeight)
            {
                _typedHeight = AddDigit(_typedHeight, digit);
            }
            else
            {
                _typedWidth = AddDigit(_typedWidth, digit);
            }
            ShowSizes();
        }

        private static string AddDigit(string text, int digit) =>
            text.Length >= TypedDigitsCount || (text.Length == 0 && digit == 0) ? text : text + digit.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// X while typing: on to the height, once there is a width
        /// </summary>
        private void TypeHeight()
        {
            if (IsTyping && !_typingHeight && _typedWidth.Length > 0)
            {
                _typingHeight = true;
                ShowSizes();
            }
        }

        /// <summary>
        /// Backspace while typing: the last digit goes, from an empty height back to the width
        /// </summary>
        private void DeleteTypedDigit()
        {
            if (!IsTyping)
            {
                return;
            }
            if (_typingHeight && _typedHeight.Length == 0)
            {
                _typingHeight = false;
            }
            else if (_typingHeight)
            {
                _typedHeight = _typedHeight.Substring(0, _typedHeight.Length - 1);
            }
            else if (_typedWidth.Length > 0)
            {
                _typedWidth = _typedWidth.Substring(0, _typedWidth.Length - 1);
            }
            ShowSizes();
        }

        /// <summary>
        /// Enter while typing: the typed size becomes the current entry, it follows the cursor
        /// </summary>
        private void TakeTypedSize()
        {
            var size = ParseSize($"{_typedWidth}x{_typedHeight}");
            if (size.IsEmpty)
            {
                return;
            }
            _typedWidth = _typedHeight = null;
            var area = new FixedCaptureArea(size);
            int index = _sizes.FindIndex(entry => entry.Area == area);
            if (index < 0)
            {
                // Before the recent ones, it becomes one of them when it is captured
                index = _sizes.FindIndex(entry => entry.Recent);
                index = index < 0 ? _sizes.Count : index;
                _sizes.Insert(index, (area, true));
            }
            _index = index;
            _places = new List<NativeRect>();
            ShowSizes();
            Host.Redraw();
            OnMouseMove();
        }

        /// <summary>
        /// 1-9 for the first nine entries, 0 for the tenth, as on the keyboard
        /// </summary>
        private static Key GetNumberKey(int index) => index < 9 ? Key.D1 + index : Key.D0;

        private static string GetNumberKeyText(int index) => index < 9 ? (index + 1).ToString() : "0";

        private int Scale(double units) => (int)Math.Round(Host.ToolStyle?.Scale(units) ?? units);

        /// <summary>
        /// The places of an entry in capture coordinates which fit on the screens and in the capture, the one the cursor goes to first
        /// </summary>
        private List<NativeRect> GetPlaces(FixedCaptureArea area)
        {
            if (!area.HasPlace)
            {
                return new List<NativeRect>();
            }
            var screenBounds = Host.ScreenBounds;
            var screens = GetScreens().ToList();
            var cursor = Host.CursorPosition.Offset(screenBounds.X, screenBounds.Y);
            IEnumerable<NativeRect> places;
            if (area.Program != null)
            {
                var windows = Host.Windows
                    .Where(window => window.IsVisible() && !window.IsMinimized())
                    .Select(window => (Info: window.GetInfo(), Window: window))
                    .ToList();
                // Every window counts for covering the windows below it, only those of the program for a place
                var windowBounds = windows.Select(window => (window.Info.Bounds, window.Info.ClientBounds, IsProgram: area.IsProgram(GetProcessName(window.Window)))).ToList();
                places = ResolveWindowPlaces(area, windowBounds, screens, cursor);
            }
            else
            {
                places = ResolveMonitorPlace(area, screens, Host.GetMonitorBounds().Offset(screenBounds.X, screenBounds.Y)) is { } place
                    ? new[] { place }
                    : Enumerable.Empty<NativeRect>();
            }
            // In the capture too: it covers all screens, but the screens can have changed since it was taken
            return places
                .Where(place => screenBounds.Intersect(place) == place)
                .Select(place => place.Offset(-screenBounds.X, -screenBounds.Y))
                .ToList();
        }

        private string GetProcessName(IInteropWindow window)
        {
            if (!_processNames.TryGetValue(window.Handle, out var name))
            {
                name = window.GetProcessName();
                _processNames[window.Handle] = name;
            }
            return name;
        }

        /// <summary>
        /// Where an entry with a place on a monitor goes, in screen coordinates: at its place when that is on the screens, else at the
        /// same place on the monitor with the cursor (a full screen window is at the same place on every monitor), null when neither fits
        /// </summary>
        /// <param name="area">The entry</param>
        /// <param name="screens">The bounds of the monitors</param>
        /// <param name="cursorMonitor">The bounds of the monitor with the cursor</param>
        public static NativeRect? ResolveMonitorPlace(FixedCaptureArea area, IList<NativeRect> screens, NativeRect cursorMonitor)
        {
            if (area.MonitorBounds is not { } bounds || area.Offset is not { } offset)
            {
                return null;
            }
            if (FitsOnScreens(bounds, screens))
            {
                return bounds;
            }
            var moved = new NativeRect(cursorMonitor.X + offset.X, cursorMonitor.Y + offset.Y, area.Size.Width, area.Size.Height);
            return cursorMonitor.Intersect(moved) == moved ? moved : null;
        }

        /// <summary>
        /// Where an entry with a place in the windows of a program goes, in screen coordinates: in each window of the program it fits in,
        /// on the screens, and not completely behind one window above it; the window with the cursor first, then in z-order
        /// </summary>
        /// <param name="area">The entry</param>
        /// <param name="windows">The visible windows, the top one first: their bounds, the bounds of their content, and if they are of the program</param>
        /// <param name="screens">The bounds of the monitors</param>
        /// <param name="cursor">The cursor in screen coordinates</param>
        public static List<NativeRect> ResolveWindowPlaces(FixedCaptureArea area, IList<(NativeRect Bounds, NativeRect Client, bool IsProgram)> windows,
            IList<NativeRect> screens, NativePoint cursor)
        {
            var places = new List<(NativeRect Place, bool UnderCursor)>();
            if (area.Offset is not { } offset)
            {
                return new List<NativeRect>();
            }
            bool cursorTaken = false;
            for (int i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                // The top window under the cursor is the one the cursor is over
                bool underCursor = !cursorTaken && window.Bounds.Contains(cursor);
                cursorTaken |= underCursor;
                if (!window.IsProgram)
                {
                    continue;
                }
                var place = new NativeRect(window.Client.X + offset.X, window.Client.Y + offset.Y, area.Size.Width, area.Size.Height);
                bool covered = windows.Take(i).Any(above => above.Bounds.Intersect(place) == place);
                if (window.Bounds.Intersect(place) == place && FitsOnScreens(place, screens) && !covered)
                {
                    places.Add((place, underCursor));
                }
            }
            return places.OrderBy(entry => entry.UnderCursor ? 0 : 1).Select(entry => entry.Place).ToList();
        }

        /// <summary>
        /// The bounds of the monitors in screen coordinates
        /// </summary>
        private static IEnumerable<NativeRect> GetScreens() => DisplayInfo.AllDisplayInfos.Select(display => display.Bounds);

        /// <summary>
        /// The entry has a place which fits: move the cursor so the area is there, it still follows the cursor
        /// </summary>
        private void MoveCursorToPlace()
        {
            if (!Conf.FixedCaptureMovesCursor || _places.Count == 0)
            {
                return;
            }
            var cursor = GetAnchorPoint(_places[0], Conf.FixedCaptureAnchor).Offset(Host.ScreenBounds.X, Host.ScreenBounds.Y);
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(cursor.X, cursor.Y);
        }

        /// <summary>
        /// The outlines of the places of the current entry
        /// </summary>
        public override void Draw(DrawingContext drawingContext)
        {
            if (_places.Count == 0 || Host.ToolStyle is not { } style)
            {
                return;
            }
            var pen = new Pen(style.Accent, 1) { DashStyle = DashStyles.Dash };
            pen.Freeze();
            foreach (var place in _places)
            {
                drawingContext.DrawRectangle(null, pen, new Rect(place.X + 0.5, place.Y + 0.5, place.Width - 1, place.Height - 1));
            }
        }

        /// <summary>
        /// PageUp and PageDown: the size above or below in the list, which wraps around
        /// </summary>
        private void StepSize(int change)
        {
            if (_sizes.Count > 0)
            {
                PickSize((_index + change + _sizes.Count) % _sizes.Count);
            }
        }

        /// <summary>
        /// C: the cursor holds the area at the next corner, after the corners in the middle
        /// </summary>
        private void NextAnchor()
        {
            int count = Enum.GetValues(typeof(FixedCaptureAnchor)).Length;
            Conf.FixedCaptureAnchor = (FixedCaptureAnchor)(((int)Conf.FixedCaptureAnchor + 1) % count);
            ShowSizes();
            OnMouseMove();
        }

        /// <summary>
        /// Where the cursor is when the area is at the rectangle, the opposite of Place
        /// </summary>
        public static NativePoint GetAnchorPoint(NativeRect rect, FixedCaptureAnchor anchor) => new NativePoint(
            anchor switch
            {
                FixedCaptureAnchor.TopRight or FixedCaptureAnchor.BottomRight => rect.Right - 1,
                FixedCaptureAnchor.Center => rect.Left + rect.Width / 2,
                _ => rect.Left
            },
            anchor switch
            {
                FixedCaptureAnchor.BottomLeft or FixedCaptureAnchor.BottomRight => rect.Bottom - 1,
                FixedCaptureAnchor.Center => rect.Top + rect.Height / 2,
                _ => rect.Top
            });

        /// <summary>
        /// True when the rectangle is completely on the screens: every pixel of it is on one of them (monitors don't overlap)
        /// </summary>
        public static bool FitsOnScreens(NativeRect rect, IEnumerable<NativeRect> screens)
        {
            if (rect.IsEmpty)
            {
                return false;
            }
            long covered = screens.Select(screen => screen.Intersect(rect)).Where(part => !part.IsEmpty).Sum(part => (long)part.Width * part.Height);
            return covered >= (long)rect.Width * rect.Height;
        }

        /// <summary>
        /// True when the rectangle is at most the distance away from the target in each direction
        /// </summary>
        public static bool IsClose(NativeRect rect, NativeRect target, int distance) =>
            Math.Abs(rect.Left - target.Left) <= distance && Math.Abs(rect.Top - target.Top) <= distance;

        /// <summary>
        /// The area of the size with the cursor at the anchor: at a corner the cursor's pixel is the corner pixel of the area,
        /// so the zoomer shows exactly where the area starts or ends
        /// </summary>
        public static NativeRect Place(NativePoint cursor, NativeSize size, FixedCaptureAnchor anchor)
        {
            int left = anchor switch
            {
                FixedCaptureAnchor.TopRight or FixedCaptureAnchor.BottomRight => cursor.X - size.Width + 1,
                FixedCaptureAnchor.Center => cursor.X - size.Width / 2,
                _ => cursor.X
            };
            int top = anchor switch
            {
                FixedCaptureAnchor.BottomLeft or FixedCaptureAnchor.BottomRight => cursor.Y - size.Height + 1,
                FixedCaptureAnchor.Center => cursor.Y - size.Height / 2,
                _ => cursor.Y
            };
            return new NativeRect(left, top, size.Width, size.Height);
        }

        /// <summary>
        /// The window under the cursor, the windows it is in, and the monitor, in capture coordinates
        /// </summary>
        private IEnumerable<NativeRect> GetSnapBounds()
        {
            yield return Host.GetMonitorBounds();
            var screenBounds = Host.ScreenBounds;
            for (var window = Host.FindWindowUnderCursor(true); window != null; window = window.ParentWindow)
            {
                yield return window.GetInfo().Bounds.Offset(-screenBounds.X, -screenBounds.Y);
            }
        }

        /// <summary>
        /// Remember the size and place (screen coordinates) of a region capture, the newest first, for the list of this tool;
        /// as FixedCaptureRemembers says: only the size, the place on its monitor, or the place in the window of the program it is in
        /// </summary>
        /// <param name="bounds">The capture in screen coordinates</param>
        /// <param name="windows">The visible windows, the top one first</param>
        public static void RememberArea(NativeRect bounds, IEnumerable<IInteropWindow> windows)
        {
            if (bounds.IsEmpty)
            {
                return;
            }
            var area = new FixedCaptureArea(bounds.Size);
            if (Conf.FixedCaptureRemembers == FixedCaptureRemember.Window && windows != null)
            {
                // The top window at the middle of the area, when the area is in it
                var center = new NativePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                var window = windows.FirstOrDefault(candidate => candidate.IsVisible() && !candidate.IsMinimized() && candidate.GetInfo().Bounds.Contains(center));
                var info = window?.GetInfo();
                string program = window?.GetProcessName();
                if (info is { } windowInfo && windowInfo.Bounds.Intersect(bounds) == bounds && !string.IsNullOrEmpty(program)
                    && !ShellWindowClasses.Contains(window.GetClassname()))
                {
                    area = new FixedCaptureArea(bounds.Size, new NativePoint(bounds.X - windowInfo.ClientBounds.X, bounds.Y - windowInfo.ClientBounds.Y), program: program + ".exe");
                }
            }
            if (!area.HasPlace && Conf.FixedCaptureRemembers != FixedCaptureRemember.Size)
            {
                // On the monitor of its top left corner
                var monitor = DisplayInfo.GetBounds(bounds.Location);
                area = new FixedCaptureArea(bounds.Size, new NativePoint(bounds.X - monitor.X, bounds.Y - monitor.Y), monitor.Location);
            }
            Conf.RecentCaptureAreas = AddRecent(Conf.RecentCaptureAreas, area);
        }

        /// <summary>
        /// The recent areas with the area in front, without an older copy of it, at most RecentSizesCount
        /// </summary>
        public static List<string> AddRecent(IEnumerable<string> recent, FixedCaptureArea area)
        {
            var areas = (recent ?? Enumerable.Empty<string>())
                .Select(FixedCaptureArea.Parse)
                .Where(recentArea => !recentArea.IsEmpty && recentArea != area)
                .Take(RecentSizesCount - 1)
                .Select(recentArea => recentArea.ToString())
                .ToList();
            areas.Insert(0, area.ToString());
            return areas;
        }

        /// <summary>
        /// The configured entries, then the recent ones which aren't configured (same size and place)
        /// </summary>
        public static List<(FixedCaptureArea Area, bool Recent)> GetSizes(IEnumerable<string> configured, IEnumerable<string> recent)
        {
            var sizes = new List<(FixedCaptureArea Area, bool Recent)>();
            void Add(IEnumerable<string> texts, bool isRecent)
            {
                foreach (var area in (texts ?? Enumerable.Empty<string>()).Select(FixedCaptureArea.Parse))
                {
                    if (!area.IsEmpty && sizes.All(entry => entry.Area != area))
                    {
                        sizes.Add((area, isRecent));
                    }
                }
            }
            Add(configured, false);
            Add(recent, true);
            return sizes;
        }

        /// <summary>
        /// "1280x720" (also with × or spaces) to a size, empty when it isn't one
        /// </summary>
        public static NativeSize ParseSize(string text)
        {
            var parts = text?.Split('x', 'X', '×');
            if (parts?.Length == 2 && int.TryParse(parts[0].Trim(), out int width) && int.TryParse(parts[1].Trim(), out int height) && width > 0 && height > 0)
            {
                return new NativeSize(width, height);
            }
            return NativeSize.Empty;
        }

        /// <summary>
        /// Move the rectangle so the edge closest to an edge of the bounds lies on it, for each direction, when it is within the distance
        /// </summary>
        public static NativeRect Snap(NativeRect rect, IEnumerable<NativeRect> bounds, int distance)
        {
            var boundsList = bounds.ToList();
            int dx = Nearest(boundsList.SelectMany(b => new[] { b.Left, b.Right }), rect.Left, rect.Right, distance);
            int dy = Nearest(boundsList.SelectMany(b => new[] { b.Top, b.Bottom }), rect.Top, rect.Bottom, distance);
            return rect.Offset(dx, dy);
        }

        private static int Nearest(IEnumerable<int> edges, int start, int end, int distance)
        {
            int move = 0;
            int moveDistance = distance + 1;
            foreach (var edge in edges)
            {
                foreach (var candidate in new[] { edge - start, edge - end })
                {
                    if (Math.Abs(candidate) < moveDistance)
                    {
                        move = candidate;
                        moveDistance = Math.Abs(candidate);
                    }
                }
            }
            return move;
        }

        /// <summary>
        /// Move the rectangle into the bounds, when it fits
        /// </summary>
        public static NativeRect KeepInside(NativeRect rect, NativeRect bounds)
        {
            if (rect.Width > bounds.Width || rect.Height > bounds.Height)
            {
                return rect;
            }
            int x = Math.Min(Math.Max(rect.Left, bounds.Left), bounds.Right - rect.Width);
            int y = Math.Min(Math.Max(rect.Top, bounds.Top), bounds.Bottom - rect.Height);
            return new NativeRect(x, y, rect.Width, rect.Height);
        }

        /// <summary>
        /// The panel with the sizes: the configured ones, then the recent ones, the number key in front, the current size highlighted
        /// </summary>
        private void ShowSizes()
        {
            var style = Host.ToolStyle;
            if (style == null)
            {
                return;
            }
            double gap = style.Scale(8);
            var title = style.CreateText(Texts.Core.CaptureFixedSizesTitle, 10, bold: true);
            var recentTitle = style.CreateText(Texts.Core.CaptureFixedSizesRecent, 9, style.MutedForeground);
            var hint = style.CreateText(Texts.Core.CaptureFixedSizesNoSnap, 9, style.MutedForeground);
            var anchor = Conf.FixedCaptureAnchor;
            var anchorText = style.CreateText(GetAnchorText(anchor), 9);
            var anchorKeyCapSize = style.MeasureKeyCap("C");
            double iconWidth = style.Scale(20), iconHeight = style.Scale(14);
            double anchorWidth = anchorKeyCapSize.Width + gap + iconWidth + gap + anchorText.Width;
            double anchorHeight = Math.Max(anchorKeyCapSize.Height, Math.Max(iconHeight, anchorText.Height));
            var keyCapSize = style.MeasureKeyCap("9");
            double rowHeight = keyCapSize.Height + style.Scale(4);
            var texts = _sizes.Select((entry, index) => index == _index
                ? style.CreateText(GetDisplayText(entry.Area), 9, style.Accent, true)
                // A place which doesn't fit now is not used
                : style.CreateText(GetDisplayText(entry.Area), 9, entry.Area.HasPlace && GetPlaces(entry.Area).Count == 0 ? style.MutedForeground : null)).ToList();
            bool hasRecent = _sizes.Any(entry => entry.Recent);
            // While typing a size: what is typed so far, the cursor where the next digit goes
            var typed = IsTyping
                ? style.CreateText(Format(Texts.Core.CaptureFixedSizesTyping, _typingHeight ? $"{_typedWidth} × {_typedHeight}▏" : $"{_typedWidth}▏", 0, 0, null), 9, style.Accent, true)
                : null;
            double typedHeight = typed == null ? 0 : typed.Height + gap;

            double width = new[] { title.Width, hint.Width, anchorWidth, typed?.Width ?? 0, hasRecent ? recentTitle.Width : 0, keyCapSize.Width + gap + texts.Select(text => text.Width).DefaultIfEmpty(0).Max() }.Max();
            double height = title.Height + gap + typedHeight + _sizes.Count * rowHeight + (hasRecent ? recentTitle.Height + gap / 2 : 0) + gap + anchorHeight + gap + hint.Height;

            Host.ShowPanel(this, new Size(width, height), dc =>
            {
                dc.DrawText(title, new Point(0, 0));
                double y = title.Height + gap;
                if (typed != null)
                {
                    dc.DrawText(typed, new Point(0, y));
                    y += typedHeight;
                }
                for (int i = 0; i < _sizes.Count; i++)
                {
                    if (_sizes[i].Recent && (i == 0 || !_sizes[i - 1].Recent))
                    {
                        dc.DrawText(recentTitle, new Point(0, y + gap / 2));
                        y += recentTitle.Height + gap / 2;
                    }
                    if (i < NumberKeysCount)
                    {
                        style.DrawKeyCap(dc, GetNumberKeyText(i), new Point(0, y + (rowHeight - keyCapSize.Height) / 2));
                    }
                    dc.DrawText(texts[i], new Point(keyCapSize.Width + gap, y + (rowHeight - texts[i].Height) / 2));
                    y += rowHeight;
                }
                // C and where the cursor holds the area: a small rectangle with a dot there
                y += gap;
                style.DrawKeyCap(dc, "C", new Point(0, y + (anchorHeight - anchorKeyCapSize.Height) / 2));
                var icon = new Rect(anchorKeyCapSize.Width + gap, y + (anchorHeight - iconHeight) / 2, iconWidth, iconHeight);
                dc.DrawRectangle(null, new Pen(style.MutedForeground, style.Scale(1)), icon);
                var dot = new Point(
                    anchor is FixedCaptureAnchor.TopLeft or FixedCaptureAnchor.BottomLeft ? icon.Left : anchor == FixedCaptureAnchor.Center ? icon.Left + icon.Width / 2 : icon.Right,
                    anchor is FixedCaptureAnchor.TopLeft or FixedCaptureAnchor.TopRight ? icon.Top : anchor == FixedCaptureAnchor.Center ? icon.Top + icon.Height / 2 : icon.Bottom);
                dc.DrawEllipse(style.Accent, null, dot, style.Scale(3), style.Scale(3));
                dc.DrawText(anchorText, new Point(icon.Right + gap, y + (anchorHeight - anchorText.Height) / 2));
                y += anchorHeight;
                dc.DrawText(hint, new Point(0, y + gap));
            });
        }

        /// <summary>
        /// How the settings and the panel show an entry: "1280 × 720", "1280 × 720 at 100, 200", "... on screen 2", "... in notepad.exe"
        /// </summary>
        public static string GetDisplayText(FixedCaptureArea area)
        {
            string size = $"{area.Size.Width} × {area.Size.Height}";
            if (area.Offset is not { } offset)
            {
                return size;
            }
            if (area.Program != null)
            {
                return Format(Texts.Core.CaptureFixedSizesAtProgram, size, offset.X, offset.Y, area.Program);
            }
            if (area.Monitor is { } monitor)
            {
                return Format(Texts.Core.CaptureFixedSizesAtScreen, size, offset.X, offset.Y, GetScreenName(monitor));
            }
            return Format(Texts.Core.CaptureFixedSizesAt, size, offset.X, offset.Y, null);
        }

        private static string Format(string format, string size, int x, int y, string relativeTo)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, size, x, y, relativeTo);
            }
            catch (FormatException)
            {
                // A translation with a wrong placeholder
                return $"{size} @ {x}, {y} {relativeTo}".TrimEnd();
            }
        }

        /// <summary>
        /// The number of the monitor at that top left corner, counting from 1 as ScreenToCapture does, its coordinates when it isn't connected
        /// </summary>
        public static string GetScreenName(NativePoint monitor)
        {
            var displays = DisplayInfo.AllDisplayInfos;
            for (int i = 0; i < displays.Length; i++)
            {
                if (displays[i].Bounds.Location == monitor)
                {
                    return (i + 1).ToString(CultureInfo.CurrentCulture);
                }
            }
            return $"{monitor.X}, {monitor.Y}";
        }

        private static string GetAnchorText(FixedCaptureAnchor anchor) => anchor switch
        {
            FixedCaptureAnchor.TopRight => Texts.Core.CaptureFixedSizesAnchorTopRight,
            FixedCaptureAnchor.BottomRight => Texts.Core.CaptureFixedSizesAnchorBottomRight,
            FixedCaptureAnchor.BottomLeft => Texts.Core.CaptureFixedSizesAnchorBottomLeft,
            FixedCaptureAnchor.Center => Texts.Core.CaptureFixedSizesAnchorCenter,
            _ => Texts.Core.CaptureFixedSizesAnchorTopLeft
        };
    }
}
