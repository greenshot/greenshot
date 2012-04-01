/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Plugin;
using GreenshotPlugin.UnmanagedHelpers;
using Greenshot.IniFile;

/// <summary>
/// Code for handling with "windows"
/// Main code is taken from vbAccelerator, location:
/// http://www.vbaccelerator.com/home/NET/Code/Libraries/Windows/Enumerating_Windows/article.asp
/// but a LOT of changes/enhancements were made to adapt it for Greenshot.
/// </summary>
namespace GreenshotPlugin.Core  {
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
		public WindowsEnumerator GetWindows() {
			GetWindows(IntPtr.Zero, null);
			return this;
		}

		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="hWndParent">Window Handle to get children for</param>
		public WindowsEnumerator GetWindows(WindowDetails parent) {
			if (parent != null) {
				GetWindows(parent.Handle, null);
			} else {
				GetWindows(IntPtr.Zero, null);
			}
			return this;
		}
		
		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="hWndParent">Window Handle to get children for</param>
		/// <param name="classname">Window Classname to copy, use null to copy all</param>
		public WindowsEnumerator GetWindows(IntPtr hWndParent, string classname) {
			this.items = new List<WindowDetails>();
			List<WindowDetails> windows = new List<WindowDetails>();
			User32.EnumChildWindows(hWndParent, new EnumWindowsProc(this.WindowEnum), 0);

			bool hasParent = !IntPtr.Zero.Equals(hWndParent);
			string parentText = null;
			if (hasParent) {
				StringBuilder title = new StringBuilder(260, 260);
				User32.GetWindowText(hWndParent, title, title.Capacity);
				parentText = title.ToString();
			}

			foreach (WindowDetails window in items) {
				if (hasParent) {
					window.Text = parentText;
					window.ParentHandle = hWndParent;
				}
				if (classname == null || window.ClassName.Equals(classname)) {
					windows.Add(window);
				}
			}
			items = windows;
			return this;
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
			if (!WindowDetails.isIgnoreHandle(hWnd)) {
				items.Add(new WindowDetails(hWnd));
			}
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
		private static List<IntPtr> ignoreHandles = new List<IntPtr>();
		private static Dictionary<string, Image> iconCache = new Dictionary<string, Image>();
		
		internal static bool isIgnoreHandle(IntPtr handle) {
			return ignoreHandles.Contains(handle);
		}

		private List<WindowDetails> childWindows = null;
		private IntPtr parentHandle = IntPtr.Zero;
		private WindowDetails parent = null;
		private bool frozen = false;

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
		
		public void FreezeDetails() {
			frozen = true;
		}
		public void UnfreezeDetails() {
			frozen = false;
		}

		/// <summary>
		/// Get the icon belonging to the process
		/// </summary>
		public Image DisplayIcon {
			get {
				try {
					string filename = Process.MainModule.FileName;
					if (!iconCache.ContainsKey(filename)) {
						Image icon = null;
						if (File.Exists(filename)) {
							using (Icon appIcon = Icon.ExtractAssociatedIcon(filename)) {
								if (appIcon != null) {
									icon = appIcon.ToBitmap();
								}
							}
						}
						iconCache.Add(filename, icon);
					}
					return iconCache[filename];
				} catch (Exception ex) {
					LOG.WarnFormat("Couldn't get icon for window {0} due to: {1}", Text, ex.Message);
				}
				return null;
			}
		}		
		/// <summary>
		/// Use this to make remove internal windows, like the mainform and the captureforms, invisible
		/// </summary>
		/// <param name="ignoreHandle"></param>
		public static void RegisterIgnoreHandle(IntPtr ignoreHandle) {
			ignoreHandles.Add(ignoreHandle);
		}

		/// <summary>
		/// Use this to remove the with RegisterIgnoreHandle registered handle
		/// </summary>
		/// <param name="ignoreHandle"></param>
		public static void UnregisterIgnoreHandle(IntPtr ignoreHandle) {
			ignoreHandles.Remove(ignoreHandle);
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
			return foundWindows;
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
		
		public IntPtr ParentHandle {
			get {
				if (parentHandle == IntPtr.Zero) {
					parentHandle = User32.GetParent(Handle);
					parent = null;
				}
				return parentHandle;
			}
			set {
				if (parentHandle != value) {
					parentHandle = value;
					parent = null;
				}
			}
		}
		/// <summary>
		/// Get the parent of the current window
		/// </summary>
		/// <returns>WindowDetails of the parent, or null if none</returns>
		public WindowDetails GetParent() {
			if (parent == null) {
				if (parentHandle == IntPtr.Zero) {
					parentHandle = User32.GetParent(Handle);
				}
				if (parentHandle != IntPtr.Zero) {
					parent = new WindowDetails(parentHandle);
				}
			}
			return parent;
		}

		/// <summary>
		/// Retrieve all the children, this only stores the children internally.
		/// One should normally use the getter "Children"
		/// </summary>
		public List<WindowDetails> GetChildren() {
			if (childWindows == null) {
				return GetChildren(0);
			}
			return childWindows;
		}

		/// <summary>
		/// Retrieve all the children, this only stores the children internally, use the "Children" property for the value
		/// </summary>
		/// <param name="levelsToGo">Specify how many levels we go in</param>
		public List<WindowDetails> GetChildren(int levelsToGo) {
			if (childWindows == null) {
				childWindows = new List<WindowDetails>();
				foreach(WindowDetails childWindow in new WindowsEnumerator().GetWindows(hWnd, null).Items) {
					childWindows.Add(childWindow);
					if (levelsToGo > 0) {
						childWindow.GetChildren(levelsToGo-1);
					}
				}
			}
			return childWindows;
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
		
		public bool HasParent {
			get {
				return !IntPtr.Zero.Equals(parentHandle);
			}
		}
		
		public IntPtr ProcessId {
			get {
				IntPtr processId;
				User32.GetWindowThreadProcessId(Handle, out processId);
				return processId;
			}
		}

		public Process Process {
			get {
				try {
					IntPtr processId;
					User32.GetWindowThreadProcessId(Handle, out processId);
					Process process = Process.GetProcessById(processId.ToInt32());
					if (process != null) {
						return process;
					}
				} catch (Exception ex) {
					LOG.Warn(ex);
				}
				return null;
			}
		}

		/// <summary>
		/// Make sure the next call of a cached value is guaranteed the real value
		/// </summary>
		public void Reset() {
			previousWindowRectangle = Rectangle.Empty;
		}

		private Rectangle previousWindowRectangle = Rectangle.Empty;
		private long lastWindowRectangleRetrieveTime = 0;
		private const long CACHE_TIME = TimeSpan.TicksPerSecond * 2;
		/// <summary>
		/// Gets the bounding rectangle of the window
		/// </summary>
		public Rectangle WindowRectangle {
			get {
				// Try to return a cached value
				long now = DateTime.Now.Ticks;
				if (previousWindowRectangle.IsEmpty || !frozen) {
					if (previousWindowRectangle.IsEmpty || now - lastWindowRectangleRetrieveTime > CACHE_TIME) {
						Rectangle windowRect = Rectangle.Empty;
						if (!HasParent && DWM.isDWMEnabled()) {
							GetExtendedFrameBounds(out windowRect);
						}

						if (windowRect.IsEmpty) {
							GetWindowRect(out windowRect);
						}
	
						if (!HasParent && this.Maximised) {
							Size size = Size.Empty;
							GetBorderSize(out size);
							windowRect = new Rectangle(windowRect.X + size.Width, windowRect.Y + size.Height, windowRect.Width - (2 * size.Width), windowRect.Height - (2 * size.Height));
						}
						lastWindowRectangleRetrieveTime = now;
						// Try to return something valid, by getting returning the previous size if the window doesn't have a Rectangle anymore
						if (windowRect.IsEmpty) {
							return previousWindowRectangle;
						}
						previousWindowRectangle = windowRect;
						return windowRect;
					}
				}
				return previousWindowRectangle;
			}
		}

		/// <summary>
		/// Gets the location of the window relative to the screen.
		/// </summary>
		public Point Location {
			get {
				Rectangle tmpRectangle = WindowRectangle;
				return new Point(tmpRectangle.Left, tmpRectangle.Top);
			}
		}

		/// <summary>
		/// Gets the size of the window.
		/// </summary>
		public Size Size {
			get {
				Rectangle tmpRectangle = WindowRectangle;
				return new Size(tmpRectangle.Right - tmpRectangle.Left, tmpRectangle.Bottom - tmpRectangle.Top);
			}
		}
		
		/// <summary>
		/// Get the client rectangle, this is the part of the window inside the borders (drawable area)
		/// </summary>
		public Rectangle ClientRectangle {
			get {
				Rectangle clientRect = Rectangle.Empty;
				GetClientRect(out clientRect);
				return clientRect;
			}
		}

		/// <summary>
		/// Check if the supplied point lies in the window
		/// </summary>
		/// <param name="p">Point with the coordinates to check</param>
		/// <returns>true if the point lies within</returns>
		public bool Contains(Point p) {
			return WindowRectangle.Contains(Cursor.Position);
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
			Application.DoEvents();
		}

		public WindowStyleFlags WindowStyle {
			get {
				return (WindowStyleFlags)User32.GetWindowLong(this.hWnd, (int)WindowLongIndex.GWL_STYLE);
			}
			set {
				User32.SetWindowLong(this.hWnd, (int)WindowLongIndex.GWL_STYLE, (uint)value);
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
				return (ExtendedWindowStyleFlags)User32.GetWindowLong(this.hWnd, (int)WindowLongIndex.GWL_EXSTYLE);
			}
		}

		/// <summary>
		/// Capture Window with GDI+
		/// </summary>
		/// <param name="capture">The capture to fill</param>
		/// <returns>ICapture</returns>
		public ICapture CaptureWindow(ICapture capture) {
			Image capturedImage = PrintWindow();
			if (capturedImage != null) {
				capture.Image = capturedImage;
				capture.Location = Location;
				return capture;
			}
			return null;
		}

		/// <summary>
		/// Capture DWM Window
		/// </summary>
		/// <param name="capture">Capture to fill</param>
		/// <param name="windowCaptureMode">Wanted WindowCaptureMode</param>
		/// <param name="autoMode">True if auto modus is used</param>
		/// <returns>ICapture with the capture</returns>
		public ICapture CaptureDWMWindow(ICapture capture, WindowCaptureMode windowCaptureMode, bool autoMode) {
			IntPtr thumbnailHandle = IntPtr.Zero;
			Form tempForm = null;
			bool tempFormShown = false;
			try {
				tempForm = new Form();
				tempForm.ShowInTaskbar = false;
				tempForm.FormBorderStyle = FormBorderStyle.None;
				tempForm.TopMost = true;

				// Register the Thumbnail
				DWM.DwmRegisterThumbnail(tempForm.Handle, Handle, out thumbnailHandle);

				// Get the original size
				SIZE sourceSize;
				DWM.DwmQueryThumbnailSourceSize(thumbnailHandle, out sourceSize);

				if (sourceSize.cx <= 0 || sourceSize.cy <= 0) {
					return null;
				}

				// Calculate the location of the temp form
				Point formLocation;
				Rectangle windowRectangle = WindowRectangle;
				Size borderSize = new Size();

				if (!Maximised) {
					Screen displayScreen = null;

					// Find the screen where the window is and check if it fits
					foreach(Screen screen in Screen.AllScreens) {
						if (screen.WorkingArea.Contains(windowRectangle)) {
							displayScreen = screen;
							break;
						}
					}
					if (displayScreen == null) {
						// If none found we find the biggest screen
						foreach(Screen screen in Screen.AllScreens) {
							if (displayScreen == null || (screen.Bounds.Width >= displayScreen.Bounds.Width && screen.Bounds.Height >= displayScreen.Bounds.Height)) {
								displayScreen = screen;
							}
						}
						// check if the window will fit
						if (displayScreen != null && displayScreen.Bounds.Contains(new Rectangle(Point.Empty, windowRectangle.Size))) {
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
					formLocation = new Point(windowRectangle.X - borderSize.Width, windowRectangle.Y - borderSize.Height);
				}

				tempForm.Location = formLocation;
				tempForm.Size = sourceSize.ToSize();

				// Prepare rectangle to capture from the screen.
				Rectangle captureRectangle = new Rectangle(formLocation.X, formLocation.Y, sourceSize.cx, sourceSize.cy);
				if (Maximised) {
					// Correct capture size for maximized window by offsetting the X,Y with the border size
					captureRectangle.X += borderSize.Width;
					captureRectangle.Y += borderSize.Height;
					// and subtrackting the border from the size (2 times, as we move right/down for the capture without resizing)
					captureRectangle.Width -= 2 * borderSize.Width;
					captureRectangle.Height -= 2 * borderSize.Height;
				} else if (autoMode) {
					// check if the capture fits
					if (!capture.ScreenBounds.Contains(captureRectangle)) {
						// if GDI is allowed..
						if (conf.isGDIAllowed(Process)) {
							// we return null which causes the capturing code to try another method.
							return null;
						}
					}
				}
				
				// Make sure the to be captured window is active, important for the close/resize buttons
				this.Restore();

				// Prepare the displaying of the Thumbnail
				DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
				props.Opacity = (byte)255;
				props.Visible = true;
				props.Destination = new RECT(0, 0, sourceSize.cx, sourceSize.cy);
				DWM.DwmUpdateThumbnailProperties(thumbnailHandle, ref props);
				tempForm.Show();
				tempFormShown = true;

				// Intersect with screen
				captureRectangle.Intersect(capture.ScreenBounds);
				
				// Destination bitmap for the capture
				Bitmap capturedBitmap = null;
				try {
					this.FreezeWindow();
					// Use red to make removal of the corners possible
					tempForm.BackColor = Color.Red;
					// Make sure everything is visible
					tempForm.Refresh();
					Application.DoEvents();
					using (Bitmap redMask = WindowCapture.CaptureRectangle(captureRectangle)) {
						// Check if we make a transparent capture
						if (windowCaptureMode == WindowCaptureMode.AeroTransparent) {
							// Use white, later black to capture transparent
							tempForm.BackColor = Color.White;
							// Make sure everything is visible
							tempForm.Refresh();
							Application.DoEvents();

							try {
								using (Bitmap whiteBitmap = WindowCapture.CaptureRectangle(captureRectangle)) {
									// Apply a white color
									tempForm.BackColor = Color.Black;
									// Make sure everything is visible
									tempForm.Refresh();
									Application.DoEvents();
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
							}
						}
						// If no capture up till now, create a normal capture.
						if (capturedBitmap == null) {
							// Remove transparency, this will break the capturing
							if (!autoMode) {
								tempForm.BackColor = Color.FromArgb(255, conf.DWMBackgroundColor.R, conf.DWMBackgroundColor.G, conf.DWMBackgroundColor.B);
							} else {
								Color colorizationColor = DWM.ColorizationColor;
								// Modify by losing the transparency and increasing the intensity (as if the background color is white)
								colorizationColor = Color.FromArgb(255, (colorizationColor.R + 255) >> 1, (colorizationColor.G + 255) >> 1, (colorizationColor.B + 255) >> 1);
								tempForm.BackColor = colorizationColor; 
							}
							// Make sure everything is visible
							tempForm.Refresh();
							Application.DoEvents();
							// Capture from the screen
							capturedBitmap = WindowCapture.CaptureRectangle(captureRectangle);
						}
						if (capturedBitmap != null && redMask != null) {
							// Remove corners
							if (!Image.IsAlphaPixelFormat(capturedBitmap.PixelFormat)) {
								LOG.Debug("Changing pixelformat to Alpha for the RemoveCorners");
								Bitmap tmpBitmap = ImageHelper.Clone(capturedBitmap, PixelFormat.Format32bppArgb);
								capturedBitmap.Dispose();
								capturedBitmap = tmpBitmap;
							}
							Color cornerColor = Color.Transparent;
							if (!Image.IsAlphaPixelFormat(capturedBitmap.PixelFormat)) {
								cornerColor = Color.FromArgb(255, conf.DWMBackgroundColor.R, conf.DWMBackgroundColor.G, conf.DWMBackgroundColor.B);
							}
							RemoveCorners(capturedBitmap, redMask, cornerColor);
						}
					}
				} finally {
					// Make sure to ALWAYS unfreeze!!
					this.UnfreezeWindow();
				}
				
				capture.Image = capturedBitmap;
				// Make sure the capture location is the location of the window, not the copy
				capture.Location = Location;
			} finally {
				if (thumbnailHandle != IntPtr.Zero) {
					// Unregister (cleanup), as we are finished we don't need the form or the thumbnail anymore
					DWM.DwmUnregisterThumbnail(thumbnailHandle);
				}
				if (tempForm != null) {
					if (tempFormShown) {
						tempForm.Close();
					}
					tempForm.Dispose();
				}
			}

			return capture;
		}

		/// <summary>
		/// Helper method to remove the corners from a DMW capture
		/// </summary>
		/// <param name="normalBitmap">The bitmap taken which would normally be returned to the editor etc.</param>
		/// <param name="redBitmap">The bitmap taken with a red background</param>
		/// <param name="cornerColor">The background color</param>
		private void RemoveCorners(Bitmap normalBitmap, Bitmap redBitmap, Color cornerColor) {
			using (BitmapBuffer redBuffer = new BitmapBuffer(redBitmap, false)) {
				redBuffer.Lock();
				using (BitmapBuffer normalBuffer = new BitmapBuffer(normalBitmap, false)) {
					normalBuffer.Lock();
					for (int y = 0; y < 8; y++) {
						for (int x = 0; x < 8; x++) {
							// top left
							int cornerX = x;
							int cornerY = y;
							Color currentPixel = redBuffer.GetColorAt(cornerX, cornerY);
							if (currentPixel.R > 0 && currentPixel.G == 0 && currentPixel.B == 0) {
								normalBuffer.SetColorAt(cornerX, cornerY, cornerColor);
							}
							// top right
							cornerX = normalBitmap.Width - x;
							cornerY = y;
							currentPixel = redBuffer.GetColorAt(cornerX, cornerY);
							if (currentPixel.R > 0 && currentPixel.G == 0 && currentPixel.B == 0) {
								normalBuffer.SetColorAt(cornerX, cornerY, cornerColor);
							}
							// bottom right
							cornerX = normalBitmap.Width - x;
							cornerY = normalBitmap.Height - y;
							currentPixel = redBuffer.GetColorAt(cornerX, cornerY);
							if (currentPixel.R > 0 && currentPixel.G == 0 && currentPixel.B == 0) {
								normalBuffer.SetColorAt(cornerX, cornerY, cornerColor);
							}
							// bottom left
							cornerX = x;
							cornerY = normalBitmap.Height - y;
							currentPixel = redBuffer.GetColorAt(cornerX, cornerY);
							if (currentPixel.R > 0 && currentPixel.G == 0 && currentPixel.B == 0) {
								normalBuffer.SetColorAt(cornerX, cornerY, cornerColor);
							}
						}
					}
				}
			}
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
				rectangle = windowInfo.rcClient.ToRectangle();
			} else {
				rectangle = Rectangle.Empty;
			}
			return result;
		}
		
		/// <summary>
		/// Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>		
		private bool GetWindowRect(out Rectangle rectangle) {
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
			Region returnRegion = null;
			if (result != RegionResult.REGION_ERROR && result != RegionResult.REGION_NULLREGION) {
				returnRegion = Region.FromHrgn(windowRegionPtr);
			}
			// Free the region object
			GDI32.DeleteObject(windowRegionPtr);
			return returnRegion;
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

//			WindowStyleFlags style = WindowStyle;
//			LOG.InfoFormat("before: {0}", style);
//			style = style.Remove(WindowStyleFlags.WS_HSCROLL);
//			WindowStyle = style;
//			if (!User32.SetWindowPos(Handle, IntPtr.Zero, 0,0,0,0, WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOZORDER | WindowPos.SWP_FRAMECHANGED| WindowPos.SWP_DRAWFRAME | WindowPos.SWP_NOACTIVATE)) {
//				Exception e = new Win32Exception();
//				LOG.Error("error with SetWindowPos", e);
//			}
//			style = WindowStyle;
//			LOG.InfoFormat("before: {0}", style);
//			style = style.Remove(WindowStyleFlags.WS_VSCROLL);
//			WindowStyle = style;
//			LOG.InfoFormat("After: {0}", style);
//			if (!User32.SetWindowPos(Handle, IntPtr.Zero, 0,0,0,0, WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOZORDER | WindowPos.SWP_FRAMECHANGED| WindowPos.SWP_DRAWFRAME | WindowPos.SWP_NOACTIVATE)) {
//				Exception e = new Win32Exception();
//				LOG.Error("error with SetWindowPos", e);
//			}

			Rectangle viewableRectangle = ClientRectangle;
			// As we work with bitmap coordinates, recalculate the location
			viewableRectangle.Offset(-WindowRectangle.X, -WindowRectangle.Y);

			int width = viewableRectangle.Width;
			int height = viewableRectangle.Height * (scrollinfo.nMax / scrollinfo.nPage);
			int pageSkip = scrollinfo.nPage;
			if (width * height == 0) {
				LOG.Error("No window content!!");
				return null;
			}
			Bitmap returnBitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);

			using (Graphics graphicsTarget = Graphics.FromImage(returnBitmap)) {
				int location = 0;
				int y = 0;
				do {
					VScroll(location);
					Thread.Sleep(200);

					Rectangle destination = new Rectangle(0, y, viewableRectangle.Width, viewableRectangle.Height);
					using (Image printedWindow = PrintWindow()) {
						if (printedWindow != null) {
							graphicsTarget.DrawImage(printedWindow, destination, viewableRectangle, GraphicsUnit.Pixel);
							graphicsTarget.Flush();
							y += viewableRectangle.Height;
						}
					}
					location += pageSkip;
					//if (location < scrollinfo.nMax && location > scrollinfo.nMax - scrollinfo.nPage) {
					//	location = scrollinfo.nMax - scrollinfo.nPage;
					//}
				} while (location < scrollinfo.nMax);
			}
			VScroll(scrollinfo.nPos);
			return returnBitmap;
		}

		/// <summary>
		/// Does the window have a HScrollbar?
		/// </summary>
		/// <returns>true if a HScrollbar is available</returns>
		public bool HasHScroll() {
			return WindowStyle.Has(WindowStyleFlags.WS_HSCROLL);
		}		

		/// <summary>
		/// Does the window have a VScrollbar?
		/// </summary>
		/// <returns>true if a VScrollbar is available</returns>
		public bool HasVScroll() {
			return WindowStyle.Has(WindowStyleFlags.WS_VSCROLL);
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
			int ptrWparam = (int)ScrollbarCommand.SB_THUMBTRACK + (vpos << 16);
			int ptrLparam = 0;
			User32.SendMessage(Handle, (int)WindowsMessages.WM_VSCROLL, ptrWparam, ptrLparam);
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
			// TODO: Check Outlook, Office etc?
			if (proc.ProcessName.ToLower().Contains("outlook")) {
				LOG.DebugFormat("Not freezing outlook due to Destinations, process was: {0}", proc.ProcessName);
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
			Rectangle windowRect = Rectangle.Empty;
			GetWindowRect(out windowRect);
			
			// Start the capture
			Exception exceptionOccured = null;
			Image returnImage = null;
			using (Region region = GetRegion()) {
				PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
				// Only use 32 bpp ARGB when the window has a region
				if (region != null) {
					pixelFormat = PixelFormat.Format32bppArgb;
				}
				returnImage = new Bitmap(windowRect.Width, windowRect.Height, pixelFormat);
				using (Graphics graphics = Graphics.FromImage(returnImage)) {
					IntPtr hDCDest = graphics.GetHdc();
					try {
						bool printSucceeded = User32.PrintWindow(Handle, hDCDest, 0x0);
						if (!printSucceeded) {
							// something went wrong, most likely a "0x80004005" (Acess Denied) when using UAC
							exceptionOccured = User32.CreateWin32Exception("PrintWindow");
						}
					} finally {
						graphics.ReleaseHdc(hDCDest);
					}
	
					// Apply the region "transparency"
					if (region != null && !region.IsEmpty(graphics)) {
						graphics.ExcludeClip(region);
						graphics.Clear(Color.Transparent);
					}
	
					graphics.Flush();
				}
			}

			// Return null if error
			if (exceptionOccured != null) {
				LOG.ErrorFormat("Error calling print window: {0}", exceptionOccured.Message);
				if (returnImage != null) {
					returnImage.Dispose();
				}
				return null;
			}
			if (!HasParent && this.Maximised) {
				LOG.Debug("Correcting for maximalization");
				Size borderSize = Size.Empty;
				GetBorderSize(out borderSize);
				Rectangle borderRectangle = new Rectangle(borderSize.Width, borderSize.Height, windowRect.Width - (2 * borderSize.Width), windowRect.Height - (2 * borderSize.Height));
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
				WindowDetails activeWindow = new WindowDetails(hWnd);
				// Invisible Windows should not be active
				if (!activeWindow.Visible || ignoreHandles.Contains(activeWindow.Handle)) {
					return WindowDetails.GetDesktopWindow();
				}
				return activeWindow;
			}
			return null;
		}
		
		/// <summary>
		/// Check if this window is Greenshot
		/// </summary>
		public bool IsGreenshot {
			get {
				try {
					return "Greenshot".Equals(Process.MainModule.FileVersionInfo.ProductName);
				} catch (Exception ex) {
					LOG.Warn(ex);
				}
				return false;
			}
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
			return new WindowsEnumerator().GetWindows(IntPtr.Zero, classname).Items;
		}

		/// <summary>
		/// Recursive "find children which"
		/// </summary>
		/// <param name="window">Window to look into</param>
		/// <param name="point">point to check for</param>
		/// <returns></returns>
		public WindowDetails FindChildUnderPoint(Point point) {
			if (!Contains(point)) {
				return null;
			}
			foreach(WindowDetails childWindow in Children) {
				if (childWindow.Contains(point)) {
					return childWindow.FindChildUnderPoint(point);
				}
			}
			return this;
		}
		
		/// <summary>
		/// Get all the visible top level windows
		/// </summary>
		/// <returns>List<WindowDetails> with all the visible top level windows</returns>
		public static List<WindowDetails> GetVisibleWindows() {
			List<WindowDetails> windows = new List<WindowDetails>();
			Rectangle screenBounds = WindowCapture.GetScreenBounds();
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
				// Ignore internal windows
				if (ignoreHandles.Contains(window.Handle)) {
					continue;
				}
				// Ignore some classes
				List<string> ignoreClasses = new List<string>(new string[] {"Progman", "XLMAIN", "Button"}); //"MS-SDIa"
				if (ignoreClasses.Contains(window.ClassName)) {
					continue;
				}
				// Windows without size
				Rectangle windowRect = window.WindowRectangle;
				windowRect.Intersect(screenBounds);
				if (windowRect.Size.IsEmpty) {
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
				Size windowSize = window.WindowRectangle.Size;
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
		/// Sort the list of WindowDetails according Z-Order, all not found "windows" come at the end
		/// </summary>
		/// <param name="hWndParent">IntPtr of the parent</param>
		/// <param name="windows">List of windows to sort</param>
		/// <returns></returns>
		public static List<WindowDetails> SortByZOrder(IntPtr hWndParent, List<WindowDetails> windows) {
			List<WindowDetails> sortedWindows = new List<WindowDetails>();
			Dictionary<IntPtr, WindowDetails> allWindows = new Dictionary<IntPtr, WindowDetails>();
			foreach(WindowDetails window in windows) {
				if (!allWindows.ContainsKey(window.Handle)) {
					allWindows.Add(window.Handle, window);
				}
			}
			// Sort by Z-Order, from top to bottom
			for(IntPtr hWnd = User32.GetTopWindow(hWndParent); hWnd!=IntPtr.Zero; hWnd = User32.GetWindow(hWnd, GetWindowCommand.GW_HWNDNEXT)) {
				if (allWindows.ContainsKey(hWnd)) {
					WindowDetails childWindow = allWindows[hWnd];
					// Force getting the clientRectangle, used for caching
					Rectangle rect = childWindow.WindowRectangle;
					sortedWindows.Add(childWindow);
					// Remove so we can add the ones that were left over
					allWindows.Remove(hWnd);
				}
			}
			// Add missing children, those that didn't show up in the GetWindow enumeration
			if (allWindows.Count > 0) {
				Rectangle screenBounds = WindowCapture.GetScreenBounds();
				// Copy not copied windows to the back, but only if visible
				foreach(IntPtr hWnd in allWindows.Keys) {
					WindowDetails notAddedChild = allWindows[hWnd];
					Rectangle windowRect = notAddedChild.WindowRectangle;
					if (windowRect.Width * windowRect.Height > 0 && screenBounds.Contains(windowRect)) {
						sortedWindows.Add(notAddedChild);
					} else {
						LOG.DebugFormat("Skipping {0}", notAddedChild.ClassName);
					}
				}
			}
			return sortedWindows;
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
