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
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;
using Greenshot.Capturing.ViewModels;
using Greenshot.Capturing.Views;

namespace Greenshot.Capturing.Overlays
{
    /// <summary>
    /// I shows or hides a panel with the resolution of the screen, the selection, the window under the mouse and the mouse position.
    /// The panel is WPF content bound to a view model: a mouse move only updates the view model.
    /// Whether it is shown is remembered (CaptureInfoVisible), so it comes back with the next capture.
    /// </summary>
    public class InfoOverlay : CaptureOverlay
    {
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        private readonly InfoPanelViewModel _viewModel = new InfoPanelViewModel();
        private InfoPanelView _panel;
        private bool _visible;

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterKey(this, Key.I, ModifierKeys.None, () => Texts.Core.CaptureKeyInfo, Toggle);
            if (Conf.CaptureInfoVisible)
            {
                _visible = true;
                // Once the window is shown: then the monitor and its DPI are known
                _ = Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    if (_visible)
                    {
                        Show();
                    }
                }), DispatcherPriority.ContextIdle);
            }
        }

        public override void OnMouseMove()
        {
            if (_visible)
            {
                Update();
            }
        }

        public override void OnToolChanged() => OnMouseMove();

        private void Toggle()
        {
            _visible = !_visible;
            Conf.CaptureInfoVisible = _visible;
            if (_visible)
            {
                Show();
            }
            else
            {
                Host.HidePanel(this);
            }
        }

        private void Show()
        {
            Update();
            _panel ??= new InfoPanelView { DataContext = _viewModel };
            Host.ShowPanel(this, _panel);
        }

        private void Update()
        {
            var screenBounds = Host.ScreenBounds;
            var monitor = Host.GetMonitorBounds();
            _viewModel.Screen = FormatSize(monitor.Width, monitor.Height);
            _viewModel.AllScreens = FormatSize(screenBounds.Width, screenBounds.Height);
            _viewModel.HasMoreScreens = monitor.Width != screenBounds.Width || monitor.Height != screenBounds.Height;

            // Positions in screen coordinates, as the user knows them; the host works in capture pixels
            var selection = Host.Selection;
            if (selection.IsEmpty)
            {
                _viewModel.Selection = Texts.Core.CaptureInfoNoSelection;
            }
            else
            {
                var size = Host.SelectionSize;
                if (size.IsEmpty)
                {
                    size = new NativeSize(selection.Width, selection.Height);
                }
                _viewModel.Selection = $"{FormatSize(size.Width, size.Height)} @ {FormatPoint(selection.X + screenBounds.X, selection.Y + screenBounds.Y)}";
            }

            _viewModel.Window = Host.FindWindowUnderCursor(false)?.GetCaption() ?? string.Empty;
            var cursor = Host.CursorPosition;
            _viewModel.Mouse = FormatPoint(cursor.X + screenBounds.X, cursor.Y + screenBounds.Y);
        }

        private static string FormatSize(int width, int height) => string.Format(CultureInfo.CurrentCulture, "{0} × {1}", width, height);

        private static string FormatPoint(int x, int y) => string.Format(CultureInfo.CurrentCulture, "{0}, {1}", x, y);
    }
}
