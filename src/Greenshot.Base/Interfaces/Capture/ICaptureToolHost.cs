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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
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
        /// The bounds of the monitor under the cursor
        /// </summary>
        NativeRect GetMonitorBounds();

        /// <summary>
        /// Show a panel (e.g. a help text) in a corner of the monitor under the cursor, away from the cursor, the selection,
        /// the zoomer and the other panels. The window draws the panel in Greenshot's style (ToolStyle.DrawPanel) with the content on it,
        /// moves it to another corner (animated) when the cursor, the selection or the zoomer comes close, and keeps the zoomer away from it.
        /// Call it again to change the content; the panel fades in the first time.
        /// </summary>
        /// <param name="owner">The tool or overlay, one panel per owner</param>
        /// <param name="contentSize">The size of the content in pixels, without the padding</param>
        /// <param name="drawContent">Draws the content, 0,0 is the top left of the content (inside the padding)</param>
        void ShowPanel(object owner, Size contentSize, Action<DrawingContext> drawContent);

        /// <summary>
        /// Show a panel with WPF content, e.g. a UserControl or a ContentControl with a DataTemplate and a view model as DataContext,
        /// whose bindings keep it up to date. Placed, moved and styled like the drawn panels; when the content changes its size
        /// (e.g. a longer text) the panel is placed again. The content can use the theme with {DynamicResource} and the keys of
        /// CaptureToolStyle, and plain TextBlocks get Greenshot's font and foreground. Sizes in the content are device independent units,
        /// as everywhere in WPF. Only the panel is laid out, not the window. Panels don't take mouse input.
        /// </summary>
        /// <param name="owner">The tool or overlay, one panel per owner</param>
        /// <param name="content">The content, shown inside the padding of the panel</param>
        void ShowPanel(object owner, FrameworkElement content);

        /// <summary>
        /// Hide the owner's panel, it fades out
        /// </summary>
        void HidePanel(object owner);

        /// <summary>
        /// The look of Greenshot: colors of the current theme, font, and helpers to draw text, panels and key caps
        /// </summary>
        CaptureToolStyle ToolStyle { get; }

        /// <summary>
        /// The active tool
        /// </summary>
        ICaptureTool ActiveTool { get; }

        /// <summary>
        /// Make another tool the active one, e.g. from the key which switches to it
        /// </summary>
        void ActivateTool(ICaptureTool tool);

        /// <summary>
        /// Register a key which is always active, e.g. for an overlay or the key which switches to a tool.
        /// Register in ICaptureTool.Attach or ICaptureOverlay.Attach; the keys stay registered as long as the window is open.
        /// The window registers its own keys first, then the built-in tools theirs, then the tools and overlays of plugins.
        /// </summary>
        /// <param name="owner">The tool or overlay which registers the key</param>
        /// <param name="key">The key</param>
        /// <param name="modifiers">Ctrl, Alt, Shift or Windows which have to be held with it, ModifierKeys.None for the key alone</param>
        /// <param name="description">What the key does, called every time it is shown (e.g. by the help overlay), so it can return the text in the current language</param>
        /// <param name="execute">What happens when the key is pressed</param>
        /// <returns>The binding, also in KeyBindings</returns>
        /// <exception cref="CaptureKeyConflictException">The key is already used</exception>
        /// <exception cref="System.ArgumentException">Ctrl, Alt or Windows alone</exception>
        CaptureKeyBinding RegisterKey(object owner, Key key, ModifierKeys modifiers, Func<string> description, Action execute);

        /// <summary>
        /// Register a key which is only active while the tool is active, e.g. Enter to finish a selection.
        /// Different tools can use the same key; it conflicts with the keys which are always active and with the other keys of the tool.
        /// </summary>
        /// <exception cref="CaptureKeyConflictException">The key is already used</exception>
        CaptureKeyBinding RegisterToolKey(ICaptureTool tool, Key key, ModifierKeys modifiers, Func<string> description, Action execute);

        /// <summary>
        /// All registered keys in the order of registration, of the window, every tool and every overlay.
        /// CaptureKeyBinding.IsActiveFor(ActiveTool) tells which of them work now.
        /// </summary>
        IReadOnlyList<CaptureKeyBinding> KeyBindings { get; }

        /// <summary>
        /// The visible windows, in z-order
        /// </summary>
        IReadOnlyList<IInteropWindow> Windows { get; }

        /// <summary>
        /// The window under the mouse cursor
        /// </summary>
        /// <param name="includeChildren">true for the child window under the cursor, false for the top level window</param>
        IInteropWindow FindWindowUnderCursor(bool includeChildren);

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
        /// Where the selection rectangle is (the end of an animation), empty when it is not shown
        /// </summary>
        NativeRect Selection { get; }

        /// <summary>
        /// The size which the labels show, empty without labels. It can differ from Selection: the region tool includes the pixel under the cursor.
        /// </summary>
        NativeSize SelectionSize { get; }

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
        void Accept(NativeRect rect, IInteropWindow window = null);

        /// <summary>
        /// Close the window without a selection
        /// </summary>
        void Cancel();
    }
}
