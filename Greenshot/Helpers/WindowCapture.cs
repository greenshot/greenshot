/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Greenshot;
using Greenshot.Configuration;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;

namespace Greenshot.Helpers {
	/// <summary>
	/// This Class is used to pass details about the capture around.
	/// The time the Capture was taken and the Title of the window (or a region of) that is captured
	/// </summary>
	public class CaptureDetails : ICaptureDetails {
		private string title;
		public string Title {
			get {return title;}
			set {title = value;}
		}

		private string filename;
		public string Filename {
			get {return filename;}
			set {filename = value;}
		}

		private DateTime dateTime;
		public DateTime DateTime {
			get {return dateTime;}
			set {dateTime = value;}
		}
		
		private float dpiX;
		public float DpiX {
			get {
				return dpiX;
			}
			set {
				dpiX = value;
			}
		}

		private float dpiY;
		public float DpiY {
			get {
				return dpiY;
			}
			set {
				dpiY = value;
			}
		}
		private Dictionary<string, string> metaData = new Dictionary<string, string>();
		public Dictionary<string, string> MetaData {
			get {return metaData;}
		}
		
		public void AddMetaData(string key, string value) {
			if (metaData.ContainsKey(key)) {
				metaData[key] = value;
			} else {
				metaData.Add(key, value);
			}
		}
		
		private CaptureMode captureMode;
		public CaptureMode CaptureMode {
			get {return captureMode;}
			set {captureMode = value;}
		}
		
		private List<CaptureDestination> captureDestinations = new List<CaptureDestination>();
		public List<CaptureDestination> CaptureDestinations {
			get {return captureDestinations;}
			set {captureDestinations = value;}
		}

		private CaptureHandler captureHandler;
		public CaptureHandler CaptureHandler {
			get {return captureHandler;}
			set {captureHandler = value;}
		}

		public void ClearDestinations() {
			captureDestinations.Clear();
		}		

		public void RemoveDestination(CaptureDestination captureDestination) {
			if (!captureDestinations.Contains(captureDestination)) {
				captureDestinations.Remove(captureDestination);
			}
		}

		public void AddDestination(CaptureDestination captureDestination) {
			if (!captureDestinations.Contains(captureDestination)) {
				captureDestinations.Add(captureDestination);
			}
		}

		public CaptureDetails() {
			dateTime = DateTime.Now;
		}
		
		
	}

