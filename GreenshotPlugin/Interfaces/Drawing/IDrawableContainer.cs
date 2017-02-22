#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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