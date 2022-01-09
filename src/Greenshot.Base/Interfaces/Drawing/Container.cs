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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Drawing.Adorners;

namespace Greenshot.Base.Interfaces.Drawing
{
    public enum RenderMode
    {
        EDIT,
        EXPORT
    };

    public enum EditStatus
    {
        UNDRAWN,
        DRAWING,
        MOVING,
        RESIZING,
        IDLE
    };

    public interface IDrawableContainer : INotifyPropertyChanged, IDisposable
    {
        ISurface Parent { get; set; }
        bool Selected { get; set; }

        int Left { get; set; }

        int Top { get; set; }

        int Width { get; set; }

        int Height { get; set; }

        Point Location { get; }

        Size Size { get; }

        Rectangle Bounds { get; }

        Rectangle DrawingBounds { get; }

        void ApplyBounds(RectangleF newBounds);

        bool hasFilters { get; }

        EditStatus Status { get; set; }
        void Invalidate();
        bool ClickableAt(int x, int y);
        void MoveBy(int x, int y);
        void Transform(Matrix matrix);
        bool HandleMouseDown(int x, int y);
        void HandleMouseUp(int x, int y);
        bool HandleMouseMove(int x, int y);
        bool InitContent();
        void MakeBoundsChangeUndoable(bool allowMerge);
        EditStatus DefaultEditMode { get; }

        /// <summary>
        /// Available adorners for the DrawableContainer
        /// </summary>
        IList<IAdorner> Adorners { get; }

        /// <summary>
        /// Adjust UI elements to the supplied DPI settings
        /// </summary>
        /// <param name="dpi">uint</param>
        void AdjustToDpi(uint dpi);
    }

    public interface IDrawableContainerList : IList<IDrawableContainer>, IDisposable
    {
        Guid ParentID { get; }

        bool Selected { get; set; }

        ISurface Parent { get; set; }
        EditStatus Status { get; set; }
        Rectangle DrawingBounds { get; }
        void MakeBoundsChangeUndoable(bool allowMerge);
        void Transform(Matrix matrix);
        void MoveBy(int dx, int dy);
        bool ClickableAt(int x, int y);
        IDrawableContainer ClickableElementAt(int x, int y);
        void OnDoubleClick();
        bool HasIntersectingFilters(Rectangle clipRectangle);
        bool IntersectsWith(Rectangle clipRectangle);
        void Draw(Graphics g, Bitmap bitmap, RenderMode renderMode, Rectangle clipRectangle);
        void Invalidate();
        void PullElementsToTop(IDrawableContainerList elements);
        bool CanPushDown(IDrawableContainerList elements);
        void PullElementsUp(IDrawableContainerList elements);
        bool CanPullUp(IDrawableContainerList elements);
        void PushElementsDown(IDrawableContainerList elements);
        void PushElementsToBottom(IDrawableContainerList elements);
        void ShowContextMenu(MouseEventArgs e, ISurface surface);
        void HandleFieldChangedEvent(object sender, FieldChangedEventArgs e);
        void AdjustToDpi(uint dpi);
    }

    public interface ITextContainer : IDrawableContainer
    {
        string Text { get; set; }
        void FitToText();
    }

    public interface IImageContainer : IDrawableContainer
    {
        Image Image { get; set; }
        void Load(string filename);
    }

    public interface IEmojiContainer : IDrawableContainer
    {
        string Emoji { get; set; }
    }

    public interface ICursorContainer : IDrawableContainer
    {
        Cursor Cursor { get; set; }
        void Load(string filename);
    }

    public interface IIconContainer : IDrawableContainer
    {
        Icon Icon { get; set; }
        void Load(string filename);
    }
}