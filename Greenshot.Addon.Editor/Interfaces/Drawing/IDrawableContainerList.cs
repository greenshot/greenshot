using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Greenshot.Addon.Editor.Interfaces.Drawing
{
	public interface IDrawableContainerList : IList<IDrawableContainer>, IDisposable
	{
		ISurface Parent { get; set; }

		Guid ParentID { get; }

		bool Selected { get; set; }

		EditStatus Status { get; set; }

		bool CanPullUp(IDrawableContainerList elements);
		bool CanPushDown(IDrawableContainerList elements);
		bool ClickableAt(int x, int y);
		IDrawableContainer ClickableElementAt(int x, int y);
		void Draw(Graphics g, Bitmap bitmap, RenderMode renderMode, Rectangle clipRectangle);
		bool HasIntersectingFilters(Rectangle clipRectangle);
		bool IntersectsWith(Rectangle clipRectangle);
		void Invalidate();
		void MakeBoundsChangeUndoable(bool allowMerge);
		void MoveBy(int dx, int dy);
		void OnDoubleClick();
		void PullElementsToTop(IDrawableContainerList elements);
		void PullElementsUp(IDrawableContainerList elements);
		void PushElementsDown(IDrawableContainerList elements);
		void PushElementsToBottom(IDrawableContainerList elements);
		Task ShowContextMenuAsync(MouseEventArgs e, ISurface surface, CancellationToken token = default(CancellationToken));
		void Transform(Matrix matrix);
	}
}