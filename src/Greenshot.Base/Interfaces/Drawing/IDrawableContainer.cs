/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing.Adorners;

namespace Greenshot.Base.Interfaces.Drawing
{
    public interface IDrawableContainer : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// The parent surface where this IDrawableContainer is on
        /// </summary>
        ISurface Parent { get; set; }

        /// <summary>
        /// Is this IDrawableContainer selected on the surface
        /// </summary>
        bool Selected { get; set; }

        int Left { get; set; }

        int Top { get; set; }

        int Width { get; set; }

        int Height { get; set; }

        NativePoint Location { get; }

        NativeSize Size { get; }

        NativeRect Bounds { get; }

        NativeRect DrawingBounds { get; }

        void ApplyBounds(NativeRectFloat newBounds);

        bool HasFilters { get; }

        EditStatus Status { get; set; }

        void Invalidate();

        bool ClickableAt(int x, int y);

        void MoveBy(int x, int y);

        void Transform(Matrix matrix);

        bool HandleMouseDown(int x, int y);

        void HandleMouseUp(int x, int y);

        bool HandleMouseMove(int x, int y);

        bool InitContent();

        /// <summary>
        /// Defines if the drawable container participates in undo / redo
        /// </summary>
        bool IsUndoable { get; }

        void MakeBoundsChangeUndoable(bool allowMerge);

        EditStatus DefaultEditMode { get; }

        /// <summary>
        /// Available adorners for the DrawableContainer
        /// </summary>
        IList<IAdorner> Adorners { get; }

        /// <summary>
        /// Is confirm/cancel possible for this container
        /// </summary>
        bool IsConfirmable { get; }

        /// <summary>
        /// Adjust UI elements to the supplied DPI settings
        /// </summary>
        /// <param name="dpi">int</param>
        void AdjustToDpi(int dpi);

        /// <summary>
        /// Enable a way for elements to add a context menu entry
        /// </summary>
        /// <param name="menu">ContextMenuStrip</param>
        /// <param name="surface">ISurface</param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        void AddContextMenuItems(ContextMenuStrip menu, ISurface surface, MouseEventArgs mouseEventArgs);
    }
}