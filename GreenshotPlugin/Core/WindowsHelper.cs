/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Linq;
using Greenshot.Plugin;
using GreenshotPlugin.Interop;
using GreenshotPlugin.UnmanagedHelpers;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Greenshot.Interop;

namespace GreenshotPlugin.Core
{
	#region EnumWindows
	/// <summary>
	/// EnumWindows wrapper for .NET
	/// </summary>
	public class WindowsEnumerator
	{
		#region Member Variables
		private IList<WindowDetails> _items;
		#endregion

		/// <summary>
		/// Returns the collection of windows returned by
		/// GetWindows
		/// </summary>
		public IList<WindowDetails> Items
		{
			get
			{
				return _items;
			}
		}

		/// <summary>
		/// Gets all top level windows on the system.
		/// </summary>
		public WindowsEnumerator GetWindows()
		{
			GetWindows(IntPtr.Zero, null);
			return this;
		}

		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="parent">Window Handle to get children for</param>
		public WindowsEnumerator GetWindows(WindowDetails parent)
		{
			if (parent != null)
			{
				GetWindows(parent.Handle, null);
			}
			else
			{
				GetWindows(IntPtr.Zero, null);
			}
			return this;
		}

		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="hWndParent">Window Handle to get children for</param>
		/// <param name="classname">Window Classname to copy, use null to copy all</param>
		public WindowsEnumerator GetWindows(IntPtr hWndParent, string classname)
		{
			_items = new List<WindowDetails>();
			var windows = new List<WindowDetails>();
			User32.EnumChildWindows(hWndParent, WindowEnum, IntPtr.Zero);

			bool hasParent = !IntPtr.Zero.Equals(hWndParent);
			string parentText = null;
			if (hasParent)
			{
				var title = new StringBuilder(260, 260);
				User32.GetWindowText(hWndParent, title, title.Capacity);
				parentText = title.ToString();
			}

			foreach (var window in _items)
			{
				if (hasParent)
				{
					window.Text = parentText;
					window.ParentHandle = hWndParent;
				}
				if (classname == null || window.ClassName.Equals(classname))
				{
					windows.Add(window);
				}
			}
			_items = windows;
			return this;
		}

		#region EnumWindows callback
		/// <summary>
		/// The enum Windows callback.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <param name="lParam">Application defined value</param>
		/// <returns>1 to continue enumeration, 0 to stop</returns>
		private int WindowEnum(IntPtr hWnd, int lParam)
		{
			if (OnWindowEnum(hWnd))
			{
				return 1;
			}
			return 0;
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
		protected virtual bool OnWindowEnum(IntPtr hWnd)
		{
			if (!WindowDetails.IsIgnoreHandle(hWnd))
			{
				_items.Add(new WindowDetails(hWnd));
			}
			return true;
		}

		#region Constructor, Dispose
		public WindowsEnumerator()
		{
			// nothing to do
		}
		#endregion
	}
	#endregion EnumWindows

	/// <summary>
	#region WindowDetails
	/// <summary>
	/// Code for handling with "windows"
	/// Main code is taken from vbAccelerator, location:
	/// http://www.vbaccelerator.com/home/NET/Code/Libraries/Windows/Enumerating_Windows/article.asp
	/// but a LOT of changes/enhancements were made to adapt it for Greenshot.
	/// </summary>
	public class WindowDetails : IEquatable<WindowDetails>
	{
		private const string METRO_WINDOWS_CLASS = "Windows.UI.Core.CoreWindow";
		private const string METRO_APPLAUNCHER_CLASS = "ImmersiveLauncher";
		private const string METRO_GUTTER_CLASS = "ImmersiveGutter";

		private static ILog LOG = LogManager.GetLogger(typeof(WindowDetails));
		private static readonly ICoreConfiguration Conf = IniConfig.Get("Greenshot","greenshot").Get<ICoreConfiguration>();
		private static readonly List<IntPtr> _ignoreHandles = new List<IntPtr>();
		private static readonly List<string> _excludeProcessesFromFreeze = new List<string>();
		private static readonly IDisposableCom<IAppVisibility> _appVisibility;

		static WindowDetails()
		{
			try
			{
				// Only try to instantiate when Windows 8 or later.
				if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
				{
					Type appVisibilityType = Type.GetTypeFromCLSID(new Guid("7E5FE3D9-985F-4908-91F9-EE19F9FD1514"));
					_appVisibility = DisposableCom.Create((IAppVisibility)Activator.CreateInstance(appVisibilityType));
				}
			}
			catch { }
		}

		public static void AddProcessToExcludeFromFreeze(string processname)
		{
			if (!_excludeProcessesFromFreeze.Contains(processname))
			{
				_excludeProcessesFromFreeze.Add(processname);
			}
		}

		internal static bool IsIgnoreHandle(IntPtr handle)
		{
			return _ignoreHandles.Contains(handle);
		}

		private List<WindowDetails> _childWindows = null;
		private IntPtr _parentHandle = IntPtr.Zero;
		private WindowDetails _parent = null;
		private bool _frozen = false;

		public bool IsApp
		{
			get
			{
				return METRO_WINDOWS_CLASS.Equals(ClassName);
			}
		}

		public bool IsGutter
		{
			get
			{
				return METRO_GUTTER_CLASS.Equals(ClassName);
			}
		}

		public bool IsAppLauncher
		{
			get
			{
				return METRO_APPLAUNCHER_CLASS.Equals(ClassName);
			}
		}

		/// <summary>
		/// Check if this window is the window of a metro app
		/// </summary>
		public bool IsMetroApp
		{
			get
			{
				return IsAppLauncher || IsApp;
			}
		}

		/// <summary>
		/// The window handle.
		/// </summary>
		private IntPtr _hWnd = IntPtr.Zero;

		/// <summary>
		/// To allow items to be compared, the hash code
		/// is set to the Window handle, so two EnumWindowsItem
		/// objects for the same Window will be equal.
		/// </summary>
		/// <returns>The Window Handle for this window</returns>
		public override int GetHashCode()
		{
			return Handle.ToInt32();
		}

		public override bool Equals(object right)
		{
			return Equals(right as WindowDetails);
		}

		public bool Equals(WindowDetails other)
		{
			if (ReferenceEquals(other, null))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (GetType() != other.GetType())
			{
				return false;
			}
			return other.Handle == Handle;
		}

		public bool HasChildren
		{
			get
			{
				return (_childWindows != null) && (_childWindows.Count > 0);
			}
		}

		public void FreezeDetails()
		{
			_frozen = true;
		}
		public void UnfreezeDetails()
		{
			_frozen = false;
		}

		public string ProcessPath
		{
			get
			{
				if (Handle == IntPtr.Zero)
				{
					// not a valid window handle
					return string.Empty;
				}
				// Get the process id
				int processid;
				User32.GetWindowThreadProcessId(Handle, out processid);
				return Kernel32.GetProcessPath(processid);
			}
		}


