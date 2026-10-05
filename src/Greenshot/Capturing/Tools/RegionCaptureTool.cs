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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.Capturing.Tools
{
    /// <summary>
    /// Drag a rectangle with the mouse, or start and end it with Enter
    /// </summary>
    public class RegionCaptureTool : CaptureTool
    {
        /// <summary>
        /// Smaller selections are taken as a click
        /// </summary>
        private const int MinimumSize = 3;

        private NativePoint _start;

        public override CaptureMode Mode => CaptureMode.Region;

        public override bool ShowsCrosshair => !IsSelecting;

        /// <summary>
        /// True while the rectangle is being dragged
        /// </summary>
        protected bool IsSelecting { get; private set; }

        /// <summary>
        /// The rectangle from the start to the cursor, without the pixel under the cursor
        /// </summary>
        protected NativeRect Selection { get; private set; } = NativeRect.Empty;

        public override void Activate(ICaptureToolHost host)
        {
            base.Activate(host);
            IsSelecting = false;
            Selection = NativeRect.Empty;
        }

        public override void Deactivate()
        {
            if (IsSelecting)
            {
                IsSelecting = false;
                Host.HideSelection();
                Host.ClearLabels();
            }
        }

        public override void OnMouseDown() => StartSelection();

        public override void OnMouseUp()
        {
            if (IsSelecting)
            {
                EndSelection();
            }
        }

        /// <summary>
        /// Enter starts and finishes the selection, only while this tool is active
        /// </summary>
        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterToolKey(this, Key.Return, ModifierKeys.None, () => Texts.Core.CaptureKeyRegionSelect, ToggleSelection);
        }

        private void ToggleSelection()
        {
            if (IsSelecting)
            {
                EndSelection();
            }
            else
            {
                StartSelection();
            }
        }

        public override void OnMouseMove()
        {
            if (IsSelecting)
            {
                var cursor = Host.CursorPosition;
                Selection = new NativeRect(cursor.X, cursor.Y, _start.X - cursor.X, _start.Y - cursor.Y).Normalize();
                Host.ShowSelection(Selection);
                // The selection includes the pixel under the cursor
                Host.ShowLabels(Selection, new NativeSize(Selection.Width + 1, Selection.Height + 1));
            }
            else if (Host.IsSelectionVisible && !Host.IsSelectionAnimating)
            {
                // e.g. what is left of the window selection
                Host.HideSelection();
                Host.ClearLabels();
            }
        }

        private void StartSelection()
        {
            _start = Host.CursorPosition;
            IsSelecting = true;
            OnMouseMove();
        }

        private void EndSelection()
        {
            IsSelecting = false;
            if (Selection.Width > MinimumSize && Selection.Height > MinimumSize)
            {
                // The selection includes the pixel under the cursor
                AcceptSelection(new NativeRect(Selection.Left, Selection.Top, Selection.Width + 1, Selection.Height + 1));
            }
            else if (!OnClick())
            {
                OnMouseMove();
            }
        }

        /// <summary>
        /// The mouse was released without selecting a region
        /// </summary>
        /// <returns>true when the click was handled</returns>
        protected virtual bool OnClick() => false;

        /// <summary>
        /// Close the window with the selected rectangle
        /// </summary>
        protected virtual void AcceptSelection(NativeRect rect) => Host.Accept(rect);
    }
}
