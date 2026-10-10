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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using log4net;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// The selection follows the (child) window under the cursor, a click or Enter selects it. D shows debug information.
    /// For beta testers it also follows the areas UI Automation knows inside a window, e.g. the parts of a browser page, which has no child windows
    /// for them. PageUp and PageDown select the larger or the smaller area.
    /// A plugin can derive from it for a tool which selects a window.
    /// </summary>
    public class WindowCaptureTool : CaptureTool
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowCaptureTool));
        private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
        // Smaller areas (a word, an icon) are rarely what the user wants to capture
        private const int MinimumAreaSize = 64;
        // The levels below the window, counted without the elements which only wrap another one. The parts of a browser page are
        // several levels below the window, under the panes of the browser and the document
        private const int MaximumAreaDepth = 8;
        // Most windows answer much faster, the busy mark only shows when reading the areas takes longer
        private static readonly TimeSpan BusyMarkDelay = TimeSpan.FromMilliseconds(200);

        // The areas of the windows the cursor was over, null when there are none; the screen doesn't change during the selection
        private readonly Dictionary<IntPtr, UiAutomationArea> _areas = new();
        private CancellationTokenSource _areasCancellation;
        private IntPtr _areasPending;
        private readonly Stopwatch _areasTime = new();
        private DispatcherTimer _busyTimer;
        private UiAutomationArea _deepestArea;
        private UiAutomationArea _selectedArea;
        // 0 is the deepest area under the cursor, every step up the area around it, the last one is the window
        private int _areaLevel;
        private IInteropWindow _selectedWindow;
        private NativeRect _windowSelection = NativeRect.Empty;
        private NativeRect _selection = NativeRect.Empty;
        private bool _showDebugInfo;

        public override CaptureMode Mode => CaptureMode.Window;

        /// <summary>
        /// The window under the cursor, null before the first mouse move
        /// </summary>
        protected IInteropWindow SelectedWindow => _selectedWindow;

        /// <summary>
        /// The visible part of the selected window, in capture coordinates
        /// </summary>
        protected NativeRect WindowSelection => _windowSelection;

        /// <summary>
        /// True when the selection also follows the areas inside a window, a tool which only selects whole windows returns false
        /// </summary>
        protected virtual bool SelectsAreas => CoreConfig.IsBetaTester;

        public override bool ShowsZoomer => false;

        public override void Activate(ICaptureToolHost host)
        {
            base.Activate(host);
            _selectedWindow = null;
            _deepestArea = null;
            _selectedArea = null;
            _areaLevel = 0;
            _windowSelection = NativeRect.Empty;
            _selection = NativeRect.Empty;
            // The selection grows out of the cursor
            Host.ShowSelection(new NativeRect(Host.CursorPosition, NativeSize.Empty));
            Host.ClearLabels();
        }

        public override void Deactivate()
        {
            _areasCancellation?.Cancel();
            _busyTimer?.Stop();
            // The selection shrinks into the cursor
            Host.ShowSelection(new NativeRect(Host.CursorPosition, NativeSize.Empty), true, () =>
            {
                if (Host.ActiveTool != this)
                {
                    Host.HideSelection();
                }
            });
            Host.ClearLabels();
        }

        public override void OnMouseMove()
        {
            var window = Host.FindWindowUnderCursor(true);
            if (window == null)
            {
                return;
            }

            if (!window.Equals(_selectedWindow))
            {
                _selectedWindow = window;
                _areaLevel = 0;
                var title = window.GetCaption();
                Host.Capture.CaptureDetails.Title = title;
                Host.Capture.CaptureDetails.AddMetaData("windowtitle", title);

                // A child window can be partly outside of its parents, GetInfo clips it to them so only the visible part is captured
                _windowSelection = ToCapture(window.GetInfo().Bounds);
                if (SelectsAreas)
                {
                    FindAreasAsync(window).FireAndLog("Find the areas of a window", Log);
                }
            }
            UpdateSelection();
        }

        public override void OnMouseUp() => AcceptWindow();

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterToolKey(this, Key.Return, ModifierKeys.None, () => Texts.Core.CaptureKeyWindowAccept, AcceptWindow);
            host.RegisterToolKey(this, Key.D, ModifierKeys.None, () => Texts.Core.CaptureKeyWindowDetails, () =>
            {
                _showDebugInfo = !_showDebugInfo;
                ShowLabels(false);
            });
            if (SelectsAreas)
            {
                host.RegisterToolKey(this, Key.PageUp, ModifierKeys.None, () => Texts.Core.CaptureKeyWindowLarger, () => ChangeAreaLevel(1));
                host.RegisterToolKey(this, Key.PageDown, ModifierKeys.None, () => Texts.Core.CaptureKeyWindowSmaller, () => ChangeAreaLevel(-1));
            }
        }

        protected virtual void AcceptWindow()
        {
            if (_selectedWindow != null)
            {
                Host.Accept(_selection, _selectedWindow);
            }
        }

        /// <summary>
        /// Read the areas of the window in the background, the lookup for a window the cursor left is cancelled
        /// </summary>
        private async Task FindAreasAsync(IInteropWindow window)
        {
            var handle = window.Handle;
            if (_areas.ContainsKey(handle) || _areasPending == handle)
            {
                return;
            }
            _areasCancellation?.Cancel();
            var cancellation = _areasCancellation = new CancellationTokenSource();
            _areasPending = handle;
            StartBusyMark();
            try
            {
                await FindAreasAsync(handle, cancellation.Token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Not every application supports UI Automation, the window is selected as a whole
                Log.Debug($"Couldn't read the areas of window {handle}", ex);
                _areas[handle] = null;
            }
            finally
            {
                if (_areasCancellation == cancellation)
                {
                    _areasPending = IntPtr.Zero;
                }
            }

            // Back on the UI thread, the cursor may have moved on
            if (Host.ActiveTool == this && window.Equals(_selectedWindow))
            {
                UpdateSelection();
            }
        }

        /// <summary>
        /// True while the areas of the window under the cursor are read
        /// </summary>
        private bool IsReadingAreas => _areasPending != IntPtr.Zero && _areasPending == _selectedWindow?.Handle;

        /// <summary>
        /// True while the busy mark is shown, a derived tool can add its own background work and call <see cref="StartBusyMark"/> when it starts
        /// </summary>
        protected virtual bool IsBusy => IsReadingAreas;

        /// <summary>
        /// Some applications (e.g. a browser after its start) need a moment for their areas, a mark next to the cursor turns until they arrive
        /// </summary>
        protected void StartBusyMark()
        {
            _areasTime.Restart();
            if (_busyTimer == null)
            {
                _busyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
                _busyTimer.Tick += (_, _) =>
                {
                    if (!IsBusy || Host.ActiveTool != this)
                    {
                        _busyTimer.Stop();
                    }
                    Host.Redraw();
                };
            }
            _busyTimer.Start();
        }

        /// <summary>
        /// The busy mark, an arc which turns once a second
        /// </summary>
        public override void Draw(DrawingContext drawingContext)
        {
            if (!IsBusy || _areasTime.Elapsed < BusyMarkDelay)
            {
                return;
            }
            var style = Host.ToolStyle;
            var radius = style.Scale(6);
            var cursor = Host.CursorPosition;
            var center = new Point(cursor.X + style.Scale(20), cursor.Y + style.Scale(20));
            var angle = _areasTime.ElapsedMilliseconds % 1000 * 2 * Math.PI / 1000;
            Point OnCircle(double a) => new(center.X + radius * Math.Cos(a), center.Y + radius * Math.Sin(a));

            var arc = new StreamGeometry();
            using (var context = arc.Open())
            {
                context.BeginFigure(OnCircle(angle), false, false);
                context.ArcTo(OnCircle(angle + 1.5 * Math.PI), new Size(radius, radius), 0, true, SweepDirection.Clockwise, true, false);
            }
            arc.Freeze();
            // The background behind the arc keeps it visible on every content
            drawingContext.DrawEllipse(null, new Pen(style.PanelBackground, style.Scale(4)), center, radius, radius);
            drawingContext.DrawGeometry(null, new Pen(style.Accent, style.Scale(2)), arc);
        }

        /// <summary>
        /// The areas of the window, or of its first parent which has some: a window which only draws (e.g. a "D3D window" of a browser)
        /// has none itself, the content belongs to its parent
        /// </summary>
        private async Task<UiAutomationArea> FindAreasAsync(IntPtr handle, CancellationToken cancellationToken)
        {
            // The windows read on the way up get the same result, their own areas or those of the parent
            var windows = new List<IntPtr>();
            UiAutomationArea result = null;
            for (var window = handle; window != IntPtr.Zero; window = InteropWindowFactory.CreateFor(window).GetParent())
            {
                if (_areas.TryGetValue(window, out result))
                {
                    break;
                }
                var stopwatch = Stopwatch.StartNew();
                var areas = await UiAutomationAreas.FindAreasAsync(window, MaximumAreaDepth, MinimumAreaSize, cancellationToken: cancellationToken);
                Log.Debug($"Areas of window {window} in {stopwatch.ElapsedMilliseconds} ms: {areas}");
                windows.Add(window);
                if (areas?.Children.Count > 0)
                {
                    result = areas;
                    break;
                }
            }
            foreach (var window in windows)
            {
                _areas[window] = result;
            }
            return result;
        }

        /// <summary>
        /// The parts of the window under the cursor in capture coordinates, from the deepest area up to the window itself
        /// </summary>
        private List<(NativeRect Bounds, UiAutomationArea Area)> GetSelectableAreas()
        {
            var selectable = new List<(NativeRect Bounds, UiAutomationArea Area)>();
            UiAutomationArea deepestArea = null;
            if (_areas.TryGetValue(_selectedWindow.Handle, out var root) && root != null)
            {
                var areas = root.GetAreasAt(User32Api.GetCursorLocation());
                // The last one is the window's own element, the window takes its place
                for (int i = 0; i < areas.Count - 1; i++)
                {
                    deepestArea ??= areas[i];
                    var area = ToCapture(areas[i].Bounds).Intersect(_windowSelection);
                    if (!area.IsEmpty && (selectable.Count == 0 || selectable[selectable.Count - 1].Bounds != area))
                    {
                        selectable.Add((area, areas[i]));
                    }
                }
            }
            if (selectable.Count == 0 || selectable[selectable.Count - 1].Bounds != _windowSelection)
            {
                selectable.Add((_windowSelection, null));
            }

            // Another area under the cursor starts at the deepest level again
            if (deepestArea != _deepestArea)
            {
                _deepestArea = deepestArea;
                _areaLevel = 0;
            }
            _areaLevel = Math.Min(_areaLevel, selectable.Count - 1);
            return selectable;
        }

        private void UpdateSelection()
        {
            if (_selectedWindow == null)
            {
                return;
            }

            var (selection, area) = GetSelectableAreas()[_areaLevel];
            if (selection == _selection)
            {
                return;
            }
            _selection = selection;
            _selectedArea = area;
            Host.ShowSelection(_selection, true);
            ShowLabels(true);
        }

        private void ChangeAreaLevel(int change)
        {
            _areaLevel = Math.Max(0, _areaLevel + change);
            UpdateSelection();
        }

        /// <summary>
        /// Screen coordinates to capture coordinates, limited to the capture
        /// </summary>
        private NativeRect ToCapture(NativeRect screenRect)
        {
            var screenBounds = Host.ScreenBounds;
            return screenRect
                .Offset(-screenBounds.X, -screenBounds.Y)
                .Intersect(new NativeRect(0, 0, screenBounds.Width, screenBounds.Height));
        }

        private void ShowLabels(bool fadeIn)
        {
            if (_selection.IsEmpty)
            {
                Host.ClearLabels();
                return;
            }
            string debugText = null;
            if (_showDebugInfo && _selectedWindow != null)
            {
                var caption = _selectedWindow.GetCaption();
                debugText = $"#{_selectedWindow.Handle.ToInt64():X} - {(string.IsNullOrEmpty(caption) ? _selectedWindow.GetProcessName() : caption)}";
                if (_selectedArea != null)
                {
                    // The UI Automation control type id and name of the area
                    debugText += $" - {_selectedArea.ControlType} {_selectedArea.Name}";
                }
            }
            Host.ShowLabels(_selection, _selection.Size, fadeIn, debugText);
        }
    }
}
