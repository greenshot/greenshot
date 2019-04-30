// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Gfx;

namespace Greenshot.Addons.Interfaces.Drawing
{
	/// <summary>
	/// This is the interface for a list of containers, it pretty much wraps the container
	/// </summary>
	public interface IDrawableContainerList : IList<IDrawableContainer>, IDisposable
	{
		/// <summary>
		/// The ID of the surface
		/// </summary>
		Guid ParentID { get; }

		/// <summary>
		/// Is this list selected
		/// </summary>
		bool Selected { get; set; }

		/// <summary>
		/// The parent surface to which this container list belongs
		/// </summary>
		ISurface Parent { get; set; }

		/// <summary>
		/// Status of the containers
		/// </summary>
		EditStatus Status { get; set; }

        /// <summary>
        /// Make bounds change undoable
        /// </summary>
        /// <param name="allowMerge">bool</param>
        void MakeBoundsChangeUndoable(bool allowMerge);

        /// <summary>
        /// Transform all container in the list
        /// </summary>
        /// <param name="matrix">Matrix</param>
		void Transform(Matrix matrix);

        /// <summary>
        /// Move all containers in the list
        /// </summary>
        /// <param name="dx">int</param>
        /// <param name="dy">int</param>
		void MoveBy(int dx, int dy);

        /// <summary>
        /// Is a container in the list click-able
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns></returns>
		bool ClickableAt(int x, int y);

        /// <summary>
        /// Get the element in the list which is under the coordinates
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>IDrawableContainer</returns>
		IDrawableContainer ClickableElementAt(int x, int y);

        /// <summary>
        /// Handle the double click
        /// </summary>
		void OnDoubleClick();

        /// <summary>
        /// Are there any filters used in the specified rectangle
        /// </summary>
        /// <param name="clipRectangle">NativeRect</param>
        /// <returns>bool</returns>
		bool HasIntersectingFilters(NativeRect clipRectangle);

        /// <summary>
        /// Is there an intersection with the specified rectangle
        /// </summary>
        /// <param name="clipRectangle">NativeRect</param>
        /// <returns>bool</returns>
		bool IntersectsWith(NativeRect clipRectangle);

        /// <summary>
        /// Draw the containers in the list
        /// </summary>
        /// <param name="graphics">Graphics</param>
        /// <param name="bitmap">IBitmapWithNativeSupport</param>
        /// <param name="renderMode">RenderMode</param>
        /// <param name="clipRectangle">NativeRect</param>
		void Draw(Graphics graphics, IBitmapWithNativeSupport bitmap, RenderMode renderMode, NativeRect clipRectangle);

        /// <summary>
        /// Invalidate the bound of all the containers in the list
        /// </summary>
		void Invalidate();

        /// <summary>
        /// Move all the elements in the list to the top
        /// </summary>
        /// <param name="elements"></param>
		void PullElementsToTop(IDrawableContainerList elements);

        /// <summary>
        /// Can elements be pushed down?
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
        /// <returns>bool</returns>
		bool CanPushDown(IDrawableContainerList elements);

        /// <summary>
        /// Move all the elements in the list up
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
		void PullElementsUp(IDrawableContainerList elements);

        /// <summary>
        /// Can elements be pulled up?
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
        /// <returns>bool</returns>
		bool CanPullUp(IDrawableContainerList elements);

        /// <summary>
        /// Move all the elements in the list down
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
		void PushElementsDown(IDrawableContainerList elements);

        /// <summary>
        /// Move all the elements in the list to the bottom
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
		void PushElementsToBottom(IDrawableContainerList elements);

        /// <summary>
        /// Show a context menu for the elements in the list
        /// </summary>
        /// <param name="e">MouseEventArgs</param>
        /// <param name="surface">ISurface</param>
		void ShowContextMenu(MouseEventArgs e, ISurface surface);

        /// <summary>
        /// Send a field changed event to all the containers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		void HandleFieldChangedEvent(object sender, FieldChangedEventArgs e);
	}
}