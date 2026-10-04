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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Color = System.Windows.Media.Color;

namespace Greenshot.Base.Interfaces.Capture
{
    /// <summary>
    /// What the CaptureWindow offers its tools: the frozen capture, the cursor, the selection with its labels, and the result.
    /// Everything in coordinates of the capture (0,0 is the top left of the virtual screen), unless noted otherwise.
    /// </summary>
    public interface ICaptureToolHost
    {
        /// <summary>
        /// The capture of the whole screen
        /// </summary>
        ICapture Capture { get; }

        /// <summary>
        /// The bounds of the capture in screen coordinates
        /// </summary>
        NativeRect ScreenBounds { get; }

        /// <summary>
        /// The cursor, corrected for the fix mode (shift keeps it to one direction)
        /// </summary>
        NativePoint CursorPosition { get; }

        /// <summary>
        /// The color of a pixel of the frozen capture, transparent outside of it. Cheap enough to call for every mouse move.
        /// </summary>
        Color GetPixelColor(NativePoint location);

        /// <summary>
        /// The active tool
        /// </summary>
        ICaptureTool ActiveTool { get; }

        /// <summary>
        /// The visible windows, in z-order
        /// </summary>
        IReadOnlyList<WindowDetails> Windows { get; }

        /// <summary>
        /// The window under the mouse cursor
        /// </summary>
        /// <param name="includeChildren">true for the child window under the cursor, false for the top level window</param>
        WindowDetails FindWindowUnderCursor(bool includeChildren);

        /// <summary>
        /// Show the selection rectangle
        /// </summary>
        /// <param name="rect">Where</param>
        /// <param name="animate">true to move it there with the window selection animation (not in a remote session)</param>
        /// <param name="completed">Called when the animation arrived, not when it was replaced by another one</param>
        void ShowSelection(NativeRect rect, bool animate = false, Action completed = null);

        /// <summary>
        /// Hide the selection rectangle
        /// </summary>
        void HideSelection();

        /// <summary>
        /// True when the selection rectangle is shown
        /// </summary>
        bool IsSelectionVisible { get; }

        /// <summary>
        /// True while the selection moves with an animation
        /// </summary>
        bool IsSelectionAnimating { get; }

        /// <summary>
        /// Show the rulers and the size of a selection
        /// </summary>
        /// <param name="rect">The rectangle the rulers are drawn at</param>
        /// <param name="size">The size which is shown</param>
        /// <param name="fadeIn">true to let them appear after a moment, e.g. when the selection arrives at a window</param>
        /// <param name="debugText">Optional text at the top left of the selection</param>
        void ShowLabels(NativeRect rect, NativeSize size, bool fadeIn = false, string debugText = null);

        /// <summary>
        /// Remove the rulers and the size
        /// </summary>
        void ClearLabels();

        /// <summary>
        /// Redraw the layer of the active tool (calls ICaptureTool.Draw)
        /// </summary>
        void Redraw();

        /// <summary>
        /// Redraw the layer of an overlay (calls ICaptureOverlay.Draw)
        /// </summary>
        void Redraw(ICaptureOverlay overlay);

        /// <summary>
        /// A resource of the CaptureWindow, e.g. one of its brushes
        /// </summary>
        object FindResource(object resourceKey);

        /// <summary>
        /// Close the window with this selection
        /// </summary>
        /// <param name="rect">The selected rectangle</param>
        /// <param name="window">The selected window, null to use the top level window under the cursor</param>
        void Accept(NativeRect rect, WindowDetails window = null);

        /// <summary>
        /// Close the window without a selection
        /// </summary>
        void Cancel();
    }
}
