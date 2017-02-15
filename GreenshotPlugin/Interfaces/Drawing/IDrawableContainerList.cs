#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

#endregion

namespace GreenshotPlugin.Interfaces.Drawing
{
	public interface IDrawableContainerList : IList<IDrawableContainer>, IDisposable
	{
		Guid ParentID { get; }

		bool Selected { get; set; }

		ISurface Parent { get; set; }

		EditStatus Status { get; set; }

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
	}
}