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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Configuration;
using Greenshot.Drawing;
using Greenshot.Plugin;
using Greenshot.UnmanagedHelpers;
using GreenshotPlugin.Core;

/// <summary>
/// Code for handling with "windows"
/// Main code is taken from vbAccelerator, location:
/// http://www.vbaccelerator.com/home/NET/Code/Libraries/Windows/Enumerating_Windows/article.asp
/// but a lot of changes/enhancements were made to adapt it for Greenshot.
/// </summary>
namespace Greenshot.Helpers  {
	#region EnumWindows
	/// <summary>
	/// EnumWindows wrapper for .NET
	/// </summary>
	public class WindowsEnumerator {
		#region Member Variables
		private List<WindowDetails> items = null;
		#endregion

		/// <summary>
		/// Returns the collection of windows returned by
		/// GetWindows
		/// </summary>
		public List<WindowDetails> Items {
			get {
				return this.items;
			}
		}

		/// <summary>
		/// Gets all top level windows on the system.
		/// </summary>
		public void GetWindows() {
			this.items = new List<WindowDetails>();
			User32.EnumWindows(new EnumWindowsProc(this.WindowEnum), 0);
		}

		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="hWndParent">Window Handle to get children for</param>
		public void GetWindows(WindowDetails parent) {
			if (parent != null) {
				GetWindows(parent.Handle);
			} else {
				GetWindows(IntPtr.Zero);
			}
		}

		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="hWndParent">Window Handle to get children for</param>
		public void GetWindows(IntPtr hWndParent) {
			this.items = new List<WindowDetails>();
			User32.EnumChildWindows(hWndParent, new EnumWindowsProc(this.WindowEnum), 0);
			if (!IntPtr.Zero.Equals(hWndParent)) {
				WindowDetails parent = new WindowDetails(hWndParent);
				foreach(WindowDetails window in items) {
					window.Text = parent.Text;
				}
			}
		}

		#region EnumWindows callback
		/// <summary>
		/// The enum Windows callback.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <param name="lParam">Application defined value</param>
		/// <returns>1 to continue enumeration, 0 to stop</returns>
		private int WindowEnum(IntPtr hWnd, int lParam) {
			if (this.OnWindowEnum(hWnd)) {
				return 1;
			} else {
				return 0;
			}
		}
		#endregion

		/// <summary>
		/// Called whenever a new window is about to be added
		/// by the Window enumeration called from GetWindows.
		/// If overriding this function, return true to continue
		/// enumeration or false to stop.  If you do not call
		/// the base implementation the Items collection will
		/// be empty.
		/// </summary>
		/// <param name="hWnd">Window handle to add</param>
		/// <returns>True to continue enumeration, False to stop</returns>
		protected virtual bool OnWindowEnum(IntPtr hWnd) {
			items.Add( new WindowDetails(hWnd));
			return true;
		}

		#region Constructor, Dispose
		public WindowsEnumerator() {
			// nothing to do
		}
		#endregion
	}	
	#endregion EnumWindows

