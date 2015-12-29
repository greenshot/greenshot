using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces.Drawing;

namespace GreenshotPlugin.Interfaces
{
	/// <summary>
	/// Alignment Enums for possitioning
	/// </summary>
	//public enum HorizontalAlignment {LEFT, CENTER, RIGHT};
	public enum VerticalAlignment
	{
		TOP,
		CENTER,
		BOTTOM
	};

	/// <summary>
	/// The interface to the Surface object, so Plugins can use it.
	/// </summary>
	public interface ISurface : ICapture
	{
		/// <summary>
		/// Unique ID of the Surface
		/// </summary>
		Guid Id
		{
			get;
			set;
		}

		/// <summary>
		/// The background here is the captured image.
		/// This is called from the SurfaceBackgroundChangeMemento.
		/// </summary>
		/// <param name="previous"></param>
		/// <param name="matrix"></param>
		void UndoBackgroundChange(Image previous, Matrix matrix);

		/// <summary>
		/// Add a TextContainer, at the given location, to the Surface.
		/// The TextContainer will be "re"sized to the text size.
		/// </summary>
		/// <param name="text">String to show</param>
		/// <param name="horizontalAlignment">Left, Center, Right</param>
		/// <param name="verticalAlignment">TOP, CENTER, BOTTOM</param>
		/// <param name="family">FontFamily</param>
		/// <param name="size">Font Size in float</param>
		/// <param name="italic">bool true if italic</param>
		/// <param name="bold">bool true if bold</param>
		/// <param name="shadow">bool true if shadow</param>
		/// <param name="borderSize">size of border (0 for none)</param>
		/// <param name="color">Color of string</param>
		/// <param name="fillColor">Color of background (e.g. Color.Transparent)</param>
		ITextContainer AddTextContainer(string text, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, FontFamily family, float size, bool italic, bool bold, bool shadow, int borderSize, Color color, Color fillColor);

		IImageContainer AddImageContainer(Image image, int x, int y);
		ICursorContainer AddCursorContainer(Cursor cursor, int x, int y);
		IIconContainer AddIconContainer(Icon icon, int x, int y);
		IImageContainer AddImageContainer(string filename, int x, int y);
		ICursorContainer AddCursorContainer(string filename, int x, int y);
		IIconContainer AddIconContainer(string filename, int x, int y);

		bool HasSelectedElements
		{
			get;
		}

		void RemoveSelectedElements();
		void CutSelectedElements();
		void CopySelectedElements();
		void PasteElementFromClipboard();
		void DuplicateSelectedElements();
		void DeselectElement(IDrawableContainer container);
		void DeselectAllElements();
		void SelectElement(IDrawableContainer container);

		/// <summary>
		/// Is the supplied container "on" the surface?
		/// </summary>
		/// <param name="container"></param>
		/// <returns>This returns false if the container is deleted but still in the undo stack</returns>
		bool IsOnSurface(IDrawableContainer container);

		void Invalidate(Rectangle rectangleToInvalidate);
		void Invalidate();

		string LastSaveFullPath
		{
			get;
			set;
		}

		Uri UploadUri
		{
			get;
			set;
		}

		void AddElement(IDrawableContainer elementToAdd, bool makeUndoable);
		void RemoveElement(IDrawableContainer elementToRemove, bool makeUndoable);
		Task ApplyBitmapEffectAsync(IEffect effect, CancellationToken token = default(CancellationToken));
		void RemoveCursor();

		bool HasCursor
		{
			get;
		}

		int CounterStart
		{
			get;
			set;
		}
	}
}