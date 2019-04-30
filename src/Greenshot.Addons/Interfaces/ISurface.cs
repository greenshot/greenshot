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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;
using Greenshot.Gfx.Effects;

namespace Greenshot.Addons.Interfaces
{
	/// <summary>
	///     The interface to the Surface object, so Plugins can use it.
	/// </summary>
	public interface ISurface : IDisposable
	{
		/// <summary>
		///     Start valueof the step-labels (counts)
		/// </summary>
		int CounterStart { get; set; }

		/// <summary>
		///     Unique ID of the Surface
		/// </summary>
		Guid Id { get; set; }

        /// <summary>
        /// All the elements (containers) on the surface
        /// </summary>
        IDrawableContainerList Elements { get; }

		/// <summary>
		///     Get/Set the image to the Surface
		///     get will give the image as is currently visible
		///     set will overwrite both the visible image as the underlying image
		///     important notice:
		///     The setter will clone the passed bitmap and dispose it when the Surface is disposed
		///     This means that the supplied image needs to be disposed by the calling code (if needed!)
		/// </summary>
        IBitmapWithNativeSupport Screenshot { get; set; }

		/// <summary>
		/// Are any elements selected
		/// </summary>
		bool HasSelectedElements { get; }

		/// <summary>
		/// Is the surface modified? False if the surface has been exported or saved.
		/// </summary>
		bool Modified { get; set; }

		/// <summary>
		/// When the surface is saved, this is the location of the last save
		/// </summary>
		string LastSaveFullPath { get; set; }

        /// <summary>
        /// When the surface is uploaded, this is the url for the last upload
        /// </summary>
		string UploadUrl { get; set; }

        /// <summary>
        /// Does the surface have a cursor?
        /// </summary>
		bool HasCursor { get; }

		/// <summary>
		/// The capture details for the capture which is used by the surface
		/// </summary>
		ICaptureDetails CaptureDetails { get; set; }

		/// <summary>
		/// Width of the surface
		/// </summary>
		int Width { get; }

        /// <summary>
        /// Height of the surface
        /// </summary>
		int Height { get; }

        /// <summary>
        /// The size changed event
        /// </summary>
		event SurfaceSizeChangeEventHandler SurfaceSizeChanged;

        /// <summary>
        /// The message event
        /// </summary>
		event SurfaceMessageEventHandler SurfaceMessage;

        /// <summary>
        /// The drawing mode changed event
        /// </summary>
		event SurfaceDrawingModeEventHandler DrawingModeChanged;

        /// <summary>
        /// The moving element changed event
        /// </summary>
		event SurfaceElementEventHandler MovingElementChanged;

        /// <summary>
        ///     Get the current Bitmap from the Editor for Exporting (save/upload etc)
        ///     Don't forget to call image.Dispose() when finished!!!
        /// </summary>
        /// <returns>Bitmap</returns>
        IBitmapWithNativeSupport GetBitmapForExport();

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
		ITextContainer AddTextContainer(string text, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, FontFamily family, float size, bool italic,
			bool bold, bool shadow, int borderSize, Color color, Color fillColor);

        /// <summary>
        /// Add an image container to the surface at the specified location
        /// </summary>
        /// <param name="bitmap">IBitmapWithNativeSupport</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>IBitmapContainer</returns>
        IBitmapContainer AddImageContainer(IBitmapWithNativeSupport bitmap, int x, int y);

        /// <summary>
        /// Add a cursor container to the surface at the specified location
        /// </summary>
        /// <param name="cursor">Cursor</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>ICursorContainer</returns>
        ICursorContainer AddCursorContainer(Cursor cursor, int x, int y);

        /// <summary>
        /// Add an icon container to the surface at the specified location
        /// </summary>
        /// <param name="icon">Icon</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>IIconContainer</returns>
		IIconContainer AddIconContainer(Icon icon, int x, int y);

        /// <summary>
        /// Add an image container to the surface at the specified location
        /// </summary>
        /// <param name="filename">string</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>IBitmapContainer</returns>
		IBitmapContainer AddImageContainer(string filename, int x, int y);

        /// <summary>
        /// Add a cursor container to the surface at the specified location
        /// </summary>
        /// <param name="filename">string</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>ICursorContainer</returns>
        ICursorContainer AddCursorContainer(string filename, int x, int y);

        /// <summary>
        /// Add an icon container to the surface at the specified location
        /// </summary>
        /// <param name="filename">string</param>
        /// <param name="x">int</param>
        /// <param name="y">int</param>
        /// <returns>IIconContainer</returns>
		IIconContainer AddIconContainer(string filename, int x, int y);

        /// <summary>
        /// Save all elements to the specified stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>long with the size</returns>
		long SaveElementsToStream(Stream stream);