	/// <summary>
	#region WindowDetails
	/// <summary>
	/// Provides details about a Window returned by the 
	/// enumeration
	/// </summary>
	public class WindowDetails : IEquatable<WindowDetails>{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WindowDetails));
		private static Dictionary<string, List<string>> classnameTree = new Dictionary<string, List<string>>();
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static ILanguage lang = Language.GetInstance();

		private List<WindowDetails> childWindows = null;
		private WindowDetails parent = null;

		/// <summary>
		/// The window handle.
		/// </summary>
		private IntPtr hWnd = IntPtr.Zero;

		/// <summary>
		/// To allow items to be compared, the hash code
		/// is set to the Window handle, so two EnumWindowsItem
		/// objects for the same Window will be equal.
		/// </summary>
		/// <returns>The Window Handle for this window</returns>
		public override int GetHashCode() {
			return Handle.ToInt32();
		}
		
		public override bool Equals(object right) {
			return this.Equals(right as WindowDetails);
		}

		public bool Equals(WindowDetails other) {
			if (Object.ReferenceEquals(other, null)) {
				return false;
			}
			
			if (Object.ReferenceEquals(this, other)) {
				return true;
			}
			
			if (this.GetType() != other.GetType()){
				return false;
			}
			return other.Handle == Handle;
		}

		public bool HasChildren {
			get {
				return (childWindows != null) && (childWindows.Count > 0);
			}
		}

		public List<WindowDetails> Children {
			get {
				if (childWindows == null) {
					 GetChildren();
				}
				return childWindows;
			}
		}

		/// <summary>
		/// Retrieve all windows with a certain title or classname
		/// </summary>
		/// <param name="titlePattern">The regexp to look for in the title</param>
		/// <param name="classnamePattern">The regexp to look for in the classname</param>
		/// <returns>List<WindowDetails> with all the found windows</returns>
		private static List<WindowDetails> FindWindow(List<WindowDetails> windows, string titlePattern, string classnamePattern) {
			List<WindowDetails> foundWindows = new List<WindowDetails>();
			Regex titleRegexp = null;
			Regex classnameRegexp = null;
			
			if (titlePattern != null && titlePattern.Trim().Length > 0) {
				titleRegexp = new Regex(titlePattern);
			}
			if (classnamePattern != null && classnamePattern.Trim().Length > 0) {
				classnameRegexp = new Regex(classnamePattern);
			}

			foreach(WindowDetails window in windows) {
				if (titleRegexp != null && titleRegexp.IsMatch(window.Text)) {
					foundWindows.Add(window);
				} else if (classnameRegexp != null && classnameRegexp.IsMatch(window.ClassName)) {
					foundWindows.Add(window);
				}
			}
			return foundWindows;;
		}
		
		/// <summary>
		/// Retrieve the child with mathing classname
		/// </summary>
		public WindowDetails GetChild(string childClassname) {
			foreach(WindowDetails child in Children) {
				if (childClassname.Equals(child.ClassName)) {
					return child;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Get the parent of the current window
		/// </summary>
		/// <returns>WindowDetails of the parent, or null if none</returns>
		public WindowDetails GetParent() {
			if (parent == null) {
				IntPtr parenthWnd = User32.GetParent(Handle);
				if (parenthWnd != IntPtr.Zero) {
					parent = new WindowDetails(parenthWnd);
				}
				
			}
			return parent;
		}

		/// <summary>
		/// Retrieve all the children, this only stores the children internally, use the "Children" property for the value
		/// </summary>
		public void GetChildren() {
			childWindows = new List<WindowDetails>();
			WindowsEnumerator windowsEnumerator = new WindowsEnumerator();
			windowsEnumerator.GetWindows(hWnd);
			foreach(WindowDetails childWindow in windowsEnumerator.Items) {
				// Copy "Parent title"
				if (childWindow.Text != null && childWindow.Text.Trim().Length == 0) {
					childWindow.Text = Text;
				}
				childWindow.parent = this;
				childWindows.Add(childWindow);
			}
		}

		/// <summary>
		/// Retrieve children with a certain title or classname
		/// </summary>
		/// <param name="titlePattern">The regexp to look for in the title</param>
		/// <param name="classnamePattern">The regexp to look for in the classname</param>
		/// <returns>List<WindowDetails> with all the found windows, or an emptry list</returns>
		public List<WindowDetails> FindChildren(string titlePattern, string classnamePattern) {
			return FindWindow(Children, titlePattern, classnamePattern);
		}
		
		/// <summary>
		/// Recursing helper method for the FindPath
		/// </summary>
		/// <param name="classnames">List<string> with classnames</param>
		/// <param name="index">The index in the list to look for</param>
		/// <returns>WindowDetails if a match was found</returns>
		private WindowDetails FindPath(List<string> classnames, int index) {
			WindowDetails resultWindow = null;
			List<WindowDetails> foundWindows = FindChildren(null, classnames[index]);
			if (index == classnames.Count - 1) {
				if (foundWindows.Count > 0) {
					resultWindow = foundWindows[0];
				}
			} else {
				foreach(WindowDetails foundWindow in foundWindows) {
					resultWindow = foundWindow.FindPath(classnames, index+1);
					if (resultWindow != null) {
						break;
					}
				}
			}
			return resultWindow;
		}

		/// <summary>
		/// This method will find the child window according to a path of classnames.
		/// Usually used for finding a certain "content" window like for the IE Browser
		/// </summary>
		/// <param name="classnames">List<string> with classname "path"</param>
		/// <param name="allowSkip">true allows the search to skip a classname of the path</param>
		/// <returns>WindowDetails if found</returns>
		public WindowDetails FindPath(List<string> classnames, bool allowSkip) {
			int index = 0;
			WindowDetails resultWindow = FindPath(classnames, index++);
			if (resultWindow == null && allowSkip) {
				while(resultWindow == null && index < classnames.Count) {
					resultWindow = FindPath(classnames, index);
				}
			}
			return resultWindow;
		}

		/// <summary>
		/// Deep scan for a certain classname pattern
		/// </summary>
		/// <param name="classnamePattern">Classname regexp pattern</param>
		/// <returns>The first WindowDetails found</returns>
		public static WindowDetails DeepScan(WindowDetails windowDetails, Regex classnamePattern) {
			if (classnamePattern.IsMatch(windowDetails.ClassName)) {
				return windowDetails;
			}
			// First loop through this level
			foreach(WindowDetails child in windowDetails.Children) {
				if (classnamePattern.IsMatch(child.ClassName)) {
					return child;
				}
			}
			// Go into all children
			foreach(WindowDetails child in windowDetails.Children) {
				WindowDetails deepWindow = DeepScan(child, classnamePattern);
				if (deepWindow != null) {
					return deepWindow;
				}
			}
			return null;
		}

		/// <summary>
		/// GetWindow
		/// </summary>
		/// <param name="gwCommand">The GetWindowCommand to use</param>
		/// <returns>null if nothing found, otherwise the WindowDetails instance of the "child"</returns>
		public WindowDetails GetWindow(GetWindowCommand gwCommand) {
			IntPtr tmphWnd = User32.GetWindow(Handle, gwCommand);
			WindowDetails windowDetails = new WindowDetails(tmphWnd);
			windowDetails.parent = this;
			return windowDetails;
		}

		/// <summary>
		/// Gets the window's handle
		/// </summary>
		public IntPtr Handle {
			get {
				return this.hWnd;
			}
		}

		private string text = null;
		/// <summary>
		/// Gets the window's title (caption)
		/// </summary>
		public string Text {
			set {
				text = value;
			}
			get {
				if (text == null) {
					StringBuilder title = new StringBuilder(260, 260);
					User32.GetWindowText(this.hWnd, title, title.Capacity);
					text = title.ToString();
				}
				return text;
			}
		}

		private string className = null;
		/// <summary>
		/// Gets the window's class name.
		/// </summary>
		public string ClassName {
			get {
				if (className == null) {
					StringBuilder classNameBuilder = new StringBuilder(260, 260);
					User32.GetClassName(this.hWnd, classNameBuilder, classNameBuilder.Capacity);
					className = classNameBuilder.ToString();
				}
				return className;
			}
		}

		/// <summary>
		/// Gets/Sets whether the window is iconic (mimimised) or not.
		/// </summary>
		public bool Iconic {
			get {
				return User32.IsIconic(this.hWnd) || Location.X <= -32000;
			}
			set {
				User32.SendMessage(
					this.hWnd, 
					User32.WM_SYSCOMMAND, 
					(IntPtr)User32.SC_MINIMIZE,
					IntPtr.Zero);
			}
		}
			
		/// <summary>
		/// Gets/Sets whether the window is maximised or not.
		/// </summary>
		public bool Maximised {
			get {
				return User32.IsZoomed(this.hWnd);
			}
			set {
				User32.SendMessage(
					this.hWnd,
					User32.WM_SYSCOMMAND, 
					(IntPtr)User32.SC_MAXIMIZE,
					IntPtr.Zero);
			}
		}

		/// <summary>
		/// Gets whether the window is visible.
		/// </summary>
		public bool Visible {
			get {
				return User32.IsWindowVisible(this.hWnd);
			}
		}
		
		public IntPtr ProcessId {
			get {
				IntPtr processId;
				User32.GetWindowThreadProcessId(Handle, out processId);
				return processId;
			}
		}

		/// <summary>
		/// Gets the bounding rectangle of the window
		/// </summary>
		public Rectangle ClientRectangle {
			get {
				Rectangle clientRect = Rectangle.Empty;

				if (parent == null && DWM.isDWMEnabled()) {
					GetExtendedFrameBounds(out clientRect);
				}
				if (clientRect.IsEmpty) {
					GetClientRect(out clientRect);
				}

				if (this.Maximised) {
					Size size = Size.Empty;
					GetBorderSize(out size);
					return new Rectangle(clientRect.X + size.Width, clientRect.Y + size.Height, clientRect.Width - (2 * size.Width), clientRect.Height - (2 * size.Height));
				}
				return clientRect;
			}
		}

		/// <summary>
		/// Gets the location of the window relative to the screen.
		/// </summary>
		public Point Location {
			get {
				Rectangle tmpRectangle = ClientRectangle;
				return new Point(tmpRectangle.Left, tmpRectangle.Top);
			}
		}

		/// <summary>
		/// Gets the size of the window.
		/// </summary>
		public Size Size {
			get {
				Rectangle tmpRectangle = ClientRectangle;
				return new Size(tmpRectangle.Right - tmpRectangle.Left, tmpRectangle.Bottom - tmpRectangle.Top);
			}
		}

		/// <summary>
		/// Restores and Brings the window to the front, 
		/// assuming it is a visible application window.
		/// </summary>
		public void Restore() {
			if (Iconic) {
				User32.SendMessage(this.hWnd, User32.WM_SYSCOMMAND, (IntPtr)User32.SC_RESTORE, IntPtr.Zero);
			}
			User32.BringWindowToTop(this.hWnd);
			User32.SetForegroundWindow(this.hWnd);
			// Make sure windows has time to perform the action
			Thread.Sleep(100);
		}

		public WindowStyleFlags WindowStyle {
			get {
				return (WindowStyleFlags)User32.GetWindowLong(this.hWnd, User32.GWL_STYLE);
			}
		}

		public WindowPlacement GetWindowPlacement() {
			WindowPlacement placement = WindowPlacement.Default;
			User32.GetWindowPlacement(this.Handle, ref placement);
			return placement;
		}

		public void SetWindowPlacement(WindowPlacement placement) {
			User32.SetWindowPlacement(this.Handle, ref placement);
		}

		public ExtendedWindowStyleFlags ExtendedWindowStyle {
			get {
				return (ExtendedWindowStyleFlags)User32.GetWindowLong(this.hWnd, User32.GWL_EXSTYLE);
			}
		}

		/// <summary>
		/// Capture Window with GDI+
		/// </summary>
		/// <param name="capture">The capture to fill</param>
		/// <returns>ICapture</returns>
		public ICapture CaptureWindow(ICapture capture) {
			if (capture == null) {
				capture = new Capture();
			}
			capture.Image = PrintWindow();
			capture.Location = Location;
			return capture;
		}

		/// <summary>
		/// Capture DWM Window
		/// </summary>
		/// <param name="capture"></param>
		/// <returns></returns>
		public ICapture CaptureDWMWindow(ICapture capture, WindowCaptureMode windowCaptureMode) {
			if (capture == null) {
				capture = new Capture();
			}

			// Prepare the form to copy from
			using (Form tempForm = new Form()) {
				tempForm.ShowInTaskbar = false;
				tempForm.FormBorderStyle = FormBorderStyle.None;
				tempForm.TopMost = true;
				// Transparent would give an error here
				if (windowCaptureMode == WindowCaptureMode.Aero) {
					Color formColor = conf.DWMBackgroundColor;
					// Remove transparency, this will break the capturing
					formColor = Color.FromArgb(255, formColor.R, formColor.G, formColor.B);
					tempForm.BackColor = formColor;
				} else {
					// Make sure the user sees transparent when going into the color picker
					conf.DWMBackgroundColor = Color.Transparent;
					// Use white, later black to capture transparent
					tempForm.BackColor = Color.White;
				}

				// Register the Thumbnail
				IntPtr thumbnailHandle;
				DWM.DwmRegisterThumbnail(tempForm.Handle, Handle, out thumbnailHandle);

				// Get the original size
				SIZE sourceSize;
				DWM.DwmQueryThumbnailSourceSize(thumbnailHandle, out sourceSize);

				if (sourceSize.cx <= 0 || sourceSize.cy <= 0) {
					return null;
				}

				// Calculate the location of the temp form
				Point formLocation;
				Rectangle windowRectangle;
				Size borderSize = new Size();
				// Get the Location/size with DWM (should work, as we are making a DWM Capture)
				if (!GetExtendedFrameBounds(out windowRectangle)) {
					// Fallback
					GetClientRect(out windowRectangle);
				}

				if (!Maximised) {
					Screen displayScreen = null;

					// Find the screen where the window is in
					foreach(Screen screen in Screen.AllScreens) {
						if (screen.WorkingArea.Contains(windowRectangle)) {
							displayScreen = screen;
						}
					}
					// If none found we check which screen it will fit...
					if (displayScreen == null) {
						foreach(Screen screen in Screen.AllScreens) {
							if (displayScreen == null || (screen.Bounds.Width >= displayScreen.Bounds.Width && screen.Bounds.Height >= displayScreen.Bounds.Height)) {
								displayScreen = screen;
							}
						}
						// Found one?
						if (displayScreen != null) {
							formLocation = new Point(displayScreen.Bounds.X, displayScreen.Bounds.Y);
						} else {
							// No, just use the primary screen
							formLocation = new Point(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y);
						}
					} else {
						// The window actually fits, so while capturing create the copy over the original
						formLocation = new Point(windowRectangle.X, windowRectangle.Y);
					}
				} else {
					//GetClientRect(out windowRectangle);
					GetBorderSize(out borderSize);
					formLocation = new Point(windowRectangle.X, windowRectangle.Y);
				}

				tempForm.Location = formLocation;
				tempForm.Size = sourceSize.ToSize();
				// Make sure the to be captured window is active, important for the close/resize buttons
				this.Restore();

				// Prepare the displaying of the Thumbnail
				DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
				props.Opacity = (byte)255;
				props.Visible = true;
				props.Destination = new RECT(0, 0, sourceSize.cx, sourceSize.cy);
				DWM.DwmUpdateThumbnailProperties(thumbnailHandle, ref props);
				tempForm.Show();

				// Wait, to make sure everything is visible
				Thread.Sleep(250);

				// Prepare rectangle to capture from the screen.
				Rectangle captureRectangle = new Rectangle(formLocation.X, formLocation.Y, sourceSize.cx, sourceSize.cy);
				if (Maximised) {
					// Correct capture size for maximized window by offsetting the X,Y with the border size
					captureRectangle.X += borderSize.Width;
					captureRectangle.Y += borderSize.Height;
					// and subtrackting the border from the size (2 times, as we move right/down for the capture without resizing)
					captureRectangle.Width -= 2 * borderSize.Width;
					captureRectangle.Height -= 2 * borderSize.Height;
				}
				
				// Destination bitmap for the capture
				Bitmap capturedBitmap = null;
				// Check if we make a transparent capture
				if (windowCaptureMode == WindowCaptureMode.AeroTransparent) {
					try {
						this.FreezeWindow();
						using (Bitmap whiteBitmap = WindowCapture.CaptureRectangle(captureRectangle)) {
							// Apply a white color
							tempForm.BackColor = Color.Black;
							// Refresh the form, otherwise the color doesn't show.
							tempForm.Refresh();
							using (Bitmap blackBitmap = WindowCapture.CaptureRectangle(captureRectangle)) {
								capturedBitmap = ApplyTransparency(blackBitmap, whiteBitmap);
							}
						}
					} catch (Exception e) {
						LOG.Debug("Exception: ", e);
						// Some problem occured, cleanup and make a normal capture
						if (capturedBitmap != null) {
							capturedBitmap.Dispose();
							capturedBitmap = null;
						}
					} finally {
						// Make sure to ALWAYS unfreeze!!
						this.UnfreezeWindow();
					}
				}
				// If no capture up till now, create a normal capture.
				if (capturedBitmap == null) {
					// Capture from the screen
					capturedBitmap = WindowCapture.CaptureRectangle(captureRectangle);
				}
				// Unregister (cleanup), as we are finished we don't need the form or the thumbnail anymore
				DWM.DwmUnregisterThumbnail(thumbnailHandle);
				tempForm.Close();
				
				capture.Image = capturedBitmap;
				// Make sure the capture location is the location of the window, not the copy
				capture.Location = Location;
			}

			return capture;
		}

		/// <summary>
		/// Apply transparency by comparing a transparent capture with a black and white background
		/// A "Math.min" makes sure there is no overflow, but this could cause the picture to have shifted colors.
		/// The pictures should have been taken without differency, exect for the colors.
		/// </summary>
		/// <param name="blackBitmap">Bitmap with the black image</param>
		/// <param name="whiteBitmap">Bitmap with the black image</param>
		/// <returns>Bitmap with transparency</returns>
		private Bitmap ApplyTransparency(Bitmap blackBitmap, Bitmap whiteBitmap) {
			Bitmap returnBitmap = new Bitmap(blackBitmap.Width, blackBitmap.Height, PixelFormat.Format32bppArgb);
			using (BitmapBuffer blackBuffer = new BitmapBuffer(blackBitmap, false)) {
				blackBuffer.Lock();
				using (BitmapBuffer whiteBuffer = new BitmapBuffer(whiteBitmap, false)) {
					whiteBuffer.Lock();
					using (BitmapBuffer targetBuffer = new BitmapBuffer(returnBitmap, false)) {
						targetBuffer.Lock();
						for(int y=0; y<blackBuffer.Height;y++) {
							for(int x=0; x<blackBuffer.Width;x++) {
								Color c0 = blackBuffer.GetColorAt(x,y);
								Color c1 = whiteBuffer.GetColorAt(x,y);
								// Calculate alpha as double in range 0-1
								double alpha = (c0.R - c1.R + 255) / 255d;
								if (alpha == 1) {
									// Alpha == 1 means no change!
									targetBuffer.SetColorAt(x, y, c0); 
								} else if (alpha == 0) {
									// Complete transparency, use transparent pixel
									targetBuffer.SetColorAt(x, y, Color.Transparent);
								} else {
									// Calculate original color
									byte originalAlpha = (byte)Math.Min(255, alpha * 255);
									//LOG.DebugFormat("Alpha {0} & c0 {1} & c1 {2}", alpha, c0, c1);
									byte originalRed = (byte)Math.Min(255, c0.R / alpha);
									byte originalGreen = (byte)Math.Min(255, c0.G / alpha);
									byte originalBlue = (byte)Math.Min(255, c0.B / alpha);
									Color originalColor = Color.FromArgb(originalAlpha, originalRed, originalGreen, originalBlue);
									//Color originalColor = Color.FromArgb(originalAlpha, originalRed, c0.G, c0.B);
									targetBuffer.SetColorAt(x, y, originalColor); 
								}
							}
						}
					}
				}
			}
			return returnBitmap;
		}
		
		/// <summary>
		/// Helper method to get the window size for DWM Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>
		private bool GetExtendedFrameBounds(out Rectangle rectangle) {
			RECT rect;
			int result = DWM.DwmGetWindowAttribute(Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
			if (result >= 0) {
				rectangle = rect.ToRectangle();
				return true;
			}
			rectangle = Rectangle.Empty;
			return false;
		}

		/// <summary>
		/// Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>		
		private bool GetClientRect(out Rectangle rectangle) {
			WindowInfo windowInfo = new WindowInfo();
			// Get the Window Info for this window
			bool result = User32.GetWindowInfo(Handle, ref windowInfo);
			if (result) {
				rectangle = windowInfo.rcWindow.ToRectangle();
			} else {
				rectangle = Rectangle.Empty;
			}
			return result;
		}

		/// <summary>
		/// Helper method to get the Border size for GDI Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>	
		private bool GetBorderSize(out Size size) {
			WindowInfo windowInfo = new WindowInfo();
			// Get the Window Info for this window
			bool result = User32.GetWindowInfo(Handle, ref windowInfo);
			if (result) {
				size = new Size((int)windowInfo.cxWindowBorders, (int)windowInfo.cyWindowBorders);
			} else {
				size = Size.Empty;
			}
			return result;
		}
		
		/// <summary>
		/// Set the window as foreground window
		/// </summary>
		public static void ToForeground(IntPtr handle) {
			User32.SetForegroundWindow(handle);
		}

		/// <summary>
		/// Set the window as foreground window
		/// </summary>
		public void ToForeground() {
			ToForeground(this.Handle);
		}
		
		/// <summary>
		/// Get the region for a window
		/// </summary>
		private Region GetRegion() {
			IntPtr windowRegionPtr = GDI32.CreateRectRgn(0,0,0,0);
			RegionResult result = User32.GetWindowRgn(Handle, windowRegionPtr);
			if (result != RegionResult.REGION_ERROR && result != RegionResult.REGION_NULLREGION) {
				return Region.FromHrgn(windowRegionPtr);
			}
			return null;
		}
		
		/// <summary>
		/// PrintWindow but with VScroll loop
		/// </summary>
		/// <returns>Image with all the scroll contents</returns>
		public Image PrintWithVScroll() {
			SCROLLINFO scrollinfo = new SCROLLINFO();
			scrollinfo.cbSize = Marshal.SizeOf( scrollinfo );
			scrollinfo.fMask = (int) ScrollInfoMask.SIF_ALL;
			User32.GetScrollInfo(Handle, (int) ScrollBarDirection.SB_VERT, ref scrollinfo );

			LOG.DebugFormat("nMax {0}", scrollinfo.nMax);
			LOG.DebugFormat("nMin {0}", scrollinfo.nMin);
			LOG.DebugFormat("nPage {0}", scrollinfo.nPage);
			LOG.DebugFormat("nPos {0}", scrollinfo.nPos);
			LOG.DebugFormat("nTrackPos {0}", scrollinfo.nTrackPos);
			
			Rectangle clientRectangle = ClientRectangle;
			int pages = ((scrollinfo.nMax+1) / scrollinfo.nPage);
			int width = clientRectangle.Width;
			int height = clientRectangle.Height * pages;
			Bitmap returnBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			using (Graphics graphicsTarget = Graphics.FromImage(returnBitmap)) {
				for(int page=0; page<pages; page++) {
					VScroll(page * scrollinfo.nPage);
					Thread.Sleep(200);
					using (Image printedWindow = PrintWindow()) {
						if (printedWindow != null) {
							graphicsTarget.DrawImage(printedWindow, new Point(0, clientRectangle.Height * page));
							graphicsTarget.Flush();
						}
					}
				}
			}
			return returnBitmap;
		}

		/// <summary>
		/// Does the window have a HScrollbar?
		/// </summary>
		/// <returns>true if a HScrollbar is available</returns>
		public bool hasHScroll() {
			return (WindowStyle & WindowStyleFlags.WS_HSCROLL) == WindowStyleFlags.WS_HSCROLL;
		}		

		/// <summary>
		/// Does the window have a VScrollbar?
		/// </summary>
		/// <returns>true if a VScrollbar is available</returns>
		public bool hasVScroll() {
			return (WindowStyle & WindowStyleFlags.WS_VSCROLL) == WindowStyleFlags.WS_VSCROLL;
		}

		/// <summary>
		/// Scroll the window to the hpos location
		/// </summary>
		/// <param name="hpos"></param>
		public void HScroll(int hpos) {
			User32.SetScrollPos(Handle, Orientation.Horizontal, hpos, true);
			User32.PostMessage(Handle, (int)WindowsMessages.WM_HSCROLL, (int)ScrollbarCommand.SB_THUMBPOSITION + (hpos<<16), 0);
		}
		
		/// <summary>
		/// Scroll the window to the vpos location
		/// </summary>
		/// <param name="vpos"></param>
		public void VScroll(int vpos) {
			User32.SetScrollPos(Handle, Orientation.Vertical, vpos, true);
			User32.PostMessage(Handle, (int)WindowsMessages.WM_VSCROLL, (int)ScrollbarCommand.SB_THUMBPOSITION + (vpos<<16), 0);
		}

		/// <summary>
		/// Freezes the process belonging to the window
		/// Warning: Use only if no other way!!
		/// </summary>
		private void FreezeWindow() {
			Process proc = Process.GetProcessById(this.ProcessId.ToInt32());
		
			if (proc.ProcessName == string.Empty){
				return;
			}
			if (proc.ProcessName.ToLower().Contains("greenshot")) {
				LOG.DebugFormat("Not freezing ourselves, process was: {0}", proc.ProcessName);
				return;
			}
			LOG.DebugFormat("Freezing process: {0}", proc.ProcessName);
		
			foreach (ProcessThread pT in proc.Threads) {
				IntPtr pOpenThread = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
		
				if (pOpenThread == IntPtr.Zero) {
					break;
				}
		
				Kernel32.SuspendThread(pOpenThread);
			}
		}

		/// <summary>
		/// Unfreeze the process belonging to the window
		/// </summary>
		public void UnfreezeWindow() {
			Process proc = Process.GetProcessById(this.ProcessId.ToInt32());
		
			if (proc.ProcessName == string.Empty) {
				return;
			}
			if (proc.ProcessName.ToLower().Contains("greenshot")) {
				LOG.DebugFormat("Not unfreezing ourselves, process was: {0}", proc.ProcessName);
				return;
			}
			LOG.DebugFormat("Unfreezing process: {0}", proc.ProcessName);
			
			foreach (ProcessThread pT in proc.Threads) {
				IntPtr pOpenThread = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
		
				if (pOpenThread == IntPtr.Zero) {
					break;
				}
		
				Kernel32.ResumeThread(pOpenThread);
			}
		}

		/// <summary>
		/// Return an Image representating the Window!
		/// As GDI+ draws it, it will be without Aero borders!
		/// </summary>
		public Image PrintWindow() {
			Rectangle clientRect = Rectangle.Empty;
			GetClientRect(out clientRect);

			// Start the capture
			bool printSucceeded = true;
			Region region = GetRegion();
			PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
			// Only use 32 bpp ARGB when the window has a region
			if (region != null) {
				pixelFormat = PixelFormat.Format32bppArgb;
			}
			Image returnImage = new Bitmap(clientRect.Width, clientRect.Height, pixelFormat);
			using (Graphics graphics = Graphics.FromImage(returnImage)) {
				IntPtr hDCDest = graphics.GetHdc();
				printSucceeded = User32.PrintWindow(Handle, hDCDest, 0x0);
				graphics.ReleaseHdc(hDCDest);

				// Apply the region "transparency"
				if (region != null && !region.IsEmpty(graphics)) {
					graphics.ExcludeClip(region);
					graphics.Clear(Color.Transparent);
				}

				graphics.Flush();
			}

			// Return null if error
			if (!printSucceeded) {
				LOG.Debug("Error??");
				returnImage.Dispose();
				return null;
			}
			if (this.Maximised) {
				LOG.Debug("Correcting for maximalization");
				Size size = Size.Empty;
				GetBorderSize(out size);
				Rectangle borderRectangle = new Rectangle(size.Width, size.Height, clientRect.Width - (2 * size.Width), clientRect.Height - (2 * size.Height));
				ImageHelper.Crop(ref returnImage, ref borderRectangle);
			}
			return returnImage;
		}
	
		/// <summary>
		///  Constructs a new instance of this class for
		///  the specified Window Handle.
		/// </summary>
		/// <param name="hWnd">The Window Handle</param>
		public WindowDetails(IntPtr hWnd) {
			this.hWnd = hWnd;
		}
		
		/// <summary>
		/// Gets an instance of the current active foreground window
		/// </summary>
		/// <returns>WindowDetails of the current window</returns>
		public static WindowDetails GetActiveWindow() {
			IntPtr hWnd = User32.GetForegroundWindow();
			if (hWnd != null && hWnd != IntPtr.Zero) {
				return new WindowDetails(hWnd);
			}
			return null;
		}
		
		/// <summary>
		/// Gets the Destop window
		/// </summary>
		/// <returns>WindowDetails for the destop window</returns>
		public static WindowDetails GetDesktopWindow() {
			return new WindowDetails(User32.GetDesktopWindow());
		}
		
		/// <summary>
		/// Get all the top level windows
		/// </summary>
		/// <returns>List<WindowDetails> with all the top level windows</returns>
		public static List<WindowDetails> GetAllWindows() {
			return GetAllWindows(null);
		}

		/// <summary>
		/// Get all the top level windows, with matching classname
		/// </summary>
		/// <returns>List<WindowDetails> with all the top level windows</returns>
		public static List<WindowDetails> GetAllWindows(string classname) {
			List<WindowDetails> windows = new List<WindowDetails>();
			WindowsEnumerator windowsEnumerator = new WindowsEnumerator();
			windowsEnumerator.GetWindows();
			foreach(WindowDetails window in windowsEnumerator.Items) {
				if (classname == null || window.ClassName.Equals(classname)) {
					windows.Add(window);
				}
			}
			return windows;
		}

		/// <summary>
		/// Get all the visible top level windows
		/// </summary>
		/// <returns>List<WindowDetails> with all the visible top level windows</returns>
		public static List<WindowDetails> GetVisibleWindows() {
			List<WindowDetails> windows = new List<WindowDetails>();
			List<WindowDetails> allWindows = WindowDetails.GetAllWindows();
			foreach(WindowDetails window in allWindows) {
				// Ignore windows without title
				if (window.Text.Length == 0) {
					continue;
				}
				// Ignore invisible
				if (!window.Visible) {
					continue;
				}
				// Ignore some classes
				List<string> ignoreClasses = new List<string>(new string[] {"Progman", "XLMAIN", "Button"}); //"MS-SDIa"
				if (ignoreClasses.Contains(window.ClassName)) {
					continue;
				}
				// Ignore us
				if (window.Text.Equals(lang.GetString(LangKey.application_title))) {
					continue;
				}
				// Windows without size
				Size windowSize = window.ClientRectangle.Size;
				if (windowSize.Width == 0 ||  windowSize.Height == 0) {
					continue;
				}
				windows.Add(window);
			}
			return windows;
		}

		/// <summary>
		/// Find a window belonging to the same process as the supplied window.
		/// </summary>
		/// <param name="windowToLinkTo"></param>
		/// <returns></returns>
		public static WindowDetails GetLinkedWindow(WindowDetails windowToLinkTo) {
			IntPtr processIdSelectedWindow = windowToLinkTo.ProcessId;
			foreach(WindowDetails window in WindowDetails.GetAllWindows()) {
				// Ignore windows without title
				if (window.Text.Length == 0) {
					continue;
				}
				// Ignore invisible
				if (!window.Visible) {
					continue;
				}
				if (window.Handle == windowToLinkTo.Handle) {
					continue;
				}
				if (window.Iconic) {
					continue;
				}
				    
				// Windows without size
				Size windowSize = window.ClientRectangle.Size;
				if (windowSize.Width == 0 ||  windowSize.Height == 0) {
					continue;
				}

				if (window.ProcessId == processIdSelectedWindow) {
					LOG.InfoFormat("Found window {0} belonging to same process as the window {1}", window.Text, windowToLinkTo.Text);
					return window;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Helper method to "active" all windows that are not in the supplied list.
		/// One should preferably call "GetVisibleWindows" for the oldWindows.
		/// </summary>
		/// <param name="oldWindows">List<WindowDetails> with old windows</param>
		public static void ActiveNewerWindows(List<WindowDetails> oldWindows) {
			List<WindowDetails> windowsAfter = WindowDetails.GetVisibleWindows();
			foreach(WindowDetails window in windowsAfter) {
				if (!oldWindows.Contains(window)) {
					window.ToForeground();
				}
			}
		}
	}
	#endregion
}
