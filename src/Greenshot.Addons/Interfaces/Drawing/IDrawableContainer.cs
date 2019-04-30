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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Interfaces.Drawing.Adorners;

namespace Greenshot.Addons.Interfaces.Drawing
{
	/// <summary>
	/// This is the interface for the drawable container
	/// </summary>
	public interface IDrawableContainer : INotifyPropertyChanged, IDisposable
	{
		/// <summary>
		/// The surface where this container belongs to
		/// </summary>
		ISurface Parent { get; set; }

		/// <summary>
		/// Is this container selected
		/// </summary>
		bool Selected { get; set; }

		/// <summary>
		/// Left for the bounds
		/// </summary>
		int Left { get; set; }

        /// <summary>
        /// Top for the bounds
        /// </summary>
		int Top { get; set; }

        /// <summary>
        /// Width for the bounds
        /// </summary>
		int Width { get; set; }

        /// <summary>
        /// Height for the bounds
        /// </summary>
		int Height { get; set; }

		/// <summary>
		/// Location for the container
		/// </summary>
		NativePoint Location { get; }

        /// <summary>
        /// Size of the container
        /// </summary>
		Size Size { get; }

		/// <summary>
		/// The bounds for the container
		/// </summary>
		NativeRect Bounds { get; }

		/// <summary>
		/// The drawing bounds for the container
		/// </summary>
		NativeRect DrawingBounds { get; }

		/// <summary>
		/// does this container have filters
		/// </summary>
		bool HasFilters { get; }

		/// <summary>
		/// The current edit status
		/// </summary>
		EditStatus Status { get; set; }

		/// <summary>
		/// Default edit status
		/// </summary>
		EditStatus DefaultEditMode { get; }

		/// <summary>
		///     Available adorners for the DrawableContainer
		/// </summary>
		IList<IAdorner> Adorners { get; }

        /// <summary>
        /// Apply new bounds for the container
        /// </summary>
        /// <param name="newBounds"></param>
		void ApplyBounds(NativeRect newBounds);

        /// <summary>
        /// Align the container to the parent
        /// </summary>
        /// <param name="horizontalAlignment">HorizontalAlignment</param>
        /// <param name="verticalAlignment">VerticalAlignment</param>
		void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment);

        /// <summary>
        /// Invalidate the content under the container
        /// </summary>
		void Invalidate();

        /// <summary>
        /// Is the container under the mouse coordinates
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>bool</returns>
		bool ClickableAt(int x, int y);

        /// <summary>
        /// Move the container
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
		void MoveBy(int x, int y);

        /// <summary>
        /// Transform the container, if possible
        /// </summary>
        /// <param name="matrix">Matrix</param>
		void Transform(Matrix matrix);

        /// <summary>
        /// Handle a mouse down event
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>bool</returns>
		bool HandleMouseDown(int x, int y);

        /// <summary>
        /// Handle a mouse up event
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
		void HandleMouseUp(int x, int y);

        /// <summary>
        /// Handle a mouse move event
        /// </summary>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>bool</returns>
        bool HandleMouseMove(int x, int y);

        /// <summary>
        /// Initialize the content
        /// </summary>
        /// <returns>bool</returns>
		bool InitContent();

        /// <summary>
        /// Make the bound change undoable
        /// </summary>
        /// <param name="allowMerge">bool</param>
		void MakeBoundsChangeUndoable(bool allowMerge);
	}
}