//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Greenshot.Addon.Editor.Interfaces.Drawing;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.Addon.Editor.Interfaces
{
	/// <summary>
	///     The interface to the Surface object, so Plugins can use it.
	/// </summary>
	public interface ISurface : ICapture
	{
		int CounterStart { get; set; }

		IDrawableContainerList Elements { get; }

		bool HasCursor { get; }

		bool HasSelectedElements { get; }

		int Height { get; }

		/// <summary>
		///     Unique ID of the Surface
		/// </summary>
		Guid Id { get; set; }

		string LastSaveFullPath { get; set; }

		Uri UploadUri { get; set; }

		int Width { get; }
		ICursorContainer AddCursorContainer(Cursor cursor, int x, int y);
		ICursorContainer AddCursorContainer(string filename, int x, int y);

		/// <summary>
		///     Add an element to the surface
		/// </summary>
		/// <param name="element">IDrawableContainer</param>
		/// <param name="makeUndoable">Should it be placed on the undo stack?</param>
		/// <param name="invalidate">Should it be invalidated (draw)</param>
		void AddElement(IDrawableContainer element, bool makeUndoable = true, bool invalidate = true);

		/// <summary>
		///     Add an element to the surface
		/// </summary>
		/// <param name="elements">IDrawableContainerList</param>
		/// <param name="makeUndoable">Should it be placed on the undo stack?</param>
		void AddElements(IDrawableContainerList elements, bool makeUndoable = true);

		IIconContainer AddIconContainer(Icon icon, int x, int y);
		IIconContainer AddIconContainer(string filename, int x, int y);

		IImageContainer AddImageContainer(Image image, int x, int y);
		IImageContainer AddImageContainer(string filename, int x, int y);

		/// <summary>
		///     Add a TextContainer, at the given location, to the Surface.
		///     The TextContainer will be "re"sized to the text size.
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

		Task ApplyBitmapEffectAsync(IEffect effect, CancellationToken token = default(CancellationToken));
		void CopySelectedElements();
		void CutSelectedElements();
		void DeselectAllElements();
		void DeselectElement(IDrawableContainer container, bool generateEvents = true);
		void DuplicateSelectedElements();

		void Invalidate(Rectangle rectangleToInvalidate);
		void Invalidate();

		/// <summary>
		///     Is the supplied container "on" the surface?
		/// </summary>
		/// <param name="container"></param>
		/// <returns>This returns false if the container is deleted but still in the undo stack</returns>
		bool IsOnSurface(IDrawableContainer container);

		void MakeUndoable(IMemento memento, bool allowMerge);
		void PasteElementFromClipboard();
		void RemoveCursor();

		/// <summary>
		///     Remove an element of the elements list
		/// </summary>
		/// <param name="elementToRemove">Element to remove</param>
		/// <param name="makeUndoable">flag specifying if the remove needs to be undoable</param>
		/// <param name="invalidate">flag specifying if an surface invalidate needs to be called</param>
		/// <param name="generateEvents">flag specifying if the deselect needs to generate an event</param>
		void RemoveElement(IDrawableContainer elementToRemove, bool makeUndoable = true, bool invalidate = true, bool generateEvents = true);

		void RemoveElements(IDrawableContainerList elements, bool makeUndoable = true);

		void RemoveSelectedElements();

		/// <summary>
		///     Select the supplied container
		/// </summary>
		/// <param name="container">IDrawableContainer</param>
		/// <param name="invalidate">false to skip invalidation</param>
		void SelectElement(IDrawableContainer container, bool invalidate = true, bool generateEvents = true);

		void SelectElements(IDrawableContainerList elements);

		/// <summary>
		///     The background here is the captured image.
		///     This is called from the SurfaceBackgroundChangeMemento.
		/// </summary>
		/// <param name="previous"></param>
		/// <param name="matrix"></param>
		void UndoBackgroundChange(Image previous, Matrix matrix);
	}
}