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

using System.Windows.Input;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.Capturing.Tools
{
    /// <summary>
    /// The selection follows the (child) window under the cursor, a click or Enter selects it. D shows debug information.
    /// </summary>
    public class WindowCaptureTool : CaptureTool
    {
        private WindowDetails _selectedWindow;
        private NativeRect _selection = NativeRect.Empty;
        private bool _showDebugInfo;

        public override CaptureMode Mode => CaptureMode.Window;

        public override bool ShowsZoomer => false;

        public override void Activate(ICaptureToolHost host)
        {
            base.Activate(host);
            _selectedWindow = null;
            _selection = NativeRect.Empty;
            // The selection grows out of the cursor
            Host.ShowSelection(new NativeRect(Host.CursorPosition, NativeSize.Empty));
            Host.ClearLabels();
        }

        public override void Deactivate()
        {
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
            if (window == null || window.Equals(_selectedWindow))
            {
                return;
            }

            _selectedWindow = window;
            Host.Capture.CaptureDetails.Title = window.Text;
            Host.Capture.CaptureDetails.AddMetaData("windowtitle", window.Text);

            var screenBounds = Host.ScreenBounds;
            _selection = GetClippedWindowRectangle(window)
                .Offset(-screenBounds.X, -screenBounds.Y)
                .Intersect(new NativeRect(0, 0, screenBounds.Width, screenBounds.Height));
            Host.ShowSelection(_selection, true);
            ShowLabels(true);
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
        }

        private void AcceptWindow()
        {
            if (_selectedWindow != null)
            {
                Host.Accept(_selection, _selectedWindow);
            }
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
                debugText = $"#{_selectedWindow.Handle.ToInt64():X} - {(_selectedWindow.Text.Length > 0 ? _selectedWindow.Text : _selectedWindow.Process?.ProcessName)}";
            }
            Host.ShowLabels(_selection, _selection.Size, fadeIn, debugText);
        }

        /// <summary>
        /// A child window can be partly outside of its parents, only the visible part is captured
        /// </summary>
        private static NativeRect GetClippedWindowRectangle(WindowDetails window)
        {
            var rect = window.WindowRectangle;
            var parent = window.GetParent();
            while (parent != null)
            {
                rect = rect.Intersect(parent.WindowRectangle);
                parent = parent.GetParent();
            }
            return rect;
        }
    }
}