	/// <summary>
	/// This class is used to pass an instance of the "Capture" around
	/// Having the Bitmap, eventually the Windows Title and cursor all together.
	/// </summary>
	public class Capture : IDisposable, ICapture {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Capture));
		
		private Rectangle screenBounds;
		/// <summary>
		/// Get/Set the Screenbounds
		/// </summary>
		public Rectangle ScreenBounds {
			get {return screenBounds;}
			set {screenBounds = value;}
		}

		private Image image;
		/// <summary>
		/// Get/Set the Image
		/// </summary>
		public Image Image {
			get {return image;}
			set {
				if (image != null) {
					image.Dispose();
				}
				image = value;
				if (value != null) {
					if (value.PixelFormat.Equals(PixelFormat.Format8bppIndexed) || value.PixelFormat.Equals(PixelFormat.Format1bppIndexed) || value.PixelFormat.Equals(PixelFormat.Format4bppIndexed)) {
						LOG.Debug("Converting Bitmap to PixelFormat.Format32bppArgb as we don't support: " + value.PixelFormat);
						try {
							// Default Bitmap PixelFormat is Format32bppArgb
							this.image = new Bitmap(value);
						} finally {
							// Always dispose, even when a exception occured
							value.Dispose();
						}
					}
					LOG.Debug("Image is set with the following specifications: " + image.Width + "," + image.Height + " - " + image.PixelFormat);
				} else {
					LOG.Debug("Image is removed.");
				}
			}
		}
		
		public void NullImage() {
			image = null;
		}

		private Icon cursor;
		/// <summary>
		/// Get/Set the image for the Cursor
		/// </summary>
		public Icon Cursor {
			get {return cursor;}
			set {
				if (cursor != null) {
					cursor.Dispose();
				}
				cursor = (Icon)value.Clone();
			}
		}
		
		private bool cursorVisible = false;
		/// <summary>
		/// Set if the cursor is visible
		/// </summary>
		public bool CursorVisible {
			get {return cursorVisible;}
			set {cursorVisible = value;}
		}

		private Point cursorLocation = Point.Empty;
		/// <summary>
		/// Get/Set the CursorLocation
		/// </summary>
		public Point CursorLocation {
			get {return cursorLocation;}
			set {cursorLocation = value;}
		}

		private Point location = Point.Empty;
		/// <summary>
		/// Get/set the Location
		/// </summary>
		public Point Location {
			get {return location;}
			set {location = value;}
		}
		
		private CaptureDetails captureDetails;
		/// <summary>
		/// Get/set the CaptureDetails
		/// </summary>
		public ICaptureDetails CaptureDetails {
			get {return captureDetails;}
			set {captureDetails = (CaptureDetails)value;}
		}
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		public Capture() {
			screenBounds = WindowCapture.GetScreenBounds();
			captureDetails = new CaptureDetails();
		}

		/// <summary>
		/// Constructor with Image
		/// Note: the supplied bitmap can be disposed immediately or when constructor is called.
		/// </summary>
		/// <param name="newImage">Image</param>
		public Capture(Image newImage) : this() {
			this.Image = newImage;
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
				if (image != null) {
					image.Dispose();
				}
				if (cursor != null) {
					cursor.Dispose();
				}
			}
			image = null;
			cursor = null;
		}

		/// <summary>
		/// Crops the capture to the specified rectangle (with Bitmap coordinates!)
		/// </summary>
		/// <param name="cropRectangle">Rectangle with bitmap coordinates</param>
		public bool Crop(Rectangle cropRectangle) {
			LOG.Debug("Cropping to: " + cropRectangle.ToString());
			if (ImageHelper.Crop(ref image, ref cropRectangle)) {
				location = cropRectangle.Location;
				// Change mouse location according to the cropRegtangle (including screenbounds) offset
				MoveMouseLocation(-cropRectangle.Location.X, -cropRectangle.Location.Y);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Apply a translate to the mouse location.
		/// e.g. needed for crop
		/// </summary>
		/// <param name="x">x coordinates to move the mouse</param>
		/// <param name="y">y coordinates to move the mouse</param>
		public void MoveMouseLocation(int x, int y) {
			cursorLocation.Offset(x, y);
		}
	}

	/// <summary>
	/// The Window Capture code
	/// </summary>
	public class WindowCapture {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WindowCapture));

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
		/// Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7
		/// <returns>Point with cursor location</returns>
		public static Point GetCursorLocation() {
			if (Environment.OSVersion.Version.Major >= 6) {
				POINT cursorLocation;
				if (User32.GetPhysicalCursorPos(out cursorLocation)) {
					return new Point(cursorLocation.X, cursorLocation.Y);
				} else {
					Win32Error error = Win32.GetLastErrorCode();
					LOG.ErrorFormat("Error retrieving PhysicalCursorPos : {0}", Win32.GetMessage(error));
				}
			}
			return new Point(Cursor.Position.X, Cursor.Position.Y);
		}

		/// <summary>
		/// This method will capture the current Cursor by using User32 Code
		/// </summary>
		/// <returns>A Capture Object with the Mouse Cursor information in it.</returns>
		public static ICapture CaptureCursor(ICapture capture) {
			LOG.Debug("Capturing the mouse cursor.");
			if (capture == null) {
				capture = new Capture();
			}
			int x,y;
			IntPtr hicon;
			CursorInfo cursorInfo = new CursorInfo(); 
			IconInfo iconInfo;
			cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
			if (User32.GetCursorInfo(out cursorInfo)) {
				if (cursorInfo.flags == User32.CURSOR_SHOWING) { 
					hicon = User32.CopyIcon(cursorInfo.hCursor);
					if (User32.GetIconInfo(hicon, out iconInfo)) {
						Point cursorLocation = GetCursorLocation();
						// Allign cursor location to Bitmap coordinates (instead of Screen coordinates)
						x = cursorLocation.X - iconInfo.xHotspot - capture.ScreenBounds.X;
						y = cursorLocation.Y - iconInfo.yHotspot - capture.ScreenBounds.Y;
						// Set the location
						capture.CursorLocation = new Point(x, y);

						using (Icon icon = Icon.FromHandle(hicon)) {
							capture.Cursor = icon;
						}

						if (iconInfo.hbmMask != IntPtr.Zero) {
							GDI32.DeleteObject(iconInfo.hbmMask);
						}
						if (iconInfo.hbmColor != IntPtr.Zero) {
							GDI32.DeleteObject(iconInfo.hbmColor);
						}
					}
					User32.DestroyIcon(hicon);
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
		/// <param name="capture">ICapture</param>
		/// <param name="captureBounds">Rectangle of what we want to capture</param>
		/// <returns></returns>
		private static Exception CreateCaptureException(string method, Rectangle captureBounds) {
			Exception exceptionToThrow = User32.CreateWin32Exception(method);
			if (captureBounds != Rectangle.Empty) {
				exceptionToThrow.Data.Add("Height", captureBounds.Height);
				exceptionToThrow.Data.Add("Width", captureBounds.Width);
			}
			return exceptionToThrow;
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
			capture.Image = CaptureRectangle(captureBounds);
			capture.Location = captureBounds.Location;
			((Bitmap)capture.Image).SetResolution(capture.CaptureDetails.DpiX, capture.CaptureDetails.DpiY);
			if (capture.Image == null) {
				return null;
			}
			return capture;
		}

		/// <summary>
		/// This method will use User32 code to capture the specified captureBounds from the screen
		/// </summary>
		/// <param name="captureBounds">Rectangle with the bounds to capture</param>
		/// <returns>Bitmap which is captured from the screen at the location specified by the captureBounds</returns>
		public static Bitmap CaptureRectangle(Rectangle captureBounds) {
			Bitmap returnBitmap = null;
			if (captureBounds.Height <= 0 || captureBounds.Width <= 0) {
				LOG.Warn("Nothing to capture, ignoring!");
				return null;
			} else {
				LOG.Debug("CaptureRectangle Called!");
			}
			
			// .NET GDI+ Solution, according to some post this has a GDI+ leak...
			// See http://connect.microsoft.com/VisualStudio/feedback/details/344752/gdi-object-leak-when-calling-graphics-copyfromscreen
			// Bitmap capturedBitmap = new Bitmap(captureBounds.Width, captureBounds.Height);
			// using (Graphics graphics = Graphics.FromImage(capturedBitmap)) {
			//	graphics.CopyFromScreen(captureBounds.Location, Point.Empty, captureBounds.Size, CopyPixelOperation.CaptureBlt);
			// }
			// capture.Image = capturedBitmap;
			// capture.Location = captureBounds.Location;
				
			// "P/Invoke" Solution for capturing the screen
			IntPtr hWndDesktop = User32.GetDesktopWindow();
			// get te hDC of the target window
			IntPtr hDCDesktop = User32.GetWindowDC(hWndDesktop);
 
			// Make sure the last error is set to 0
			Win32.SetLastError(0);

			// create a device context we can copy to
			IntPtr hDCDest = GDI32.CreateCompatibleDC(hDCDesktop);
			// Check if the device context is there, if not throw an error with as much info as possible!
			if (hDCDest == IntPtr.Zero) {
				// Get Exception before the error is lost
				Exception exceptionToThrow = CreateCaptureException("CreateCompatibleDC", captureBounds);
				// Cleanup
				User32.ReleaseDC(hWndDesktop, hDCDesktop);
				// throw exception
				throw exceptionToThrow;
			}

			// Create BitmapInfoHeader for CreateDIBSection
			BitmapInfoHeader bmi = new BitmapInfoHeader(captureBounds.Width, captureBounds.Height, 24);

			// Make sure the last error is set to 0
			Win32.SetLastError(0);

			// create a bitmap we can copy it to, using GetDeviceCaps to get the width/height
			IntPtr bits0; // not used for our purposes. It returns a pointer to the raw bits that make up the bitmap.
			IntPtr hDIBSection = GDI32.CreateDIBSection(hDCDesktop, ref bmi, BitmapInfoHeader.DIB_RGB_COLORS, out bits0, IntPtr.Zero, 0);

			if (hDIBSection == IntPtr.Zero) {
				// Get Exception before the error is lost
				Exception exceptionToThrow = CreateCaptureException("CreateDIBSection", captureBounds);
				exceptionToThrow.Data.Add("hdcDest", hDCDest.ToInt32());
				exceptionToThrow.Data.Add("hdcSrc", hDCDesktop.ToInt32());
				
				// clean up
				GDI32.DeleteDC(hDCDest);
				User32.ReleaseDC(hWndDesktop, hDCDesktop);

				// Throw so people can report the problem
				throw exceptionToThrow;
			} else {
				// select the bitmap object and store the old handle
				IntPtr hOldObject = GDI32.SelectObject(hDCDest, hDIBSection);
	
				// bitblt over (make copy)
				GDI32.BitBlt(hDCDest, 0, 0, captureBounds.Width, captureBounds.Height, hDCDesktop, captureBounds.X,  captureBounds.Y, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
				
				// restore selection (old handle)
				GDI32.SelectObject(hDCDest, hOldObject);
				// clean up
				GDI32.DeleteDC(hDCDest);
				User32.ReleaseDC(hWndDesktop, hDCDesktop);

				// get a .NET image object for it
				// A suggestion for the "A generic error occurred in GDI+." E_FAIL/0×80004005 error is to re-try...
				bool success = false;
				ExternalException exception = null;
				for(int i = 0; i < 3; i++) {
					try {
						// assign image to Capture, the image will be disposed there..
						returnBitmap = Bitmap.FromHbitmap(hDIBSection);
						success = true;
						break;
					} catch (ExternalException ee) {
						LOG.Warn("Problem getting bitmap at try " + i + " : ", ee);
						exception = ee;
					}
				}
				if (!success) {
					LOG.Error("Still couldn't create Bitmap!");
					throw exception;
				}
				// free up the Bitmap object
				GDI32.DeleteObject(hDIBSection);
				
			}
            return returnBitmap;
        }
	}
}
