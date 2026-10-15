/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.Capturing.Tools
{
    /// <summary>
    /// A rectangle of a chosen size follows the mouse, a click or Enter captures it. Its edges snap to the edges of the windows under the cursor
    /// and of the monitor, with Alt held it is placed freely. The sizes are those of the settings (FixedCaptureSizes), followed by the sizes
    /// of the last region captures (RecentCaptureSizes); a panel lists them, 1-9 pick one, PageUp and PageDown go up and down the list.
    /// </summary>
    public class FixedSizeCaptureTool : CaptureTool
    {
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        // The number of remembered sizes of region captures
        private const int RecentSizesCount = 5;
        // An edge closer than this (device independent units) to the edge of a window snaps to it
        private const double SnapDistance = 8;

        private List<(NativeSize Size, bool Recent)> _sizes = new List<(NativeSize Size, bool Recent)>();
        private int _index;
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
            for (int i = 0; i < 9; i++)
            {
                int index = i;
                host.RegisterToolKey(this, Key.D1 + i, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizePick, () => PickSize(index));
            }
            host.RegisterToolKey(this, Key.PageUp, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizePrevious, () => StepSize(-1));
            host.RegisterToolKey(this, Key.PageDown, ModifierKeys.None, () => Texts.Core.CaptureKeyFixedSizeNext, () => StepSize(1));
        }

        public override void Activate(ICaptureToolHost host)
        {
            base.Activate(host);
            _sizes = GetSizes(Conf.FixedCaptureSizes, Conf.RecentCaptureSizes);
            // The size of the last fixed size capture again
            _index = Math.Max(0, _sizes.FindIndex(entry => entry.Size == Conf.FixedCaptureSize));
            _selection = NativeRect.Empty;
            OnMouseMove();
            // Once the window is shown, when it starts with this tool: then the monitor and its DPI are known
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (Host.ActiveTool == this)
                {
                    ShowSizes();
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
            var size = _sizes[_index].Size;
            var cursor = Host.CursorPosition;
            // The cursor holds the middle of the area
            var rect = new NativeRect(cursor.X - size.Width / 2, cursor.Y - size.Height / 2, size.Width, size.Height);
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
            {
                rect = Snap(rect, GetSnapBounds(), (int)Math.Round(Host.ToolStyle?.Scale(SnapDistance) ?? SnapDistance));
            }
            var captureBounds = new NativeRect(0, 0, Host.ScreenBounds.Width, Host.ScreenBounds.Height);
            _selection = KeepInside(KeepInside(rect, Host.GetMonitorBounds()), captureBounds).Intersect(captureBounds);
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
            Conf.FixedCaptureSize = _sizes[_index].Size;
            RememberSize(_selection.Size);
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
                ShowSizes();
                OnMouseMove();
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
        /// Remember the size of a region capture, the newest first, for the list of this tool
        /// </summary>
        public static void RememberSize(NativeSize size)
        {
            if (size.IsEmpty)
            {
                return;
            }
            var recent = (Conf.RecentCaptureSizes ?? new List<string>())
                .Select(ParseSize)
                .Where(recentSize => !recentSize.IsEmpty && recentSize != size)
                .Take(RecentSizesCount - 1)
                .Select(FormatSize)
                .ToList();
            recent.Insert(0, FormatSize(size));
            Conf.RecentCaptureSizes = recent;
        }

        /// <summary>
        /// The configured sizes, then the recent sizes which aren't configured
        /// </summary>
        public static List<(NativeSize Size, bool Recent)> GetSizes(IEnumerable<string> configured, IEnumerable<string> recent)
        {
            var sizes = new List<(NativeSize Size, bool Recent)>();
            void Add(IEnumerable<string> texts, bool isRecent)
            {
                foreach (var size in (texts ?? Enumerable.Empty<string>()).Select(ParseSize))
                {
                    if (!size.IsEmpty && sizes.All(entry => entry.Size != size))
                    {
                        sizes.Add((size, isRecent));
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
        /// A size as it is stored in greenshot.ini, e.g. "1280x720"
        /// </summary>
        public static string FormatSize(NativeSize size) => $"{size.Width}x{size.Height}";

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
            var keyCapSize = style.MeasureKeyCap("9");
            double rowHeight = keyCapSize.Height + style.Scale(4);
            var texts = _sizes.Select((entry, index) => index == _index
                ? style.CreateText($"{entry.Size.Width} × {entry.Size.Height}", 9, style.Accent, true)
                : style.CreateText($"{entry.Size.Width} × {entry.Size.Height}", 9)).ToList();
            bool hasRecent = _sizes.Any(entry => entry.Recent);

            double width = new[] { title.Width, hint.Width, hasRecent ? recentTitle.Width : 0, keyCapSize.Width + gap + texts.Select(text => text.Width).DefaultIfEmpty(0).Max() }.Max();
            double height = title.Height + gap + _sizes.Count * rowHeight + (hasRecent ? recentTitle.Height + gap / 2 : 0) + gap + hint.Height;

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
                    if (i < 9)
                    {
                        style.DrawKeyCap(dc, (i + 1).ToString(), new Point(0, y + (rowHeight - keyCapSize.Height) / 2));
                    }
                    dc.DrawText(texts[i], new Point(keyCapSize.Width + gap, y + (rowHeight - texts[i].Height) / 2));
                    y += rowHeight;
                }
                dc.DrawText(hint, new Point(0, y + gap));
            });
        }
    }
}
