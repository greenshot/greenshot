/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// The interface to the Surface object, so Plugins can use it.
    /// </summary>
    public interface ISurface : IDisposable
    {
        event SurfaceSizeChangeEventHandler SurfaceSizeChanged;
        event SurfaceMessageEventHandler SurfaceMessage;
        event SurfaceDrawingModeEventHandler DrawingModeChanged;
        event SurfaceElementEventHandler MovingElementChanged;

        /// <summary>
        /// Start value of the step-labels (counts)
        /// </summary>
        int CounterStart { get; set; }

        /// <summary>
        /// Unique ID of the Surface
        /// </summary>
        Guid ID { get; set; }

        IDrawableContainerList Elements { get; }

        /// <summary>
        /// Get/Set the image to the Surface
        /// get will give the image as is currently visible
        /// set will overwrite both the visible image as the underlying image
        ///
        /// important notice:
        /// The setter will clone the passed bitmap and dispose it when the Surface is disposed
        /// This means that the supplied image needs to be disposed by the calling code (if needed!)
        /// </summary>
        Image Image { get; set; }

        /// <summary>
        /// Get the current Image from the Editor for Exporting (save/upload etc)
        /// Don't forget to call image.Dispose() when finished!!!
        /// </summary>
        /// <returns>Bitmap</returns>
        Image GetImageForExport();

        /// <summary>
        /// Add a TextContainer, at the given location, to the Surface.
        /// The TextContainer will be "re"sized to the text size.
        /// </summary>
        /// <param name="text">String to show</param>
        /// <param name="x">Where to put the container, X coordinate in the Image coordinate space</param>
        /// <param name="y">Where to put the container, Y coordinate in the Image coordinate space</param>
        /// <param name="family">FontFamily</param>
        /// <param name="size">Font Size in float</param>
        /// <param name="italic">bool true if italic</param>
        /// <param name="bold">bool true if bold</param>
        /// <param name="shadow">bool true if shadow</param>
        /// <param name="borderSize">size of border (0 for none)</param>
        /// <param name="color">Color of string</param>
        /// <param name="fillColor">Color of background (e.g. Color.Transparent)</param>
        ITextContainer AddTextContainer(string text, int x, int y, FontFamily family, float size, bool italic, bool bold, bool shadow, int borderSize, Color color,
            Color fillColor);

        IImageContainer AddImageContainer(Image image, int x, int y);
        ICursorContainer AddCursorContainer(Cursor cursor, int x, int y);
        IIconContainer AddIconContainer(Icon icon, int x, int y);
        IImageContainer AddImageContainer(string filename, int x, int y);
        ICursorContainer AddCursorContainer(string filename, int x, int y);
        IIconContainer AddIconContainer(string filename, int x, int y);
        long SaveElementsToStream(Stream stream);
        void LoadElementsFromStream(Stream stream);

        bool HasSelectedElements { get; }
        void RemoveSelectedElements();
        void CutSelectedElements();
        void CopySelectedElements();
        void PasteElementFromClipboard();
        void DuplicateSelectedElements();
        void DeselectElement(IDrawableContainer container, bool generateEvents = true);
        void DeselectAllElements();

        /// <summary>
        /// Add an element to the surface
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
        /// <param name="makeUndoable">Should it be placed on the undo stack?</param>
        void AddElements(IDrawableContainerList elements, bool makeUndoable = true);

        void RemoveElements(IDrawableContainerList elements, bool makeUndoable = true);
        void SelectElements(IDrawableContainerList elements);

        /// <summary>
        /// Add an element to the surface
        /// </summary>
        /// <param name="element">IDrawableContainer</param>
        /// <param name="makeUndoable">Should it be placed on the undo stack?</param>
        /// <param name="invalidate">Should it be invalidated (draw)</param>
        void AddElement(IDrawableContainer element, bool makeUndoable = true, bool invalidate = true);

        /// <summary>
        /// Select the supplied container
        /// </summary>
        /// <param name="container">IDrawableContainer</param>
        /// <param name="invalidate">false to skip invalidation</param>
        /// <param name="generateEvents">false to skip event generation</param>
        void SelectElement(IDrawableContainer container, bool invalidate = true, bool generateEvents = true);

        /// <summary>
        /// Is the supplied container "on" the surface?
        /// </summary>
        /// <param name="container"></param>
        /// <returns>This returns false if the container is deleted but still in the undo stack</returns>
        bool IsOnSurface(IDrawableContainer container);

        void Invalidate();

        /// <summary>
        /// Invalidates the specified region of the Surface.
        /// Takes care of the Surface zoom level, accepts rectangle in the coordinate space of the Image.
        /// </summary>
        /// <param name="rectangleToInvalidate">Bounding rectangle for updated elements, in the coordinate space of the Image.</param>
        void InvalidateElements(Rectangle rectangleToInvalidate);

        bool Modified { get; set; }
        string LastSaveFullPath { get; set; }
        string UploadUrl { get; set; }

        /// <summary>
        /// Remove an element of the elements list
        /// </summary>
        /// <param name="elementToRemove">Element to remove</param>
        /// <param name="makeUndoable">flag specifying if the remove needs to be undoable</param>
        /// <param name="invalidate">flag specifying if an surface invalidate needs to be called</param>
        /// <param name="generateEvents">flag specifying if the deselect needs to generate an event</param>
        void RemoveElement(IDrawableContainer elementToRemove, bool makeUndoable = true, bool invalidate = true, bool generateEvents = true);

        void SendMessageEvent(object source, SurfaceMessageTyp messageType, string message);
        void ApplyBitmapEffect(IEffect effect);
        void RemoveCursor();
        bool HasCursor { get; }

        ICaptureDetails CaptureDetails { get; set; }

        /// <summary>
        /// Zoom value applied to the surface.
        /// </summary>
        Fraction ZoomFactor { get; set; }

        /// <summary>
        /// Translate a point from image coorditate space to surface coordinate space.
        /// </summary>
        /// <param name="point">A point in the coordinate space of the image.</param>
        Point ToSurfaceCoordinates(Point point);

        /// <summary>
        /// Translate a rectangle from image coorditate space to surface coordinate space.
        /// </summary>
        /// <param name="rc">A rectangle in the coordinate space of the image.</param>
        Rectangle ToSurfaceCoordinates(Rectangle rc);

        /// <summary>
        /// Translate a point from surface coorditate space to image coordinate space.
        /// </summary>
        /// <param name="point">A point in the coordinate space of the surface.</param>
        Point ToImageCoordinates(Point point);

        /// <summary>
        /// Translate a rectangle from surface coorditate space to image coordinate space.
        /// </summary>
        /// <param name="rc">A rectangle in the coordinate space of the surface.</param>
        Rectangle ToImageCoordinates(Rectangle rc);

        void MakeUndoable(IMemento memento, bool allowMerge);
    }
}