		/// <summary>
		/// Get the icon belonging to the process
		/// </summary>
		public Image DisplayIcon
		{
			get
			{
				try
				{
					using (Icon appIcon = GetAppIcon(Handle))
					{
						if (appIcon != null)
						{
							return appIcon.ToBitmap();
						}
					}
				}
				catch (Exception ex)
				{
					LOG.WarnFormat("Couldn't get icon for window {0} due to: {1}", Text, ex.Message);
					LOG.Warn(ex);
				}
				if (IsMetroApp)
				{
					// No method yet to get the metro icon
					return null;
				}
				try
				{
					return PluginUtils.GetCachedExeIcon(ProcessPath, 0);
				}
				catch (Exception ex)
				{
					LOG.WarnFormat("Couldn't get icon for window {0} due to: {1}", Text, ex.Message);
					LOG.Warn(ex);
				}
				return null;
			}
		}

		/// <summary>
		/// Get the icon for a hWnd
		/// </summary>
		/// <param name="hwnd"></param>
		/// <returns></returns>
		private static Icon GetAppIcon(IntPtr hwnd)
		{
			IntPtr ICON_SMALL = IntPtr.Zero;
			IntPtr ICON_BIG = new IntPtr(1);
			IntPtr ICON_SMALL2 = new IntPtr(2);

			IntPtr iconHandle = User32.SendMessage(hwnd, (int)WindowsMessages.WM_GETICON, ICON_BIG, IntPtr.Zero);
			if (CoreConfigurationChecker.UseLargeIcons(Conf.IconSize))
			{
				iconHandle = User32.SendMessage(hwnd, (int)WindowsMessages.WM_GETICON, ICON_BIG, IntPtr.Zero);
				if (iconHandle == IntPtr.Zero)
				{
					iconHandle = User32.GetClassLongWrapper(hwnd, (int)ClassLongIndex.GCL_HICON);
				}
			}
			else
			{
				iconHandle = User32.SendMessage(hwnd, (int)WindowsMessages.WM_GETICON, ICON_SMALL2, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = User32.SendMessage(hwnd, (int)WindowsMessages.WM_GETICON, ICON_SMALL, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = User32.GetClassLongWrapper(hwnd, (int)ClassLongIndex.GCL_HICONSM);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = User32.SendMessage(hwnd, (int)WindowsMessages.WM_GETICON, ICON_BIG, IntPtr.Zero);
			}
			if (iconHandle == IntPtr.Zero)
			{
				iconHandle = User32.GetClassLongWrapper(hwnd, (int)ClassLongIndex.GCL_HICON);
			}

			if (iconHandle == IntPtr.Zero)
			{
				return null;
			}

			Icon icon = Icon.FromHandle(iconHandle);

			return icon;
		}

		/// <summary>
		/// Use this to make remove internal windows, like the mainform and the captureforms, invisible
		/// </summary>
		/// <param name="ignoreHandle"></param>
		public static void RegisterIgnoreHandle(IntPtr ignoreHandle)
		{
			_ignoreHandles.Add(ignoreHandle);
		}

		/// <summary>
		/// Use this to remove the with RegisterIgnoreHandle registered handle
		/// </summary>
		/// <param name="ignoreHandle"></param>
		public static void UnregisterIgnoreHandle(IntPtr ignoreHandle)
		{
			_ignoreHandles.Remove(ignoreHandle);
		}

		public List<WindowDetails> Children
		{
			get
			{
				if (_childWindows == null)
				{
					GetChildren();
				}
				return _childWindows;
			}
		}

		/// <summary>
		/// Retrieve all windows with a certain title or classname
		/// </summary>
		/// <param name="titlePattern">The regexp to look for in the title</param>
		/// <param name="classnamePattern">The regexp to look for in the classname</param>
		/// <returns>List<WindowDetails> with all the found windows</returns>
		private static List<WindowDetails> FindWindow(List<WindowDetails> windows, string titlePattern, string classnamePattern)
		{
			List<WindowDetails> foundWindows = new List<WindowDetails>();
			Regex titleRegexp = null;
			Regex classnameRegexp = null;

			if (titlePattern != null && titlePattern.Trim().Length > 0)
			{
				titleRegexp = new Regex(titlePattern);
			}
			if (classnamePattern != null && classnamePattern.Trim().Length > 0)
			{
				classnameRegexp = new Regex(classnamePattern);
			}

			foreach (WindowDetails window in windows)
			{
				if (titleRegexp != null && titleRegexp.IsMatch(window.Text))
				{
					foundWindows.Add(window);
				}
				else if (classnameRegexp != null && classnameRegexp.IsMatch(window.ClassName))
				{
					foundWindows.Add(window);
				}
			}
			return foundWindows;
		}

		/// <summary>
		/// Retrieve the child with matching classname
		/// </summary>
		public WindowDetails GetChild(string childClassname)
		{
			foreach (WindowDetails child in Children)
			{
				if (childClassname.Equals(child.ClassName))
				{
					return child;
				}
			}
			return null;
		}

		/// <summary>
		/// Retrieve the children with matching classname
		/// </summary>
		public IEnumerable<WindowDetails> GetChilden(string childClassname)
		{
			foreach (WindowDetails child in Children)
			{
				if (childClassname.Equals(child.ClassName))
				{
					yield return child;
				}
			}
		}

		public IntPtr ParentHandle
		{
			get
			{
				if (_parentHandle == IntPtr.Zero)
				{
					_parentHandle = User32.GetParent(Handle);
					_parent = null;
				}
				return _parentHandle;
			}
			set
			{
				if (_parentHandle != value)
				{
					_parentHandle = value;
					_parent = null;
				}
			}
		}
		/// <summary>
		/// Get the parent of the current window
		/// </summary>
		/// <returns>WindowDetails of the parent, or null if none</returns>
		public WindowDetails GetParent()
		{
			if (_parent == null)
			{
				if (_parentHandle == IntPtr.Zero)
				{
					_parentHandle = User32.GetParent(Handle);
				}
				if (_parentHandle != IntPtr.Zero)
				{
					_parent = new WindowDetails(_parentHandle);
				}
			}
			return _parent;
		}

		/// <summary>
		/// Retrieve all the children, this only stores the children internally.
		/// One should normally use the getter "Children"
		/// </summary>
		public List<WindowDetails> GetChildren()
		{
			if (_childWindows == null)
			{
				return GetChildren(0);
			}
			return _childWindows;
		}

		/// <summary>
		/// Retrieve all the children, this only stores the children internally, use the "Children" property for the value
		/// </summary>
		/// <param name="levelsToGo">Specify how many levels we go in</param>
		public List<WindowDetails> GetChildren(int levelsToGo)
		{
			if (_childWindows == null)
			{
				_childWindows = new List<WindowDetails>();
				foreach (var childWindow in new WindowsEnumerator().GetWindows(_hWnd, null).Items)
				{
					_childWindows.Add(childWindow);
					if (levelsToGo > 0)
					{
						childWindow.GetChildren(levelsToGo - 1);
					}
				}
			}
			return _childWindows;
		}

		/// <summary>
		/// Retrieve children with a certain title or classname
		/// </summary>
		/// <param name="titlePattern">The regexp to look for in the title</param>
		/// <param name="classnamePattern">The regexp to look for in the classname</param>
		/// <returns>List<WindowDetails> with all the found windows, or an empty list</returns>
		public List<WindowDetails> FindChildren(string titlePattern, string classnamePattern)
		{
			return FindWindow(Children, titlePattern, classnamePattern);
		}

		/// <summary>
		/// Recursing helper method for the FindPath
		/// </summary>
		/// <param name="classnames">List<string> with classnames</param>
		/// <param name="index">The index in the list to look for</param>
		/// <returns>WindowDetails if a match was found</returns>
		private WindowDetails FindPath(List<string> classnames, int index)
		{
			WindowDetails resultWindow = null;
			List<WindowDetails> foundWindows = FindChildren(null, classnames[index]);
			if (index == classnames.Count - 1)
			{
				if (foundWindows.Count > 0)
				{
					resultWindow = foundWindows[0];
				}
			}
			else
			{
				foreach (WindowDetails foundWindow in foundWindows)
				{
					resultWindow = foundWindow.FindPath(classnames, index + 1);
					if (resultWindow != null)
					{
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
		public WindowDetails FindPath(List<string> classnames, bool allowSkip)
		{
			int index = 0;
			WindowDetails resultWindow = FindPath(classnames, index++);
			if (resultWindow == null && allowSkip)
			{
				while (resultWindow == null && index < classnames.Count)
				{
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
		public static WindowDetails DeepScan(WindowDetails windowDetails, Regex classnamePattern)
		{
			if (classnamePattern.IsMatch(windowDetails.ClassName))
			{
				return windowDetails;
			}
			// First loop through this level
			foreach (WindowDetails child in windowDetails.Children)
			{
				if (classnamePattern.IsMatch(child.ClassName))
				{
					return child;
				}
			}
			// Go into all children
			foreach (WindowDetails child in windowDetails.Children)
			{
				WindowDetails deepWindow = DeepScan(child, classnamePattern);
				if (deepWindow != null)
				{
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
		public WindowDetails GetWindow(GetWindowCommand gwCommand)
		{
			IntPtr tmphWnd = User32.GetWindow(Handle, gwCommand);
			if (IntPtr.Zero == tmphWnd)
			{
				return null;
			}
			WindowDetails windowDetails = new WindowDetails(tmphWnd);
			windowDetails._parent = this;
			return windowDetails;
		}

		/// <summary>
		/// Gets the window's handle
		/// </summary>
		public IntPtr Handle
		{
			get
			{
				return _hWnd;
			}
		}

		private string _text = null;
		/// <summary>
		/// Gets the window's title (caption)
		/// </summary>
		public string Text
		{
			set
			{
				_text = value;
			}
			get
			{
				if (_text == null)
				{
					StringBuilder title = new StringBuilder(260, 260);
					User32.GetWindowText(_hWnd, title, title.Capacity);
					_text = title.ToString();
				}
				return _text;
			}
		}

		private string _className = null;
		/// <summary>
		/// Gets the window's class name.
		/// Some information on classes can be found here: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633574%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
		/// </summary>
		public string ClassName
		{
			get
			{
				if (_className == null)
				{
					_className = GetClassName(_hWnd);
				}
				return _className;
			}
		}

		/// <summary>
		/// Gets/Sets whether the window is iconic (mimimized) or not.
		/// </summary>
		public bool Iconic
		{
			get
			{
				if (IsMetroApp)
				{
					return !Visible;
				}
				return User32.IsIconic(_hWnd) || Location.X <= -32000;
			}
			set
			{
				if (value)
				{
					User32.SendMessage(_hWnd, (int)WindowsMessages.WM_SYSCOMMAND, (IntPtr)User32.SC_MINIMIZE, IntPtr.Zero);
				}
				else
				{
					User32.SendMessage(_hWnd, (int)WindowsMessages.WM_SYSCOMMAND, (IntPtr)User32.SC_RESTORE, IntPtr.Zero);
				}
			}
		}

		/// <summary>
		/// Gets/Sets whether the window is maximised or not.
		/// </summary>
		public bool Maximised
		{
			get
			{
				if (IsApp)
				{
					if (Visible)
					{
						Rectangle windowRectangle = WindowRectangle;
						foreach (Screen screen in Screen.AllScreens)
						{
							if (screen.Bounds.Contains(windowRectangle))
							{
								if (windowRectangle.Equals(screen.Bounds))
								{
									return true;
								}
							}
						}
					}
					return false;
				}
				return User32.IsZoomed(_hWnd);
			}
			set
			{
				if (value)
				{
					User32.SendMessage(_hWnd, (int)WindowsMessages.WM_SYSCOMMAND, (IntPtr)User32.SC_MAXIMIZE, IntPtr.Zero);
				}
				else
				{
					User32.SendMessage(_hWnd, (int)WindowsMessages.WM_SYSCOMMAND, (IntPtr)User32.SC_MINIMIZE, IntPtr.Zero);
				}
			}
		}

		/// <summary>
		/// This doesn't work as good as is should, but does move the App out of the way...
		/// </summary>
		public void HideApp()
		{
			User32.ShowWindow(Handle, ShowWindowCommand.Hide);
		}

		/// <summary>
		/// Gets whether the window is visible.
		/// </summary>
		public bool Visible
		{
			get
			{
				if (IsApp)
				{
					Rectangle windowRectangle = WindowRectangle;
					foreach (Screen screen in Screen.AllScreens)
					{
						if (screen.Bounds.Contains(windowRectangle))
						{
							if (windowRectangle.Equals(screen.Bounds))
							{
								// Fullscreen, it's "visible" when AppVisibilityOnMonitor says yes
								// Although it might be the other App, this is not "very" important
								RECT rect = new RECT(screen.Bounds);
								IntPtr monitor = User32.MonitorFromRect(ref rect, User32.MONITOR_DEFAULTTONULL);
								if (monitor != IntPtr.Zero)
								{
									if (_appVisibility != null)
									{
										MONITOR_APP_VISIBILITY monitorAppVisibility = _appVisibility.ComObject.GetAppVisibilityOnMonitor(monitor);
										//LOG.DebugFormat("App {0} visible: {1} on {2}", Text, monitorAppVisibility, screen.Bounds);
										if (monitorAppVisibility == MONITOR_APP_VISIBILITY.MAV_APP_VISIBLE)
										{
											return true;
										}
									}
								}
							}
							else
							{
								// Is only partly on the screen, when this happens the app is always visible!
								return true;
							}
						}
					}
					return false;
				}
				if (IsGutter)
				{
					// gutter is only made available when it's visible
					return true;
				}
				if (IsAppLauncher)
				{
					return IsAppLauncherVisible;
				}
				return User32.IsWindowVisible(_hWnd);
			}
		}

		public bool HasParent
		{
			get
			{
				GetParent();
				return _parentHandle != IntPtr.Zero;
			}
		}

		public int ProcessId
		{
			get
			{
				int processId;
				User32.GetWindowThreadProcessId(Handle, out processId);
				return processId;
			}
		}

		public Process Process
		{
			get
			{
				try
				{
					int processId;
					User32.GetWindowThreadProcessId(Handle, out processId);
					Process process = Process.GetProcessById(processId);
					if (process != null)
					{
						return process;
					}
				}
				catch (Exception ex)
				{
					LOG.Warn(ex);
				}
				return null;
			}
		}

		/// <summary>
		/// Make sure the next call of a cached value is guaranteed the real value
		/// </summary>
		public void Reset()
		{
			_previousWindowRectangle = Rectangle.Empty;
		}

		private Rectangle _previousWindowRectangle = Rectangle.Empty;
		private long _lastWindowRectangleRetrieveTime = 0;
		private const long CACHE_TIME = TimeSpan.TicksPerSecond * 2;
		/// <summary>
		/// Gets the bounding rectangle of the window
		/// </summary>
		public Rectangle WindowRectangle
		{
			get
			{
				// Try to return a cached value
				long now = DateTime.Now.Ticks;
				if (_previousWindowRectangle.IsEmpty || !_frozen)
				{
					if (_previousWindowRectangle.IsEmpty || now - _lastWindowRectangleRetrieveTime > CACHE_TIME)
					{
						Rectangle windowRect = Rectangle.Empty;
						if (DWM.isDWMEnabled())
						{
							GetExtendedFrameBounds(out windowRect);
						}

						if (windowRect.IsEmpty)
						{
							GetWindowRect(out windowRect);
						}

						// Correction for maximized windows, only if it's not an app
						if (!HasParent && !IsApp && Maximised)
						{
							Size size = Size.Empty;
							GetBorderSize(out size);
							windowRect = new Rectangle(windowRect.X + size.Width, windowRect.Y + size.Height, windowRect.Width - (2 * size.Width), windowRect.Height - (2 * size.Height));
						}
						_lastWindowRectangleRetrieveTime = now;
						// Try to return something valid, by getting returning the previous size if the window doesn't have a Rectangle anymore
						if (windowRect.IsEmpty)
						{
							return _previousWindowRectangle;
						}
						_previousWindowRectangle = windowRect;
						return windowRect;
					}
				}
				return _previousWindowRectangle;
			}
		}

		/// <summary>
		/// Gets/set the location of the window relative to the screen.
		/// </summary>
		public Point Location
		{
			get
			{
				Rectangle tmpRectangle = WindowRectangle;
				return new Point(tmpRectangle.Left, tmpRectangle.Top);
			}
			set
			{
				User32.SetWindowPos(Handle, IntPtr.Zero, value.X, value.Y, 0, 0, WindowPos.SWP_SHOWWINDOW | WindowPos.SWP_NOSIZE);
			}
		}

		/// <summary>
		/// Gets the size of the window.
		/// </summary>
		public Size Size
		{
			get
			{
				Rectangle tmpRectangle = WindowRectangle;
				return new Size(tmpRectangle.Right - tmpRectangle.Left, tmpRectangle.Bottom - tmpRectangle.Top);
			}
		}

		/// <summary>
		/// Get the client rectangle, this is the part of the window inside the borders (drawable area)
		/// </summary>
		public Rectangle ClientRectangle
		{
			get
			{
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
		public bool Contains(Point p)
		{
			return WindowRectangle.Contains(Cursor.Position);
		}

		/// <summary>
		/// Restores and Brings the window to the front, 
		/// assuming it is a visible application window.
		/// </summary>
		public void Restore()
		{
			if (Iconic)
			{
				User32.SendMessage(_hWnd, (int)WindowsMessages.WM_SYSCOMMAND, (IntPtr)User32.SC_RESTORE, IntPtr.Zero);
			}
			User32.BringWindowToTop(_hWnd);
			User32.SetForegroundWindow(_hWnd);
			// Make sure windows has time to perform the action
			while (Iconic)
			{
				Application.DoEvents();
			}
		}

		/// <summary>
		/// Get / Set the WindowStyle
		/// </summary>
		public WindowStyleFlags WindowStyle
		{
			get
			{
				return (WindowStyleFlags)User32.GetWindowLongWrapper(_hWnd, (int)WindowLongIndex.GWL_STYLE);
			}
			set
			{
				User32.SetWindowLongWrapper(_hWnd, (int)WindowLongIndex.GWL_STYLE, new IntPtr((long)value));
			}
		}

		/// <summary>
		/// Get/Set the WindowPlacement
		/// </summary>
		public WindowPlacement WindowPlacement
		{
			get
			{
				WindowPlacement placement = WindowPlacement.Default;
				User32.GetWindowPlacement(Handle, ref placement);
				return placement;
			}
			set
			{
				User32.SetWindowPlacement(Handle, ref value);
			}
		}

		/// <summary>
		/// Get/Set the Extended WindowStyle
		/// </summary>
		public ExtendedWindowStyleFlags ExtendedWindowStyle
		{
			get
			{
				return (ExtendedWindowStyleFlags)User32.GetWindowLongWrapper(_hWnd, (int)WindowLongIndex.GWL_EXSTYLE);
			}
			set
			{
				User32.SetWindowLongWrapper(_hWnd, (int)WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)value));
			}
		}

		/// <summary>
		/// Capture Window with GDI+
		/// </summary>
		/// <param name="capture">The capture to fill</param>
		/// <returns>ICapture</returns>
		public ICapture CaptureGDIWindow(ICapture capture)
		{
			Image capturedImage = PrintWindow();
			if (capturedImage != null)
			{
				capture.Image = capturedImage;
				capture.Location = Location;
				return capture;
			}
			return null;
		}

		/// <summary>
		/// Simple screen capture for the Window
		/// </summary>
		/// <returns></returns>
		public Bitmap CaptureFromScreen()
		{
			return WindowCapture.CaptureRectangle(WindowRectangle);
		}

		/// <summary>
		/// Capture DWM Window
		/// </summary>
		/// <param name="capture">Capture to fill</param>
		/// <param name="windowCaptureMode">Wanted WindowCaptureMode</param>
		/// <param name="autoMode">True if auto modus is used</param>
		/// <returns>ICapture with the capture</returns>
		public ICapture CaptureDWMWindow(ICapture capture, WindowCaptureMode windowCaptureMode, bool autoMode)
		{
			capture.Image = DwmCapture(windowCaptureMode, autoMode, capture.ScreenBounds);
			// Make sure the capture location is the location of the window, not the copy
			capture.Location = Location;

			return capture;
		}

		/// <summary>
		/// Capture DWM Window
		/// </summary>
		/// <param name="capture">Capture to fill</param>
		/// <param name="windowCaptureMode">Wanted WindowCaptureMode</param>
		/// <param name="autoMode">True if auto modus is used</param>
		/// <returns>ICapture with the capture</returns>
		public Bitmap DwmCapture(WindowCaptureMode windowCaptureMode, bool autoMode, Rectangle screenbounds)
		{
			IntPtr thumbnailHandle = IntPtr.Zero;
			Form tempForm = null;
			bool tempFormShown = false;
			try
			{
				tempForm = new Form();
				tempForm.ShowInTaskbar = false;
				tempForm.FormBorderStyle = FormBorderStyle.None;
				tempForm.TopMost = true;

				// Register the Thumbnail
				DWM.DwmRegisterThumbnail(tempForm.Handle, Handle, out thumbnailHandle);

				// Get the original size
				SIZE sourceSize;
				DWM.DwmQueryThumbnailSourceSize(thumbnailHandle, out sourceSize);

				if (sourceSize.width <= 0 || sourceSize.height <= 0)
				{
					return null;
				}

				// Calculate the location of the temp form
				Rectangle windowRectangle = WindowRectangle;
				// Assume using it's own location
				Point formLocation = windowRectangle.Location;
				Size borderSize = new Size();
				bool doesCaptureFit = false;
				if (!Maximised)
				{
					doesCaptureFit = GetVisibleLocation(out formLocation);
				}
				else
				{
					//GetClientRect(out windowRectangle);
					GetBorderSize(out borderSize);
					formLocation = new Point(windowRectangle.X - borderSize.Width, windowRectangle.Y - borderSize.Height);
				}

				tempForm.Location = formLocation;
				tempForm.Size = sourceSize.ToSize();

				// Prepare rectangle to capture from the screen.
				Rectangle captureRectangle = new Rectangle(formLocation.X, formLocation.Y, sourceSize.width, sourceSize.height);
				if (Maximised)
				{
					// Correct capture size for maximized window by offsetting the X,Y with the border size
					captureRectangle.X += borderSize.Width;
					captureRectangle.Y += borderSize.Height;
					// and subtracting the border from the size (2 times, as we move right/down for the capture without resizing)
					captureRectangle.Width -= 2 * borderSize.Width;
					captureRectangle.Height -= 2 * borderSize.Height;
				}
				else if (autoMode)
				{
					// check if the capture fits
					if (!doesCaptureFit)
					{
						// if GDI is allowed.. (a screenshot won't be better than we comes if we continue)
						using (Process thisWindowProcess = Process)
						{
							if (!IsMetroApp && WindowCapture.IsGdiAllowed(thisWindowProcess))
							{
								// we return null which causes the capturing code to try another method.
								return null;
							}
						}
					}
				}

				// Prepare the displaying of the Thumbnail
				DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
				props.Opacity = (byte)255;
				props.Visible = true;
				props.Destination = new RECT(0, 0, sourceSize.width, sourceSize.height);
				DWM.DwmUpdateThumbnailProperties(thumbnailHandle, ref props);
				tempForm.Show();
				tempFormShown = true;

				if (screenbounds != Rectangle.Empty)
				{
					// Intersect with screen
					captureRectangle.Intersect(screenbounds);
				}

				// Destination bitmap for the capture
				Bitmap capturedBitmap = null;
				bool frozen = false;
				try
				{
					// Check if we make a transparent capture
					if (windowCaptureMode == WindowCaptureMode.AeroTransparent)
					{
						frozen = FreezeWindow();
						// Use white, later black to capture transparent
						tempForm.BackColor = Color.White;
						// Make sure everything is visible
						tempForm.Refresh();
						Application.DoEvents();

						try
						{
							using (Bitmap whiteBitmap = WindowCapture.CaptureRectangle(captureRectangle))
							{
								// Apply a white color
								tempForm.BackColor = Color.Black;
								// Make sure everything is visible
								tempForm.Refresh();
								if (!IsMetroApp)
								{
									// Make sure the application window is active, so the colors & buttons are right
									ToForeground();
								}
								// Make sure all changes are processed and visible
								Application.DoEvents();
								using (Bitmap blackBitmap = WindowCapture.CaptureRectangle(captureRectangle))
								{
									capturedBitmap = ApplyTransparency(blackBitmap, whiteBitmap);
								}
							}
						}
						catch (Exception e)
						{
							LOG.Debug("Exception: ", e);
							// Some problem occurred, cleanup and make a normal capture
							if (capturedBitmap != null)
							{
								capturedBitmap.Dispose();
								capturedBitmap = null;
							}
						}
					}
					// If no capture up till now, create a normal capture.
					if (capturedBitmap == null)
					{
						// Remove transparency, this will break the capturing
						if (!autoMode)
						{
							tempForm.BackColor = Color.FromArgb(255, Conf.DWMBackgroundColor.R, Conf.DWMBackgroundColor.G, Conf.DWMBackgroundColor.B);
						}
						else
						{
							Color colorizationColor = DWM.ColorizationColor;
							// Modify by losing the transparency and increasing the intensity (as if the background color is white)
							colorizationColor = Color.FromArgb(255, (colorizationColor.R + 255) >> 1, (colorizationColor.G + 255) >> 1, (colorizationColor.B + 255) >> 1);
							tempForm.BackColor = colorizationColor;
						}
						// Make sure everything is visible
						tempForm.Refresh();
						if (!IsMetroApp)
						{
							// Make sure the application window is active, so the colors & buttons are right
							ToForeground();
						}
						// Make sure all changes are processed and visible
						Application.DoEvents();
						// Capture from the screen
						capturedBitmap = WindowCapture.CaptureRectangle(captureRectangle);
					}
					if (capturedBitmap != null)
					{
						// Not needed for Windows 8
						if (!(Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 2))
						{
							// Only if the Inivalue is set, not maximized and it's not a tool window.
							if (Conf.WindowCaptureRemoveCorners && !Maximised && (ExtendedWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) == 0)
							{
								// Remove corners
								if (!Image.IsAlphaPixelFormat(capturedBitmap.PixelFormat))
								{
									LOG.Debug("Changing pixelformat to Alpha for the RemoveCorners");
									Bitmap tmpBitmap = ImageHelper.Clone(capturedBitmap, PixelFormat.Format32bppArgb);
									capturedBitmap.Dispose();
									capturedBitmap = tmpBitmap;
								}
								RemoveCorners(capturedBitmap);
							}
						}
					}
				}
				finally
				{
					// Make sure to ALWAYS unfreeze!!
					if (frozen)
					{
						UnfreezeWindow();
					}
				}

				return capturedBitmap;
			}
			finally
			{
				if (thumbnailHandle != IntPtr.Zero)
				{
					// Unregister (cleanup), as we are finished we don't need the form or the thumbnail anymore
					DWM.DwmUnregisterThumbnail(thumbnailHandle);
				}
				if (tempForm != null)
				{
					if (tempFormShown)
					{
						tempForm.Close();
					}
					tempForm.Dispose();
					tempForm = null;
				}
			}
		}

		/// <summary>
		/// Get a location where this window would be visible
		/// * if none is found return false, formLocation = the original location
		/// * if something is found, return true and formLocation = new location
		/// </summary>
		public bool GetVisibleLocation(out Point formLocation)
		{
			bool doesCaptureFit = false;
			Rectangle windowRectangle = WindowRectangle;
			// assume own location
			formLocation = windowRectangle.Location;
			using (Region workingArea = new Region(Screen.PrimaryScreen.Bounds))
			{
				// Create a region with the screens working area
				foreach (Screen screen in Screen.AllScreens)
				{
					if (screen != Screen.PrimaryScreen)
					{
						workingArea.Union(screen.Bounds);
					}
				}

				// If the formLocation is not inside the visible area
				if (!workingArea.AreRectangleCornersVisisble(windowRectangle))
				{
					// If none found we find the biggest screen
					foreach (Screen screen in Screen.AllScreens)
					{
						Rectangle newWindowRectangle = new Rectangle(screen.WorkingArea.Location, windowRectangle.Size);
						if (workingArea.AreRectangleCornersVisisble(newWindowRectangle))
						{
							formLocation = screen.Bounds.Location;
							doesCaptureFit = true;
							break;
						}
					}
				}
				else
				{
					doesCaptureFit = true;
				}
			}
			return doesCaptureFit;
		}

		/// <summary>
		/// Helper method to remove the corners from a DMW capture
		/// </summary>
		/// <param name="image">The bitmap to remove the corners from.</param>
		private void RemoveCorners(Bitmap image)
		{
			using (IFastBitmap fastBitmap = FastBitmap.Create(image))
			{
				for (int y = 0; y < Conf.WindowCornerCutShape.Count; y++)
				{
					for (int x = 0; x < Conf.WindowCornerCutShape[y]; x++)
					{
						fastBitmap.SetColorAt(x, y, Color.Transparent);
						fastBitmap.SetColorAt(image.Width - 1 - x, y, Color.Transparent);
						fastBitmap.SetColorAt(image.Width - 1 - x, image.Height - 1 - y, Color.Transparent);
						fastBitmap.SetColorAt(x, image.Height - 1 - y, Color.Transparent);
					}
				}
			}
		}

		/// <summary>
		/// Apply transparency by comparing a transparent capture with a black and white background
		/// A "Math.min" makes sure there is no overflow, but this could cause the picture to have shifted colors.
		/// The pictures should have been taken without differency, except for the colors.
		/// </summary>
		/// <param name="blackBitmap">Bitmap with the black image</param>
		/// <param name="whiteBitmap">Bitmap with the black image</param>
		/// <returns>Bitmap with transparency</returns>
		private Bitmap ApplyTransparency(Bitmap blackBitmap, Bitmap whiteBitmap)
		{
			using (IFastBitmap targetBuffer = FastBitmap.CreateEmpty(blackBitmap.Size, PixelFormat.Format32bppArgb, Color.Transparent))
			{
				targetBuffer.SetResolution(blackBitmap.HorizontalResolution, blackBitmap.VerticalResolution);
				using (IFastBitmap blackBuffer = FastBitmap.Create(blackBitmap))
				{
					using (IFastBitmap whiteBuffer = FastBitmap.Create(whiteBitmap))
					{
						for (int y = 0; y < blackBuffer.Height; y++)
						{
							for (int x = 0; x < blackBuffer.Width; x++)
							{
								Color c0 = blackBuffer.GetColorAt(x, y);
								Color c1 = whiteBuffer.GetColorAt(x, y);
								// Calculate alpha as double in range 0-1
								double alpha = (c0.R - c1.R + 255) / 255d;
								if (alpha == 1)
								{
									// Alpha == 1 means no change!
									targetBuffer.SetColorAt(x, y, c0);
								}
								else if (alpha == 0)
								{
									// Complete transparency, use transparent pixel
									targetBuffer.SetColorAt(x, y, Color.Transparent);
								}
								else
								{
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
				return targetBuffer.UnlockAndReturnBitmap();
			}
		}

		/// <summary>
		/// Helper method to get the window size for DWM Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>
		private bool GetExtendedFrameBounds(out Rectangle rectangle)
		{
			RECT rect;
			int result = DWM.DwmGetWindowAttribute(Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
			if (result >= 0)
			{
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
		private bool GetClientRect(out Rectangle rectangle)
		{
			WindowInfo windowInfo = new WindowInfo();
			// Get the Window Info for this window
			bool result = User32.GetWindowInfo(Handle, ref windowInfo);
			if (result)
			{
				rectangle = windowInfo.rcClient.ToRectangle();
			}
			else
			{
				rectangle = Rectangle.Empty;
			}
			return result;
		}

		/// <summary>
		/// Helper method to get the window size for GDI Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>		
		private bool GetWindowRect(out Rectangle rectangle)
		{
			WindowInfo windowInfo = new WindowInfo();
			// Get the Window Info for this window
			bool result = User32.GetWindowInfo(Handle, ref windowInfo);
			if (result)
			{
				rectangle = windowInfo.rcWindow.ToRectangle();
			}
			else
			{
				rectangle = Rectangle.Empty;
			}
			return result;
		}

		/// <summary>
		/// Helper method to get the Border size for GDI Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>	
		private bool GetBorderSize(out Size size)
		{
			WindowInfo windowInfo = new WindowInfo();
			// Get the Window Info for this window
			bool result = User32.GetWindowInfo(Handle, ref windowInfo);
			if (result)
			{
				size = new Size((int)windowInfo.cxWindowBorders, (int)windowInfo.cyWindowBorders);
			}
			else
			{
				size = Size.Empty;
			}
			return result;
		}

		/// <summary>
		/// Set the window as foreground window
		/// </summary>
		public static void ToForeground(IntPtr handle)
		{
			User32.SetForegroundWindow(handle);
		}

		/// <summary>
		/// Set the window as foreground window
		/// </summary>
		public void ToForeground()
		{
			ToForeground(Handle);
		}

		/// <summary>
		/// Get the region for a window
		/// </summary>
		public Region GetRegion(out RegionResult regionResult)
		{
			regionResult = RegionResult.REGION_NULLREGION;
			using (SafeRegionHandle region = GDI32.CreateRectRgn(0, 0, 0, 0))
			{
				if (!region.IsInvalid)
				{
					regionResult = User32.GetWindowRgn(Handle, region);
					if (regionResult != RegionResult.REGION_ERROR && regionResult != RegionResult.REGION_NULLREGION)
					{
						return Region.FromHrgn(region.DangerousGetHandle());
					}
				}
			}
			return null;
		}

		private bool CanFreezeOrUnfreeze(string titleOrProcessname)
		{
			if (string.IsNullOrEmpty(titleOrProcessname))
			{
				return false;
			}
			if (titleOrProcessname.ToLower().Contains("greenshot"))
			{
				return false;
			}

			foreach (string excludeProcess in _excludeProcessesFromFreeze)
			{
				if (titleOrProcessname.ToLower().Contains(excludeProcess))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Freezes the process belonging to the window
		/// Warning: Use only if no other way!!
		/// </summary>
		private bool FreezeWindow()
		{
			bool frozen = false;
			using (Process proc = Process.GetProcessById(ProcessId))
			{
				string processName = proc.ProcessName;
				if (!CanFreezeOrUnfreeze(processName))
				{
					LOG.DebugFormat("Not freezing {0}", processName);
					return false;
				}
				if (!CanFreezeOrUnfreeze(Text))
				{
					LOG.DebugFormat("Not freezing {0}", processName);
					return false;
				}
				LOG.DebugFormat("Freezing process: {0}", processName);


				foreach (ProcessThread pT in proc.Threads)
				{
					IntPtr pOpenThread = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

					if (pOpenThread == IntPtr.Zero)
					{
						break;
					}
					frozen = true;
					Kernel32.SuspendThread(pOpenThread);
					pT.Dispose();
				}
			}
			return frozen;
		}

		/// <summary>
		/// Unfreeze the process belonging to the window
		/// </summary>
		public void UnfreezeWindow()
		{
			using (Process proc = Process.GetProcessById(ProcessId))
			{
				string processName = proc.ProcessName;
				if (!CanFreezeOrUnfreeze(processName))
				{
					LOG.DebugFormat("Not unfreezing {0}", processName);
					return;
				}
				if (!CanFreezeOrUnfreeze(Text))
				{
					LOG.DebugFormat("Not unfreezing {0}", processName);
					return;
				}
				LOG.DebugFormat("Unfreezing process: {0}", processName);

				foreach (ProcessThread pT in proc.Threads)
				{
					IntPtr pOpenThread = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

					if (pOpenThread == IntPtr.Zero)
					{
						break;
					}

					Kernel32.ResumeThread(pOpenThread);
				}
			}
		}

		/// <summary>
		/// Return an Image representing the Window!
		/// As GDI+ draws it, it will be without Aero borders!
		/// </summary>
		public Image PrintWindow()
		{
			Rectangle windowRect = WindowRectangle;
			// Start the capture
			Exception exceptionOccured = null;
			Image returnImage = null;
			RegionResult regionResult;
			using (Region region = GetRegion(out regionResult))
			{
				PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
				// Only use 32 bpp ARGB when the window has a region
				if (region != null)
				{
					pixelFormat = PixelFormat.Format32bppArgb;
				}
				returnImage = new Bitmap(windowRect.Width, windowRect.Height, pixelFormat);
				using (Graphics graphics = Graphics.FromImage(returnImage))
				{
					using (SafeDeviceContextHandle graphicsDC = graphics.GetSafeDeviceContext())
					{
						bool printSucceeded = User32.PrintWindow(Handle, graphicsDC.DangerousGetHandle(), 0x0);
						if (!printSucceeded)
						{
							// something went wrong, most likely a "0x80004005" (Acess Denied) when using UAC
							exceptionOccured = User32.CreateWin32Exception("PrintWindow");
						}
					}

					// Apply the region "transparency"
					if (region != null && !region.IsEmpty(graphics))
					{
						graphics.ExcludeClip(region);
						graphics.Clear(Color.Transparent);
					}

					graphics.Flush();
				}
			}

			// Return null if error
			if (exceptionOccured != null)
			{
				LOG.ErrorFormat("Error calling print window: {0}", exceptionOccured.Message);
				returnImage.Dispose();
				return null;
			}
			if (!HasParent && Maximised)
			{
				LOG.Debug("Correcting for maximalization");
				Size borderSize;
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
		public WindowDetails(IntPtr hWnd)
		{
			_hWnd = hWnd;
		}

		/// <summary>
		/// Gets an instance of the current active foreground window
		/// </summary>
		/// <returns>WindowDetails of the current window</returns>
		public static WindowDetails GetActiveWindow()
		{
			IntPtr hWnd = User32.GetForegroundWindow();
			if (hWnd != IntPtr.Zero)
			{
				if (_ignoreHandles.Contains(hWnd))
				{
					return GetDesktopWindow();
				}

				var activeWindow = new WindowDetails(hWnd);
				// Invisible Windows should not be active
				if (!activeWindow.Visible)
				{
					return GetDesktopWindow();
				}
				return activeWindow;
			}
			return null;
		}

		/// <summary>
		/// Check if this window is Greenshot
		/// </summary>
		public bool IsGreenshot
		{
			get
			{
				try
				{
					if (!IsMetroApp)
					{
						using (Process thisWindowProcess = Process)
						{
							return "Greenshot".Equals(thisWindowProcess.MainModule.FileVersionInfo.ProductName);
						}
					}
				}
				catch (Exception ex)
				{
					LOG.Warn(ex);
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the Desktop window
		/// </summary>
		/// <returns>WindowDetails for the desktop window</returns>
		public static WindowDetails GetDesktopWindow()
		{
			return new WindowDetails(User32.GetDesktopWindow());
		}

		/// <summary>
		/// Get all the top level windows
		/// </summary>
		/// <returns>List of WindowDetails with all the top level windows</returns>
		public static IList<WindowDetails> GetAllWindows()
		{
			return GetAllWindows(null);
		}

		/// <summary>
		/// Get all the top level windows, with matching classname
		/// </summary>
		/// <returns>List of WindowDetails with all the top level windows</returns>
		public static IList<WindowDetails> GetAllWindows(string classname)
		{
			return new WindowsEnumerator().GetWindows(IntPtr.Zero, classname).Items;
		}

		/// <summary>
		/// Recursive "find children which"
		/// </summary>
		/// <param name="window">Window to look into</param>
		/// <param name="point">point to check for</param>
		/// <returns></returns>
		public WindowDetails FindChildUnderPoint(Point point)
		{
			if (!Contains(point))
			{
				return null;
			}
			foreach (WindowDetails childWindow in Children)
			{
				if (childWindow.Contains(point))
				{
					return childWindow.FindChildUnderPoint(point);
				}
			}
			return this;
		}

		/// <summary>
		/// Retrieves the classname for a hWnd
		/// </summary>
		/// <param name="hWnd">IntPtr with the windows handle</param>
		/// <returns>String with ClassName</returns>
		public static String GetClassName(IntPtr hWnd)
		{
			var classNameBuilder = new StringBuilder(260, 260);
			User32.GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
			return classNameBuilder.ToString();
		}

		/// <summary>
		/// Get all the visible top level windows
		/// </summary>
		/// <returns>List of WindowDetails with all the visible top level windows</returns>
		public static IList<WindowDetails> GetVisibleWindows()
		{
			var windows = new List<WindowDetails>();
			Rectangle screenBounds = WindowCapture.GetScreenBounds();
			foreach (var window in GetMetroApps().Concat(GetAllWindows()))
			{
				// Ignore windows without title
				if (window.Text.Length == 0)
				{
					continue;
				}
				// Ignore invisible
				if (!window.Visible)
				{
					continue;
				}
				// Ignore some classes
				var ignoreClasses = new List<string>(new[] { "Progman", "XLMAIN", "Button", "Dwm" }); //"MS-SDIa"
				if (ignoreClasses.Contains(window.ClassName))
				{
					continue;
				}
				// Windows without size
				var windowRect = window.WindowRectangle;
				windowRect.Intersect(screenBounds);
				if (windowRect.Size.IsEmpty)
				{
					continue;
				}
				windows.Add(window);
			}
			return windows;
		}

		/// <summary>
		/// Get the WindowDetails for all Metro Apps
		/// These are all Windows with Classname "Windows.UI.Core.CoreWindow"
		/// </summary>
		/// <returns>List of WindowDetails with visible metro apps</returns>
		public static IList<WindowDetails> GetMetroApps()
		{
			var metroApps = new List<WindowDetails>();
			// if the appVisibility != null we have Windows 8.
			if (_appVisibility == null)
			{
				return metroApps;
			}
			//string[] wcs = {"ImmersiveGutter", "Snapped Desktop", "ImmersiveBackgroundWindow","ImmersiveLauncher","Windows.UI.Core.CoreWindow","ApplicationManager_ImmersiveShellWindow","SearchPane","MetroGhostWindow","EdgeUiInputWndClass", "NativeHWNDHost", "Shell_CharmWindow"};
			//List<WindowDetails> specials = new List<WindowDetails>();
			//foreach(string wc in wcs) {
			//	IntPtr wcHandle = User32.FindWindow(null, null);
			//	while (wcHandle != IntPtr.Zero) {
			//		WindowDetails special = new WindowDetails(wcHandle);
			//		if (special.WindowRectangle.Left >= 1920 && special.WindowRectangle.Size != Size.Empty) {
			//			specials.Add(special);
			//			LOG.DebugFormat("Found special {0} : {1} at {2} visible: {3} {4} {5}", special.ClassName, special.Text, special.WindowRectangle, special.Visible, special.ExtendedWindowStyle, special.WindowStyle);
			//		}
			//		wcHandle = User32.FindWindowEx(IntPtr.Zero, wcHandle, null, null);
			//	};
			//}
			var nextHandle = User32.FindWindow(METRO_WINDOWS_CLASS, null);
			while (nextHandle != IntPtr.Zero)
			{
				var metroApp = new WindowDetails(nextHandle);
				metroApps.Add(metroApp);
				// Check if we have a gutter!
				if (metroApp.Visible && !metroApp.Maximised)
				{
					IntPtr gutterHandle = User32.FindWindow(METRO_GUTTER_CLASS, null);
					if (gutterHandle != IntPtr.Zero)
					{
						metroApps.Add(new WindowDetails(gutterHandle));
					}
				}
				nextHandle = User32.FindWindowEx(IntPtr.Zero, nextHandle, METRO_WINDOWS_CLASS, null);
			};

			return metroApps;
		}

		/// <summary>
		/// Get all the top level windows
		/// </summary>
		/// <returns>List of WindowDetails with all the top level windows</returns>
		public static IList<WindowDetails> GetTopLevelWindows()
		{
			var windows = new List<WindowDetails>();
			var possibleTopLevelWindows = GetMetroApps();

			foreach (var window in possibleTopLevelWindows.Concat(GetAllWindows()))
			{
				// Ignore windows without title
				if (window.Text.Length == 0)
				{
					continue;
				}
				// Ignore some classes
				var ignoreClasses = new List<string>(new[] { "Progman", "XLMAIN", "Button", "Dwm" }); //"MS-SDIa"
				if (ignoreClasses.Contains(window.ClassName))
				{
					continue;
				}
				// Windows without size
				if (window.WindowRectangle.Size.IsEmpty)
				{
					continue;
				}
				if (window.HasParent)
				{
					continue;
				}
				if ((window.ExtendedWindowStyle & ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW) != 0)
				{
					continue;
				}
				// Skip some windows 10 apps, this is currently undocumented
				if ((window.ExtendedWindowStyle & ExtendedWindowStyleFlags.WS_EX_WINDOWS10) != 0)
				{
					continue;
				}
				// Skip preview windows, like the one from Firefox
				if ((window.WindowStyle & WindowStyleFlags.WS_VISIBLE) == 0)
				{
					continue;
				}
				if (!window.Visible && !window.Iconic)
				{
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
		public static WindowDetails GetLinkedWindow(WindowDetails windowToLinkTo)
		{
			int processIdSelectedWindow = windowToLinkTo.ProcessId;
			foreach (WindowDetails window in GetAllWindows())
			{
				// Ignore windows without title
				if (window.Text.Length == 0)
				{
					continue;
				}
				// Ignore invisible
				if (!window.Visible)
				{
					continue;
				}
				if (window.Handle == windowToLinkTo.Handle)
				{
					continue;
				}
				if (window.Iconic)
				{
					continue;
				}

				// Windows without size
				Size windowSize = window.WindowRectangle.Size;
				if (windowSize.Width == 0 || windowSize.Height == 0)
				{
					continue;
				}

				if (window.ProcessId == processIdSelectedWindow)
				{
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
		/// <param name="oldWindows">List of WindowDetails with old windows</param>
		public static void ActiveNewerWindows(IList<WindowDetails> oldWindows)
		{
			foreach (var window in GetVisibleWindows())
			{
				if (!oldWindows.Contains(window))
				{
					window.ToForeground();
				}
			}
		}

		/// <summary>
		/// Get the AppLauncher
		/// </summary>
		/// <returns></returns>
		public static WindowDetails GetAppLauncher()
		{
			// Only if Windows 8 (or higher)
			if (_appVisibility == null)
			{
				return null;
			}
			IntPtr appLauncher = User32.FindWindow(METRO_APPLAUNCHER_CLASS, null);
			if (appLauncher != IntPtr.Zero)
			{
				return new WindowDetails(appLauncher);
			}
			return null;
		}

		/// <summary>
		/// Return true if the metro-app-launcher is visible
		/// </summary>
		/// <returns></returns>
		public static bool IsAppLauncherVisible
		{
			get
			{
				if (_appVisibility != null)
				{
					return _appVisibility.ComObject.IsLauncherVisible;
				}
				return false;
			}
		}

		/// <summary>
		/// Used for logging
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return string.Format("{0} - {1} '{2}', {3}", ClassName, Process.ProcessName, Text, WindowRectangle);
		}
	}
	#endregion
}
