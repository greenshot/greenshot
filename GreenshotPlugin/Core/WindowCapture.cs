/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.UnmanagedHelpers;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// This Class is used to pass details about the capture around.
	/// The time the Capture was taken and the Title of the window (or a region of) that is captured
	/// </summary>
	public class CaptureDetails : ICaptureDetails {
		public string Title {
			get;
			set;
		}

		public string Filename {
			get;
			set;
		}

		public DateTime DateTime {
			get;
			set;
		}

		public float DpiX {
			get;
			set;
		}

		public float DpiY {
			get;
			set;
		}

		public Dictionary<string, string> MetaData { get; } = new Dictionary<string, string>();

		public void AddMetaData(string key, string value) {
			if (MetaData.ContainsKey(key)) {
				MetaData[key] = value;
			} else {
				MetaData.Add(key, value);
			}
		}

		public CaptureMode CaptureMode {
			get;
			set;
		}

		public List<IDestination> CaptureDestinations { get; set; } = new List<IDestination>();

		public void ClearDestinations() {
			CaptureDestinations.Clear();
		}		

		public void RemoveDestination(IDestination destination) {
			if (CaptureDestinations.Contains(destination)) {
				CaptureDestinations.Remove(destination);
			}
		}

		public void AddDestination(IDestination captureDestination) {
			if (!CaptureDestinations.Contains(captureDestination)) {
				CaptureDestinations.Add(captureDestination);
			}
		}

		public bool HasDestination(string designation) {
			foreach(IDestination destination in CaptureDestinations) {
				if (designation.Equals(destination.Designation)) {
					return true;
				}
			}
			return false;
		}

		public CaptureDetails() {
			DateTime = DateTime.Now;
		}
	}
	
	/// <summary>
	/// This class is used to pass an instance of the "Capture" around
	/// Having the Bitmap, eventually the Windows Title and cursor all together.
	/// </summary>
	public class Capture : ICapture {
		private static readonly ILog Log = LogManager.GetLogger(typeof(Capture));
		private List<ICaptureElement> _elements = new List<ICaptureElement>();

		private Rectangle _screenBounds;
		/// <summary>
		/// Get/Set the Screenbounds
		/// </summary>
		public Rectangle ScreenBounds {
			get {
				if (_screenBounds == Rectangle.Empty) {
					_screenBounds = WindowCapture.GetScreenBounds();
				}
				return _screenBounds;
			}
			set {_screenBounds = value;}
		}

		private Image _image;
		/// <summary>
		/// Get/Set the Image
		/// </summary>
		public Image Image {
			get {return _image;}
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
			get {return _cursor;}
			set {
				_cursor?.Dispose();
				_cursor = (Icon)value.Clone();
			}
		}

		/// <summary>
		/// Set if the cursor is visible
		/// </summary>
		public bool CursorVisible { get; set; }

		private Point _cursorLocation = Point.Empty;
		/// <summary>
		/// Get/Set the CursorLocation
		/// </summary>
		public Point CursorLocation {
			get {return _cursorLocation;}
			set {_cursorLocation = value;}
		}

		private Point _location = Point.Empty;
		/// <summary>
		/// Get/set the Location
		/// </summary>
		public Point Location {
			get {return _location;}
			set {_location = value;}
		}
		
		private CaptureDetails _captureDetails;
		/// <summary>
		/// Get/set the CaptureDetails
		/// </summary>
		public ICaptureDetails CaptureDetails {
			get {return _captureDetails;}
			set {_captureDetails = (CaptureDetails)value;}
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
			// Change mouse location according to the cropRegtangle (including screenbounds) offset
			MoveMouseLocation(-cropRectangle.Location.X, -cropRectangle.Location.Y);
			// Move all the elements
			// TODO: Enable when the elements are usable again.
			// MoveElements(-cropRectangle.Location.X, -cropRectangle.Location.Y);
				
			// Remove invisible elements
			var newElements = new List<ICaptureElement>();
			foreach(var captureElement in _elements) {
				if (captureElement.Bounds.IntersectsWith(cropRectangle)) {
					newElements.Add(captureElement);
				}
			}
			_elements = newElements;

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
	
	/// <summary>
	/// A class representing an element in the capture
	/// </summary>
	public class CaptureElement : ICaptureElement {
		public CaptureElement(Rectangle bounds) {
			Bounds = bounds;
		}
		public CaptureElement(string name) {
			Name = name;
		}
		public CaptureElement(string name, Rectangle bounds) {
			Name = name;
			Bounds = bounds;
		}

		public List<ICaptureElement> Children { get; set; } = new List<ICaptureElement>();

		public string Name {
			get;
			set;
		}
		public Rectangle Bounds {
			get;
			set;
		}

		// CaptureElements are regarded equal if their bounds are equal. this should be sufficient.
		public override bool Equals(object obj) {
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}
			CaptureElement other = obj as CaptureElement;
			return other != null && Bounds.Equals(other.Bounds);
		}
		
		public override int GetHashCode() {
			// TODO: Fix this, this is not right...
			return Bounds.GetHashCode();
		}
	}
	/// <summary>
	/// The Window Capture code
	/// </summary>
	public class WindowCapture {
		private static readonly ILog Log = LogManager.GetLogger(typeof(WindowCapture));
		private static readonly CoreConfiguration Configuration = IniConfig.GetIniSection<CoreConfiguration>();

		/// <summary>
		/// Used to cleanup the unmanged resource in the iconInfo for the CaptureCursor method
		/// </summary>
		/// <param name="hObject"></param>
		/// <returns></returns>
		[DllImport("gdi32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject(IntPtr hObject);

		private WindowCapture() {
		}

		/// <summary>
		/// Get the bounds of all screens combined.
		/// </summary>
		/// <returns>A Rectangle of the bounds of the entire display area.</returns>
		public static Rectangle GetScreenBounds() {
			int left = 0, top = 0, bottom = 0, right = 0;
			foreach (Screen screen in Screen.AllScreens) {
				left = Math.Min(left, screen.Bounds.X);
				top = Math.Min(top, screen.Bounds.Y);
				int screenAbsRight = screen.Bounds.X + screen.Bounds.Width;
				int screenAbsBottom = screen.Bounds.Y + screen.Bounds.Height;
				right = Math.Max(right, screenAbsRight);
				bottom = Math.Max(bottom, screenAbsBottom);
			}
			return new Rectangle(left, top, (right + Math.Abs(left)), (bottom + Math.Abs(top)));
		}
		
		/// <summary>
		/// Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7. This implementation
		/// can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
		/// </summary>
		/// <returns>
		/// Point with cursor location, relative to the top left corner of the monitor setup (which itself might actually not be on any screen)
		/// </returns>
		public static Point GetCursorLocationRelativeToScreenBounds() {
			return GetLocationRelativeToScreenBounds(User32.GetCursorLocation());
		}
		
		/// <summary>
		/// Converts locationRelativeToScreenOrigin to be relative to top left corner of all screen bounds, which might
		/// be different in multiscreen setups. This implementation
		/// can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
		/// </summary>
		/// <param name="locationRelativeToScreenOrigin"></param>
		/// <returns></returns>
		public static Point GetLocationRelativeToScreenBounds(Point locationRelativeToScreenOrigin) {
			Point ret = locationRelativeToScreenOrigin;
			Rectangle bounds = GetScreenBounds();
			ret.Offset(-bounds.X, -bounds.Y);
			return ret;
		}

		/// <summary>
		/// This method will capture the current Cursor by using User32 Code
		/// </summary>
		/// <returns>A Capture Object with the Mouse Cursor information in it.</returns>
		public static ICapture CaptureCursor(ICapture capture) {
			Log.Debug("Capturing the mouse cursor.");
			if (capture == null) {
				capture = new Capture();
			}
			CursorInfo cursorInfo = new CursorInfo();
			cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
			if (User32.GetCursorInfo(out cursorInfo)) {
				if (cursorInfo.flags == User32.CURSOR_SHOWING) { 
					using (SafeIconHandle safeIcon = User32.CopyIcon(cursorInfo.hCursor))
					{
						IconInfo iconInfo;
						if (User32.GetIconInfo(safeIcon, out iconInfo)) {
							Point cursorLocation = User32.GetCursorLocation();
							// Allign cursor location to Bitmap coordinates (instead of Screen coordinates)
							var x = cursorLocation.X - iconInfo.xHotspot - capture.ScreenBounds.X;
							var y = cursorLocation.Y - iconInfo.yHotspot - capture.ScreenBounds.Y;
							// Set the location
							capture.CursorLocation = new Point(x, y);
	
							using (Icon icon = Icon.FromHandle(safeIcon.DangerousGetHandle())) {
								capture.Cursor = icon;
							}
	
							if (iconInfo.hbmMask != IntPtr.Zero) {
								DeleteObject(iconInfo.hbmMask);
							}
							if (iconInfo.hbmColor != IntPtr.Zero) {
								DeleteObject(iconInfo.hbmColor);
							}
						}
					}
				}
			}
			return capture;
		}

		/// <summary>
		/// This method will call the CaptureRectangle with the screenbounds, therefor Capturing the whole screen.
		/// </summary>
		/// <returns>A Capture Object with the Screen as an Image</returns>
		public static ICapture CaptureScreen(ICapture capture) {
			if (capture == null) {
				capture = new Capture();
			}
			return CaptureRectangle(capture, capture.ScreenBounds);
		}
		
		/// <summary>
		/// Helper method to create an exception that might explain what is wrong while capturing
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <param name="captureBounds">Rectangle of what we want to capture</param>
		/// <returns></returns>
		private static Exception CreateCaptureException(string method, Rectangle captureBounds) {
			Exception exceptionToThrow = User32.CreateWin32Exception(method);
			if (!captureBounds.IsEmpty) {
				exceptionToThrow.Data.Add("Height", captureBounds.Height);
				exceptionToThrow.Data.Add("Width", captureBounds.Width);
			}
			return exceptionToThrow;
		}

		/// <summary>
		/// Helper method to check if it is allowed to capture the process using DWM
		/// </summary>
		/// <param name="process">Process owning the window</param>
		/// <returns>true if it's allowed</returns>
		public static bool IsDwmAllowed(Process process) {
			if (process != null) {
				if (Configuration.NoDWMCaptureForProduct != null && Configuration.NoDWMCaptureForProduct.Count > 0) {
					try {
						string productName = process.MainModule.FileVersionInfo.ProductName;
						if (productName != null && Configuration.NoDWMCaptureForProduct.Contains(productName.ToLower())) {
							return false;
						}
					} catch (Exception ex) {
						Log.Warn(ex.Message);
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Helper method to check if it is allowed to capture the process using GDI
		/// </summary>
		/// <param name="process">Process owning the window</param>
		/// <returns>true if it's allowed</returns>
		public static bool IsGdiAllowed(Process process) {
			if (process != null) {
				if (Configuration.NoGDICaptureForProduct != null && Configuration.NoGDICaptureForProduct.Count > 0) {
					try {
						string productName = process.MainModule.FileVersionInfo.ProductName;
						if (productName != null && Configuration.NoGDICaptureForProduct.Contains(productName.ToLower())) {
							return false;
						}
					} catch (Exception ex) {
						Log.Warn(ex.Message);
					}
				}
			}
			return true;
		}

		/// <summary>
		/// This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <param name="capture">ICapture where the captured Bitmap will be stored</param>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>A Capture Object with a part of the Screen as an Image</returns>
		public static ICapture CaptureRectangle(ICapture capture, Rectangle captureBounds) {
			if (capture == null) {
				capture = new Capture();
			}
			Image capturedImage = null;
			// If the CaptureHandler has a handle use this, otherwise use the CaptureRectangle here
			if (CaptureHandler.CaptureScreenRectangle != null) {
				try {
					capturedImage = CaptureHandler.CaptureScreenRectangle(captureBounds);
				}
				catch
				{
					// ignored
				}
			}
			// If no capture, use the normal screen capture
			if  (capturedImage == null) {
				capturedImage = CaptureRectangle(captureBounds);
			}
			capture.Image = capturedImage;
			capture.Location = captureBounds.Location;
			return capture.Image == null ? null : capture;
		}

		/// <summary>
		/// This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <param name="capture">ICapture where the captured Bitmap will be stored</param>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>A Capture Object with a part of the Screen as an Image</returns>
		public static ICapture CaptureRectangleFromDesktopScreen(ICapture capture, Rectangle captureBounds) {
			if (capture == null) {
				capture = new Capture();
			}
			capture.Image = CaptureRectangle(captureBounds);
			capture.Location = captureBounds.Location;
			return capture.Image == null ? null : capture;
		}

		/// <summary>
		/// This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>Bitmap which is captured from the screen at the location specified by the captureBounds</returns>
		public static Bitmap CaptureRectangle(Rectangle captureBounds) {
			Bitmap returnBitmap = null;
			if (captureBounds.Height <= 0 || captureBounds.Width <= 0) {
				Log.Warn("Nothing to capture, ignoring!");
				return null;
			}
			Log.Debug("CaptureRectangle Called!");

			// .NET GDI+ Solution, according to some post this has a GDI+ leak...
			// See http://connect.microsoft.com/VisualStudio/feedback/details/344752/gdi-object-leak-when-calling-graphics-copyfromscreen
			// Bitmap capturedBitmap = new Bitmap(captureBounds.Width, captureBounds.Height);
			// using (Graphics graphics = Graphics.FromImage(capturedBitmap)) {
			//	graphics.CopyFromScreen(captureBounds.Location, Point.Empty, captureBounds.Size, CopyPixelOperation.CaptureBlt);
			// }
			// capture.Image = capturedBitmap;
			// capture.Location = captureBounds.Location;

			using (var desktopDcHandle = SafeWindowDcHandle.FromDesktop()) {
				if (desktopDcHandle.IsInvalid) {
					// Get Exception before the error is lost
					Exception exceptionToThrow = CreateCaptureException("desktopDCHandle", captureBounds);
					// throw exception
					throw exceptionToThrow;
				}

				// create a device context we can copy to
				using (SafeCompatibleDCHandle safeCompatibleDcHandle = GDI32.CreateCompatibleDC(desktopDcHandle)) {
					// Check if the device context is there, if not throw an error with as much info as possible!
					if (safeCompatibleDcHandle.IsInvalid) {
						// Get Exception before the error is lost
						Exception exceptionToThrow = CreateCaptureException("CreateCompatibleDC", captureBounds);
						// throw exception
						throw exceptionToThrow;
					}
					// Create BITMAPINFOHEADER for CreateDIBSection
					BITMAPINFOHEADER bmi = new BITMAPINFOHEADER(captureBounds.Width, captureBounds.Height, 24);

					// Make sure the last error is set to 0
					Win32.SetLastError(0);

					// create a bitmap we can copy it to, using GetDeviceCaps to get the width/height
					IntPtr bits0; // not used for our purposes. It returns a pointer to the raw bits that make up the bitmap.
					using (SafeDibSectionHandle safeDibSectionHandle = GDI32.CreateDIBSection(desktopDcHandle, ref bmi, BITMAPINFOHEADER.DIB_RGB_COLORS, out bits0, IntPtr.Zero, 0)) {
						if (safeDibSectionHandle.IsInvalid) {
							// Get Exception before the error is lost
							Exception exceptionToThrow = CreateCaptureException("CreateDIBSection", captureBounds);
							exceptionToThrow.Data.Add("hdcDest", safeCompatibleDcHandle.DangerousGetHandle().ToInt32());
							exceptionToThrow.Data.Add("hdcSrc", desktopDcHandle.DangerousGetHandle().ToInt32());

							// Throw so people can report the problem
							throw exceptionToThrow;
						}
						// select the bitmap object and store the old handle
						using (safeCompatibleDcHandle.SelectObject(safeDibSectionHandle)) {
							// bitblt over (make copy)
							// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
							GDI32.BitBlt(safeCompatibleDcHandle, 0, 0, captureBounds.Width, captureBounds.Height, desktopDcHandle, captureBounds.X, captureBounds.Y, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
						}

						// get a .NET image object for it
						// A suggestion for the "A generic error occurred in GDI+." E_FAIL/0×80004005 error is to re-try...
						bool success = false;
						ExternalException exception = null;
						for (int i = 0; i < 3; i++) {
							try {
								// Collect all screens inside this capture
								List<Screen> screensInsideCapture = new List<Screen>();
								foreach (Screen screen in Screen.AllScreens) {
									if (screen.Bounds.IntersectsWith(captureBounds)) {
										screensInsideCapture.Add(screen);
									}
								}
								// Check all all screens are of an equal size
								bool offscreenContent;
								using (Region captureRegion = new Region(captureBounds)) {
									// Exclude every visible part
									foreach (Screen screen in screensInsideCapture) {
										captureRegion.Exclude(screen.Bounds);
									}
									// If the region is not empty, we have "offscreenContent"
									using (Graphics screenGraphics = Graphics.FromHwnd(User32.GetDesktopWindow())) {
										offscreenContent = !captureRegion.IsEmpty(screenGraphics);
									}
								}
								// Check if we need to have a transparent background, needed for offscreen content
								if (offscreenContent) {
									using (Bitmap tmpBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle())) {
										// Create a new bitmap which has a transparent background
										returnBitmap = ImageHelper.CreateEmpty(tmpBitmap.Width, tmpBitmap.Height, PixelFormat.Format32bppArgb, Color.Transparent, tmpBitmap.HorizontalResolution, tmpBitmap.VerticalResolution);
										// Content will be copied here
										using (Graphics graphics = Graphics.FromImage(returnBitmap)) {
											// For all screens copy the content to the new bitmap
											foreach (Screen screen in Screen.AllScreens) {
												Rectangle screenBounds = screen.Bounds;
												// Make sure the bounds are offsetted to the capture bounds
												screenBounds.Offset(-captureBounds.X, -captureBounds.Y);
												graphics.DrawImage(tmpBitmap, screenBounds, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height, GraphicsUnit.Pixel);
											}
										}
									}
								} else {
									// All screens, which are inside the capture, are of equal size
									// assign image to Capture, the image will be disposed there..
									returnBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());
								}
								// We got through the capture without exception
								success = true;
								break;
							} catch (ExternalException ee) {
								Log.Warn("Problem getting bitmap at try " + i + " : ", ee);
								exception = ee;
							}
						}
						if (!success) {
							Log.Error("Still couldn't create Bitmap!");
							if (exception != null) {
								throw exception;
							}
						}
					}
				}
			}
			return returnBitmap;
		}
	}
}