        /// <summary>
        /// Load all the elements from the specified stream unto this surface
        /// </summary>
        /// <param name="stream">Stream</param>
		void LoadElementsFromStream(Stream stream);

        /// <summary>
        /// Remove all elements which are selected
        /// </summary>
		void RemoveSelectedElements();

        /// <summary>
        /// Cut all selected elements
        /// </summary>
		void CutSelectedElements();

        /// <summary>
        /// Copy all selected elements onto the clipboard
        /// </summary>
		void CopySelectedElements();

        /// <summary>
        /// Paste all elements from the clipboard unto the surface
        /// </summary>
		void PasteElementFromClipboard();

        /// <summary>
        /// Create a clone from all selected elements
        /// </summary>
		void DuplicateSelectedElements();

        /// <summary>
        /// de-select the specified element
        /// </summary>
        /// <param name="container">IDrawableContainer</param>
        /// <param name="generateEvents">bool</param>
		void DeselectElement(IDrawableContainer container, bool generateEvents = true);

        /// <summary>
        /// de-select all elements
        /// </summary>
		void DeselectAllElements();

		/// <summary>
		///     Add an element to the surface
		/// </summary>
		/// <param name="elements">IDrawableContainerList</param>
		/// <param name="makeUndoable">Should it be placed on the undo stack?</param>
		void AddElements(IDrawableContainerList elements, bool makeUndoable = true);

        /// <summary>
        /// Remove the specified elements
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
        /// <param name="makeUndoable">bool</param>
        void RemoveElements(IDrawableContainerList elements, bool makeUndoable = true);

        /// <summary>
        /// Select the specified elements
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
		void SelectElements(IDrawableContainerList elements);

		/// <summary>
		///     Add an element to the surface
		/// </summary>
		/// <param name="element">IDrawableContainer</param>
		/// <param name="makeUndoable">Should it be placed on the undo stack?</param>
		/// <param name="invalidate">Should it be invalidated (draw)</param>
		void AddElement(IDrawableContainer element, bool makeUndoable = true, bool invalidate = true);

		/// <summary>
		///     Select the supplied container
		/// </summary>
		/// <param name="container">IDrawableContainer</param>
		/// <param name="invalidate">false to skip invalidation</param>
		/// <param name="generateEvents">false to skip event generation</param>
		void SelectElement(IDrawableContainer container, bool invalidate = true, bool generateEvents = true);

		/// <summary>
		///     Is the supplied container "on" the surface?
		/// </summary>
		/// <param name="container"></param>
		/// <returns>This returns false if the container is deleted but still in the undo stack</returns>
		bool IsOnSurface(IDrawableContainer container);

        /// <summary>
        /// Invalidate the surface, used for redrawing, for the specified rectangle
        /// </summary>
        /// <param name="rectangleToInvalidate">Rectangle</param>
        void Invalidate(Rectangle rectangleToInvalidate);

        /// <summary>
        /// Invalidate the surface, used for redrawing.
        /// </summary>
		void Invalidate();

		/// <summary>
		///     Remove an element of the elements list
		/// </summary>
		/// <param name="elementToRemove">Element to remove</param>
		/// <param name="makeUndoable">flag specifying if the remove needs to be undoable</param>
		/// <param name="invalidate">flag specifying if an surface invalidate needs to be called</param>
		/// <param name="generateEvents">flag specifying if the deselect needs to generate an event</param>
		void RemoveElement(IDrawableContainer elementToRemove, bool makeUndoable = true, bool invalidate = true, bool generateEvents = true);

        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="source">object</param>
        /// <param name="messageType">SurfaceMessageTyp</param>
        /// <param name="message">string</param>
        void SendMessageEvent(object source, SurfaceMessageTyp messageType, string message);

		/// <summary>
		/// Apply an effect to the surface
		/// </summary>
		/// <param name="effect"></param>
		void ApplyBitmapEffect(IEffect effect);

		/// <summary>
		/// Remove the cursor from the surface
		/// </summary>
		void RemoveCursor();

        /// <summary>
        /// Make a change to the surface undoable.
        /// </summary>
        /// <param name="memento">IMemento</param>
        /// <param name="allowMerge">bool</param>
        void MakeUndoable(IMemento memento, bool allowMerge);

	    /// <summary>
	    /// Use the supplied capture in the surface
	    /// </summary>
	    /// <param name="capture">ICapture</param>
	    void SetCapture(ICapture capture);

	    /// <summary>
	    /// Use the supplied bitmap in the surface
	    /// </summary>
	    /// <param name="bitmap">IBitmapWithNativeSupport</param>
	    /// <param name="dispose">specify if the current bitmap must be disposed</param>
	    void SetBitmap(IBitmapWithNativeSupport bitmap, bool dispose = false);
    }
}