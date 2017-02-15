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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GreenshotPlugin.Interfaces.Drawing.Adorners;

#endregion

namespace GreenshotPlugin.Interfaces.Drawing
{
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

		bool hasFilters { get; }

		EditStatus Status { get; set; }

		EditStatus DefaultEditMode { get; }

		/// <summary>
		///     Available adorners for the DrawableContainer
		/// </summary>
		IList<IAdorner> Adorners { get; }

		void ApplyBounds(RectangleF newBounds);
		void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment);
		void Invalidate();
		bool ClickableAt(int x, int y);
		void MoveBy(int x, int y);
		void Transform(Matrix matrix);
		bool HandleMouseDown(int x, int y);
		void HandleMouseUp(int x, int y);
		bool HandleMouseMove(int x, int y);
		bool InitContent();
		void MakeBoundsChangeUndoable(bool allowMerge);
	}
}