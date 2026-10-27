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
    /// An entry can have a place (a configured one, or where a recent capture was): when it fits on the screens, picking the entry moves the
    /// cursor so the area is there, it is drawn as an outline, and the area snaps back onto it when it comes close.
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
        // The place of the current entry in capture coordinates, null when it has none or it doesn't fit on the screens
        private NativeRect? _place;
        private NativeRect _selection = NativeRect.Empty;

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
            _place = GetPlace();
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
            var place = _place;
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
            {
                rect = place is { } placeBounds && IsClose(rect, placeBounds, Scale(PlaceSnapDistance))
                    ? placeBounds
                    : Snap(rect, GetSnapBounds(), Scale(SnapDistance));
            }
            var captureBounds = new NativeRect(0, 0, Host.ScreenBounds.Width, Host.ScreenBounds.Height);
            // At its place the area may be on more than one monitor, it fits on the screens
            _selection = rect == place
                ? rect
                : KeepInside(KeepInside(rect, Host.GetMonitorBounds()), captureBounds).Intersect(captureBounds);
            Host.ShowSelection(_selection);
            Host.ShowLabels(_selection, _selection.Size);
        }

        public override void OnMouseUp() => AcceptSelection();

        private void AcceptSelection()
        {
            if (_selection.IsEmpty)
            {
                return;
            }
            Conf.FixedCaptureArea = _sizes[_index].Area.ToString();
            RememberArea(_selection.Offset(Host.ScreenBounds.X, Host.ScreenBounds.Y));
            Host.Accept(_selection);
        }

        /// <summary>
        /// A number key: the size at that place in the list, when there is one
        /// </summary>
        private void PickSize(int index)
        {
            if (index < _sizes.Count)
            {
                _index = index;
                _place = GetPlace();
                ShowSizes();
                Host.Redraw();
                MoveCursorToPlace();
                OnMouseMove();
            }
        }

        /// <summary>
        /// 1-9 for the first nine entries, 0 for the tenth, as on the keyboard
        /// </summary>
        private static Key GetNumberKey(int index) => index < 9 ? Key.D1 + index : Key.D0;

        private static string GetNumberKeyText(int index) => index < 9 ? (index + 1).ToString() : "0";

        private int Scale(double units) => (int)Math.Round(Host.ToolStyle?.Scale(units) ?? units);

        /// <summary>
        /// The place of the current entry in capture coordinates, null when it has none or it doesn't fit on the screens
        /// </summary>
        private NativeRect? GetPlace()
        {
            if (_sizes.Count == 0 || _sizes[_index].Area.Bounds is not { } bounds)
            {
                return null;
            }
            // On the screens, and in the capture (it covers all screens, but the screens can have changed since it was taken)
            var screenBounds = Host.ScreenBounds;
            if (!FitsOnScreens(bounds, GetScreens()) || screenBounds.Intersect(bounds) != bounds)
            {
                return null;
            }
            return bounds.Offset(-screenBounds.X, -screenBounds.Y);
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
            if (_place is not { } place)
            {
                return;
            }
            var cursor = GetAnchorPoint(place, Conf.FixedCaptureAnchor).Offset(Host.ScreenBounds.X, Host.ScreenBounds.Y);
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(cursor.X, cursor.Y);
        }

        /// <summary>
        /// The outline of the place of the current entry, when it fits on the screens
        /// </summary>
        public override void Draw(DrawingContext drawingContext)
        {
            if (_place is not { } place || Host.ToolStyle is not { } style)
            {
                return;
            }
            var pen = new Pen(style.Accent, 1) { DashStyle = DashStyles.Dash };
            pen.Freeze();
            drawingContext.DrawRectangle(null, pen, new Rect(place.X + 0.5, place.Y + 0.5, place.Width - 1, place.Height - 1));
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
        /// Remember the size and place (screen coordinates) of a region capture, the newest first, for the list of this tool
        /// </summary>
        public static void RememberArea(NativeRect bounds)
        {
            if (bounds.IsEmpty)
            {
                return;
            }
            Conf.RecentCaptureAreas = AddRecent(Conf.RecentCaptureAreas, new FixedCaptureArea(bounds.Size, bounds.Location));
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
                ? style.CreateText(entry.Area.DisplayText, 9, style.Accent, true)
                // A place which doesn't fit on the screens is not used
                : style.CreateText(entry.Area.DisplayText, 9, entry.Area.Bounds is { } bounds && !FitsOnScreens(bounds, GetScreens()) ? style.MutedForeground : null)).ToList();
            bool hasRecent = _sizes.Any(entry => entry.Recent);

            double width = new[] { title.Width, hint.Width, anchorWidth, hasRecent ? recentTitle.Width : 0, keyCapSize.Width + gap + texts.Select(text => text.Width).DefaultIfEmpty(0).Max() }.Max();
            double height = title.Height + gap + _sizes.Count * rowHeight + (hasRecent ? recentTitle.Height + gap / 2 : 0) + gap + anchorHeight + gap + hint.Height;

            Host.ShowPanel(this, new Size(width, height), dc =>
            {
                dc.DrawText(title, new Point(0, 0));
                double y = title.Height + gap;
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
