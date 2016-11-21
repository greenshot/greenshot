using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Greenshot.Addon.Editor.Interfaces.Drawing
{
	public interface IDrawableContainer : IFieldHolder, IDisposable
	{
		/// <summary>
		///     Available adorners for the DrawableContainer
		/// </summary>
		IList<IAdorner> Adorners { get; }

		Rectangle Bounds { get; }

		EditStatus DefaultEditMode { get; }

		Rectangle DrawingBounds { get; }

		List<IFilter> Filters { get; }

		bool HasFilters { get; }

		int Height { get; set; }

		int Left { get; set; }

		Point Location { get; }

		ISurface Parent { get; set; }

		bool Selected { get; set; }

		Size Size { get; }

		EditStatus Status { get; set; }

		int Top { get; set; }

		int Width { get; set; }

		void AlignToParent(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment);

		void ApplyBounds(RectangleF newBounds);
		bool ClickableAt(int x, int y);
		bool HandleMouseDown(int x, int y);
		bool HandleMouseMove(int x, int y);
		void HandleMouseUp(int x, int y);
		bool InitContent();
		void MakeBoundsChangeUndoable(bool allowMerge);
		void MoveBy(int x, int y);
		void Transform(Matrix matrix);
	}
}