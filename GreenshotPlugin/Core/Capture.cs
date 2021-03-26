/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Ocr;
using log4net;

namespace GreenshotPlugin.Core
{
    /// <summary>
    /// This class is used to pass an instance of the "Capture" around
    /// Having the Bitmap, eventually the Windows Title and cursor all together.
    /// </summary>
    public class Capture : ICapture {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Capture));

        private Rectangle _screenBounds;
        /// <summary>
        /// Get/Set the screen bounds
        /// </summary>
        public Rectangle ScreenBounds {
            get {
                if (_screenBounds == Rectangle.Empty) {
                    _screenBounds = WindowCapture.GetScreenBounds();
                }
                return _screenBounds;
            }
            set => _screenBounds = value;
        }

        private Image _image;
        /// <summary>
        /// Get/Set the Image
        /// </summary>
        public Image Image {
            get => _image;
            set {
                _image?.Dispose();
                _image = value;
                if (value != null) {
                    if (value.PixelFormat.Equals(PixelFormat.Format8bppIndexed) || value.PixelFormat.Equals(PixelFormat.Format1bppIndexed) || value.PixelFormat.Equals(PixelFormat.Format4bppIndexed)) {
                        Log.Debug("Converting Bitmap to PixelFormat.Format32bppArgb as we don't support: " + value.PixelFormat);
                        try {
                            // Default Bitmap PixelFormat is Format32bppArgb
                            _image = new Bitmap(value);
                        } finally {
                            // Always dispose, even when a exception occured
                            value.Dispose();
                        }
                    }
                    Log.DebugFormat("Image is set with the following specifications: {0} - {1}", _image.Size, _image.PixelFormat);
                } else {
                    Log.Debug("Image is removed.");
                }
            }
        }

        public void NullImage() {
            _image = null;
        }

        private Icon _cursor;
        /// <summary>
        /// Get/Set the image for the Cursor
        /// </summary>
        public Icon Cursor {
            get => _cursor;
            set {
                _cursor?.Dispose();
                _cursor = (Icon)value.Clone();
            }
        }

        /// <summary>
        /// The information which OCR brings
        /// </summary>
        public OcrInformation OcrInformation { get; set; }

        /// <summary>
        /// Set if the cursor is visible
        /// </summary>
        public bool CursorVisible { get; set; }

        private Point _cursorLocation = Point.Empty;
        /// <summary>
        /// Get/Set the CursorLocation
        /// </summary>
        public Point CursorLocation {
            get => _cursorLocation;
            set => _cursorLocation = value;
        }

        private Point _location = Point.Empty;
        /// <summary>
        /// Get/set the Location
        /// </summary>
        public Point Location {
            get => _location;
            set => _location = value;
        }

        private CaptureDetails _captureDetails;
        /// <summary>
        /// Get/set the CaptureDetails
        /// </summary>
        public ICaptureDetails CaptureDetails {
            get => _captureDetails;
            set => _captureDetails = (CaptureDetails)value;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Capture() {
            _screenBounds = WindowCapture.GetScreenBounds();
            _captureDetails = new CaptureDetails();
        }

        /// <summary>
        /// Constructor with Image
        /// Note: the supplied bitmap can be disposed immediately or when constructor is called.
        /// </summary>
        /// <param name="newImage">Image</param>
        public Capture(Image newImage) : this() {
            Image = newImage;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~Capture() {
            Dispose(false);
        }

        /// <summary>
        /// The public accessible Dispose
        /// Will call the GarbageCollector to SuppressFinalize, preventing being cleaned twice
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This Dispose is called from the Dispose and the Destructor.
        /// When disposing==true all non-managed resources should be freed too!
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                _image?.Dispose();
                _cursor?.Dispose();
            }
            _image = null;
            _cursor = null;
        }

        /// <summary>
        /// Crops the capture to the specified rectangle (with Bitmap coordinates!)
        /// </summary>
        /// <param name="cropRectangle">Rectangle with bitmap coordinates</param>
        public bool Crop(Rectangle cropRectangle) {
            Log.Debug("Cropping to: " + cropRectangle);
            if (!ImageHelper.Crop(ref _image, ref cropRectangle))
            {
                return false;
            }
            _location = cropRectangle.Location;
            // Change mouse location according to the cropRectangle (including screenbounds) offset
            MoveMouseLocation(-cropRectangle.Location.X, -cropRectangle.Location.Y);
            // Move all the elements
            // TODO: Enable when the elements are usable again.
            // MoveElements(-cropRectangle.Location.X, -cropRectangle.Location.Y);

            // Offset the OCR information
            // TODO: Remove invisible lines/words?
            CaptureDetails.OcrInformation?.Offset(-cropRectangle.Location.X, -cropRectangle.Location.Y);

            return true;
        }

        /// <summary>
        /// Apply a translate to the mouse location.
        /// e.g. needed for crop
        /// </summary>
        /// <param name="x">x coordinates to move the mouse</param>
        /// <param name="y">y coordinates to move the mouse</param>
        public void MoveMouseLocation(int x, int y) {
            _cursorLocation.Offset(x, y);
        }

        // TODO: Enable when the elements are usable again.
        ///// <summary>
        ///// Apply a translate to the elements
        ///// e.g. needed for crop
        ///// </summary>
        ///// <param name="x">x coordinates to move the elements</param>
        ///// <param name="y">y coordinates to move the elements</param>
        //public void MoveElements(int x, int y) {
        //    MoveElements(elements, x, y);
        //}

        //private void MoveElements(List<ICaptureElement> listOfElements, int x, int y) {
        //    foreach(ICaptureElement childElement in listOfElements) {
        //        Rectangle bounds = childElement.Bounds;
        //        bounds.Offset(x, y);
        //        childElement.Bounds = bounds;
        //        MoveElements(childElement.Children, x, y);
        //    }
        //}

        ///// <summary>
        ///// Add a new element to the capture
        ///// </summary>
        ///// <param name="element">CaptureElement</param>
        //public void AddElement(ICaptureElement element) {
        //    int match = elements.IndexOf(element);
        //    if (match >= 0) {
        //        if (elements[match].Children.Count < element.Children.Count) {
        //            elements.RemoveAt(match);
        //            elements.Add(element);
        //        }
        //    } else {
        //        elements.Add(element);
        //    }
        //}

        ///// <summary>
        ///// Returns a list of rectangles which represent object that are on the capture
        ///// </summary>
        //public List<ICaptureElement> Elements {
        //    get {
        //        return elements;
        //    }
        //    set {
        //        elements = value;
        //    }
        //}

    }
}