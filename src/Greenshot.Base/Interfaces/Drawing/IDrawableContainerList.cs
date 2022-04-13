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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Drawing
{
    public interface IDrawableContainerList : IList<IDrawableContainer>, IDisposable
    {
        Guid ParentID { get; }

        bool Selected { get; set; }

        ISurface Parent { get; set; }
        EditStatus Status { get; set; }
        NativeRect DrawingBounds { get; }
        void MakeBoundsChangeUndoable(bool allowMerge);
        void Transform(Matrix matrix);
        void MoveBy(int dx, int dy);
        bool ClickableAt(int x, int y);
        IDrawableContainer ClickableElementAt(int x, int y);
        void OnDoubleClick();
        bool HasIntersectingFilters(NativeRect clipRectangle);
        bool IntersectsWith(NativeRect clipRectangle);
        void Draw(Graphics g, Bitmap bitmap, RenderMode renderMode, NativeRect clipRectangle);
        void SetForegroundColor(Color color);
        void SetBackgroundColor(Color color);
        int IncreaseLineThickness(int increaseBy);
        bool FlipShadow();
        void Invalidate();
        void PullElementsToTop(IDrawableContainerList elements);
        bool CanPushDown(IDrawableContainerList elements);
        void PullElementsUp(IDrawableContainerList elements);
        bool CanPullUp(IDrawableContainerList elements);
        void PushElementsDown(IDrawableContainerList elements);
        void PushElementsToBottom(IDrawableContainerList elements);
        void ShowContextMenu(MouseEventArgs e, ISurface surface);
        void HandleFieldChangedEvent(object sender, FieldChangedEventArgs e);
        void AdjustToDpi(int dpi);
    }
}