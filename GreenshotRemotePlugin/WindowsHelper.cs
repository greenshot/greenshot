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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Plugin;
using GreenshotPlugin.Core;

/// <summary>
/// Code for handling with "windows"
/// Main code is taken from vbAccelerator, location:
/// http://www.vbaccelerator.com/home/NET/Code/Libraries/Windows/Enumerating_Windows/article.asp
/// but a lot of changes/enhancements were made to adapt it for Greenshot.
/// </summary>
namespace GreenshotRemotePlugin {
	
	#region Global Enums
	/// <summary>
	/// Window Style Flags
	/// </summary>
	[Flags]
    public enum WindowStyleFlags : uint {
		WS_OVERLAPPED       = 0x00000000,
		WS_POPUP            = 0x80000000,
		WS_CHILD            = 0x40000000,
		WS_MINIMIZE         = 0x20000000,
		WS_VISIBLE          = 0x10000000,
		WS_DISABLED         = 0x08000000,
		WS_CLIPSIBLINGS     = 0x04000000,
		WS_CLIPCHILDREN     = 0x02000000,
		WS_MAXIMIZE         = 0x01000000,
		WS_BORDER           = 0x00800000,
		WS_DLGFRAME         = 0x00400000,
		WS_VSCROLL          = 0x00200000,
		WS_HSCROLL          = 0x00100000,
		WS_SYSMENU          = 0x00080000,
		WS_THICKFRAME       = 0x00040000,
		WS_GROUP            = 0x00020000,
		WS_TABSTOP          = 0x00010000,

		WS_MINIMIZEBOX      = 0x00020000,
		WS_MAXIMIZEBOX      = 0x00010000,

		WS_CAPTION          = WS_BORDER | WS_DLGFRAME,
		WS_TILED            = WS_OVERLAPPED,
		WS_ICONIC           = WS_MINIMIZE,
		WS_SIZEBOX          = WS_THICKFRAME,
		WS_TILEDWINDOW      = WS_OVERLAPPEDWINDOW,

		WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
		WS_POPUPWINDOW	    = WS_POPUP | WS_BORDER | WS_SYSMENU,
		WS_CHILDWINDOW      = WS_CHILD
	}

	[Flags]
    public enum ExtendedWindowStyleFlags : uint {
		WS_EX_DLGMODALFRAME    = 0x00000001,
		WS_EX_NOPARENTNOTIFY   = 0x00000004,
		WS_EX_TOPMOST      = 0x00000008,
		WS_EX_ACCEPTFILES      = 0x00000010,
		WS_EX_TRANSPARENT      = 0x00000020,

		//#if(WINVER >= 0x0400)
		WS_EX_MDICHILD     = 0x00000040,
		WS_EX_TOOLWINDOW       = 0x00000080,
		WS_EX_WINDOWEDGE       = 0x00000100,
		WS_EX_CLIENTEDGE       = 0x00000200,
		WS_EX_CONTEXTHELP      = 0x00000400,

		WS_EX_RIGHT        = 0x00001000,
		WS_EX_LEFT         = 0x00000000,
		WS_EX_RTLREADING       = 0x00002000,
		WS_EX_LTRREADING       = 0x00000000,
		WS_EX_LEFTSCROLLBAR    = 0x00004000,
		WS_EX_RIGHTSCROLLBAR   = 0x00000000,

		WS_EX_CONTROLPARENT    = 0x00010000,
		WS_EX_STATICEDGE       = 0x00020000,
		WS_EX_APPWINDOW    = 0x00040000,

		WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE),
		WS_EX_PALETTEWINDOW    = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST),

		//#endif /* WINVER >= 0x0400 */

		//#if(WIN32WINNT >= 0x0500)
		WS_EX_LAYERED      = 0x00080000,
		//#endif /* WIN32WINNT >= 0x0500 */

		//#if(WINVER >= 0x0500)
		WS_EX_NOINHERITLAYOUT  = 0x00100000, // Disable inheritence of mirroring by children
		WS_EX_LAYOUTRTL    = 0x00400000, // Right to left mirroring
		//#endif /* WINVER >= 0x0500 */

		//#if(WIN32WINNT >= 0x0500)
		WS_EX_COMPOSITED       = 0x02000000,
		WS_EX_NOACTIVATE       = 0x08000000
		//#endif /* WIN32WINNT >= 0x0500 */
    }
	#endregion

	#region EnumWindows
	/// <summary>
	/// EnumWindows wrapper for .NET
	/// </summary>
	public class WindowsEnumerator {
		#region Delegates
		private delegate int EnumWindowsProc(IntPtr hwnd, int lParam);
		#endregion

		#region UnManagedMethods
		private class UnManagedMethods {
			[DllImport("user32", SetLastError = true)]
			public extern static int EnumWindows(EnumWindowsProc lpEnumFunc, int lParam);
			[DllImport("user32", SetLastError = true)]
			public extern static int EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, int lParam);
		}
		#endregion

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
			UnManagedMethods.EnumWindows(new EnumWindowsProc(this.WindowEnum), 0);
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
			UnManagedMethods.EnumChildWindows(hWndParent, new EnumWindowsProc(this.WindowEnum), 0);
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
	public class WindowDetails {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WindowDetails));
		private static Dictionary<string, List<string>> classnameTree = new Dictionary<string, List<string>>();

		#region Structures
		
		// See: http://msdn.microsoft.com/en-us/library/aa969502(v=vs.85).aspx
		[StructLayout(LayoutKind.Sequential)]
		public struct DWM_THUMBNAIL_PROPERTIES {
			// A bitwise combination of DWM thumbnail constant values that indicates which members of this structure are set.
			public int dwFlags;
			// The area in the destination window where the thumbnail will be rendered.
			public RECT rcDestination;
			// The region of the source window to use as the thumbnail. By default, the entire window is used as the thumbnail.
			public RECT rcSource;
			// The opacity with which to render the thumbnail. 0 is fully transparent while 255 is fully opaque. The default value is 255.
			public byte opacity;
			// TRUE to make the thumbnail visible; otherwise, FALSE. The default is FALSE.
			public bool fVisible;
			// TRUE to use only the thumbnail source's client area; otherwise, FALSE. The default is FALSE.
			public bool fSourceClientAreaOnly;
			public RECT Destination {
				set {
					dwFlags |= DWM_TNP_RECTDESTINATION;
					rcDestination = value;
				}
			}
			public RECT Source {
				set {
					dwFlags |= DWM_TNP_RECTSOURCE;
					rcSource = value;
				}
			}
			public byte Opacity {
				set {
					dwFlags |= DWM_TNP_OPACITY;
					opacity = value;
				}
			}
			public bool Visible {
				set {
					dwFlags |= DWM_TNP_VISIBLE;
					fVisible = value;
				}
			}
			public bool SourceClientAreaOnly {
				set {
					dwFlags |= DWM_TNP_SOURCECLIENTAREAONLY;
					fSourceClientAreaOnly = value;
				}
			}
			// A value for the rcDestination member has been specified.
			public const int DWM_TNP_RECTDESTINATION = 0x00000001;
			// A value for the rcSource member has been specified.
			public const int DWM_TNP_RECTSOURCE = 0x00000002;
			// A value for the opacity member has been specified.
			public const int DWM_TNP_OPACITY = 0x00000004;
			// A value for the fVisible member has been specified.
			public const int DWM_TNP_VISIBLE = 0x00000008;
			// A value for the fSourceClientAreaOnly member has been specified.
			public const int DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct WINDOWINFO {
			public uint cbSize;
			public RECT rcWindow;
			public RECT rcClient;
			public uint dwStyle;
			public uint dwExStyle;
			public uint dwWindowStatus;
			public uint cxWindowBorders;
			public uint cyWindowBorders;
			public ushort atomWindowType;
			public ushort wCreatorVersion;
			// Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
			public WINDOWINFO(Boolean ? filler) : this() {
				cbSize = (UInt32)(Marshal.SizeOf(typeof( WINDOWINFO )));
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT {
			private int _Left;
			private int _Top;
			private int _Right;
			private int _Bottom;
		
			public RECT(RECT Rectangle) : this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom) {
			}
			public RECT(int Left, int Top, int Right, int Bottom) {
				_Left = Left;
				_Top = Top;
				_Right = Right;
				_Bottom = Bottom;
			}
		
			public int X {
				get { return _Left; }
				set { _Left = value; }
			}
			public int Y {
				get { return _Top; }
				set { _Top = value; }
			}
			public int Left {
				get { return _Left; }
				set { _Left = value; }
			}
			public int Top {
				get { return _Top; }
				set { _Top = value; }
			}
			public int Right {
				get { return _Right; }
				set { _Right = value; }
			}
			public int Bottom {
				get { return _Bottom; }
				set { _Bottom = value; }
			}
			public int Height {
				get { return _Bottom - _Top; }
				set { _Bottom = value - _Top; }
			}
			public int Width {
				get { return _Right - _Left; }
				set { _Right = value + _Left; }
			}
			public Point Location {
				get { return new Point(Left, Top); }
				set {
					_Left = value.X;
					_Top = value.Y;
				}
			}
			public Size Size {
				get { return new Size(Width, Height); }
				set {
					_Right = value.Width + _Left;
					_Bottom = value.Height + _Top;
				}
			}
		
			public static implicit operator Rectangle(RECT Rectangle) {
				return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
			}
			public static implicit operator RECT(Rectangle Rectangle) {
				return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
			}
			public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
			{
				return Rectangle1.Equals(Rectangle2);
			}
			public static bool operator !=(RECT Rectangle1, RECT Rectangle2) {
				return !Rectangle1.Equals(Rectangle2);
			}
		
			public override string ToString() {
				return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
			}
		
			public override int GetHashCode() {
				return ToString().GetHashCode();
			}
		
			public bool Equals(RECT Rectangle) {
				return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
			}
			
			public Rectangle ToRectangle() {
				return new Rectangle(Left, Top, Width, Height);
			}
			public override bool Equals(object Object) {
				if (Object is RECT) {
					return Equals((RECT)Object);
				} else if (Object is Rectangle) {
					return Equals(new RECT((Rectangle)Object));
				}
		
				return false;
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct SIZE {
			public int cx;
			public int cy;
			public SIZE(int cx, int cy) {
				this.cx = cx;
				this.cy = cy;
			}
			public Size ToSize() {
				return new Size(cx, cy);
			}
		} 

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT {
			public int X;
			public int Y;
	
			public POINT(int x, int y) {
				this.X = x;
				this.Y = y;
			}
	
			public static implicit operator System.Drawing.Point(POINT p) {
				return new System.Drawing.Point(p.X, p.Y);
			}
	
			public static implicit operator POINT(System.Drawing.Point p) {
				return new POINT(p.X, p.Y);
			}
			
			override public string ToString() {
				return X +","+Y;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct FLASHWINFO {
			public int cbSize;
			public IntPtr hwnd;
			public int dwFlags;
			public int uCount;
			public int dwTimeout;
		}
		#endregion Structures

		#region Enums
		/// <summary>
		/// Flags used with the Windows API (User32.dll):GetSystemMetrics(SystemMetric smIndex)
		///  
		/// This Enum and declaration signature was written by Gabriel T. Sharp
		/// ai_productions@verizon.net or osirisgothra@hotmail.com
		/// Obtained on pinvoke.net, please contribute your code to support the wiki!
		/// </summary>
		public enum SystemMetric : int {
			/// <summary>
			///  Width of the screen of the primary display monitor, in pixels. This is the same values obtained by calling GetDeviceCaps as follows: GetDeviceCaps( hdcPrimaryMonitor, HORZRES).
			/// </summary>
			SM_CXSCREEN=0,
			/// <summary>
			/// Height of the screen of the primary display monitor, in pixels. This is the same values obtained by calling GetDeviceCaps as follows: GetDeviceCaps( hdcPrimaryMonitor, VERTRES).
			/// </summary>
			SM_CYSCREEN=1,
			/// <summary>
			/// Width of a horizontal scroll bar, in pixels.
			/// </summary>
			SM_CYVSCROLL=2,
			/// <summary>
			/// Height of a horizontal scroll bar, in pixels.
			/// </summary>
			SM_CXVSCROLL=3,
			/// <summary>
			/// Height of a caption area, in pixels.
			/// </summary>
			SM_CYCAPTION=4,
			/// <summary>
			/// Width of a window border, in pixels. This is equivalent to the SM_CXEDGE value for windows with the 3-D look.
			/// </summary>
			SM_CXBORDER=5,
			/// <summary>
			/// Height of a window border, in pixels. This is equivalent to the SM_CYEDGE value for windows with the 3-D look.
			/// </summary>
			SM_CYBORDER=6,
			/// <summary>
			/// Thickness of the frame around the perimeter of a window that has a caption but is not sizable, in pixels. SM_CXFIXEDFRAME is the height of the horizontal border and SM_CYFIXEDFRAME is the width of the vertical border.
			/// </summary>
			SM_CXDLGFRAME=7,
			/// <summary>
			/// Thickness of the frame around the perimeter of a window that has a caption but is not sizable, in pixels. SM_CXFIXEDFRAME is the height of the horizontal border and SM_CYFIXEDFRAME is the width of the vertical border.
			/// </summary>
			SM_CYDLGFRAME=8,
			/// <summary>
			/// Height of the thumb box in a vertical scroll bar, in pixels
			/// </summary>
			SM_CYVTHUMB=9,
			/// <summary>
			/// Width of the thumb box in a horizontal scroll bar, in pixels.
			/// </summary>
			SM_CXHTHUMB=10,
			/// <summary>
			/// Default width of an icon, in pixels. The LoadIcon function can load only icons with the dimensions specified by SM_CXICON and SM_CYICON
			/// </summary>
			SM_CXICON=11,
			/// <summary>
			/// Default height of an icon, in pixels. The LoadIcon function can load only icons with the dimensions SM_CXICON and SM_CYICON.
			/// </summary>
			SM_CYICON=12,
			/// <summary>
			/// Width of a cursor, in pixels. The system cannot create cursors of other sizes.
			/// </summary>
			SM_CXCURSOR=13,
			/// <summary>
			/// Height of a cursor, in pixels. The system cannot create cursors of other sizes.
			/// </summary>
			SM_CYCURSOR=14,
			/// <summary>
			/// Height of a single-line menu bar, in pixels.
			/// </summary>
			SM_CYMENU=15,
			/// <summary>
			/// Width of the client area for a full-screen window on the primary display monitor, in pixels. To get the coordinates of the portion of the screen not obscured by the system taskbar or by application desktop toolbars, call the SystemParametersInfo function with the SPI_GETWORKAREA value.
			/// </summary>
			SM_CXFULLSCREEN=16,
			/// <summary>
			/// Height of the client area for a full-screen window on the primary display monitor, in pixels. To get the coordinates of the portion of the screen not obscured by the system taskbar or by application desktop toolbars, call the SystemParametersInfo function with the SPI_GETWORKAREA value.
			/// </summary>
			SM_CYFULLSCREEN=17,
			/// <summary>
			/// For double byte character set versions of the system, this is the height of the Kanji window at the bottom of the screen, in pixels
			/// </summary>
			SM_CYKANJIWINDOW=18,
			/// <summary>
			/// Nonzero if a mouse with a wheel is installed; zero otherwise
			/// </summary>
			SM_MOUSEWHEELPRESENT=75,
			/// <summary>
			/// Height of the arrow bitmap on a vertical scroll bar, in pixels.
			/// </summary>
			SM_CYHSCROLL=20,
			/// <summary>
			/// Width of the arrow bitmap on a horizontal scroll bar, in pixels.
			/// </summary>
			SM_CXHSCROLL=21,
			/// <summary>
			/// Nonzero if the debug version of User.exe is installed; zero otherwise.
			/// </summary>
			SM_DEBUG=22,
			/// <summary>
			/// Nonzero if the left and right mouse buttons are reversed; zero otherwise.
			/// </summary>
			SM_SWAPBUTTON=23,
			/// <summary>
			/// Reserved for future use
			/// </summary>
			SM_RESERVED1=24,
			/// <summary>
			/// Reserved for future use
			/// </summary>
			SM_RESERVED2=25,
			/// <summary>
			/// Reserved for future use
			/// </summary>
			SM_RESERVED3=26,
			/// <summary>
			/// Reserved for future use
			/// </summary>
			SM_RESERVED4=27,
			/// <summary>
			/// Minimum width of a window, in pixels.
			/// </summary>
			SM_CXMIN=28,
			/// <summary>
			/// Minimum height of a window, in pixels.
			/// </summary>
			SM_CYMIN=29,
			/// <summary>
			/// Width of a button in a window's caption or title bar, in pixels.
			/// </summary>
			SM_CXSIZE=30,
			/// <summary>
			/// Height of a button in a window's caption or title bar, in pixels.
			/// </summary>
			SM_CYSIZE=31,
			/// <summary>
			/// Thickness of the sizing border around the perimeter of a window that can be resized, in pixels. SM_CXSIZEFRAME is the width of the horizontal border, and SM_CYSIZEFRAME is the height of the vertical border.
			/// </summary>
			SM_CXFRAME=32,
			/// <summary>
			/// Thickness of the sizing border around the perimeter of a window that can be resized, in pixels. SM_CXSIZEFRAME is the width of the horizontal border, and SM_CYSIZEFRAME is the height of the vertical border.
			/// </summary>
			SM_CYFRAME=33,
			/// <summary>
			/// Minimum tracking width of a window, in pixels. The user cannot drag the window frame to a size smaller than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
			/// </summary>
			SM_CXMINTRACK=34,
			/// <summary>
			/// Minimum tracking height of a window, in pixels. The user cannot drag the window frame to a size smaller than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message
			/// </summary>
			SM_CYMINTRACK=35,
			/// <summary>
			/// Width of the rectangle around the location of a first click in a double-click sequence, in pixels. The second click must occur within the rectangle defined by SM_CXDOUBLECLK and SM_CYDOUBLECLK for the system to consider the two clicks a double-click
			/// </summary>
			SM_CXDOUBLECLK=36,
			/// <summary>
			/// Height of the rectangle around the location of a first click in a double-click sequence, in pixels. The second click must occur within the rectangle defined by SM_CXDOUBLECLK and SM_CYDOUBLECLK for the system to consider the two clicks a double-click. (The two clicks must also occur within a specified time.)
			/// </summary>
			SM_CYDOUBLECLK=37,
			/// <summary>
			/// Width of a grid cell for items in large icon view, in pixels. Each item fits into a rectangle of size SM_CXICONSPACING by SM_CYICONSPACING when arranged. This value is always greater than or equal to SM_CXICON
			/// </summary>
			SM_CXICONSPACING=38,
			/// <summary>
			/// Height of a grid cell for items in large icon view, in pixels. Each item fits into a rectangle of size SM_CXICONSPACING by SM_CYICONSPACING when arranged. This value is always greater than or equal to SM_CYICON.
			/// </summary>
			SM_CYICONSPACING=39,
			/// <summary>
			/// Nonzero if drop-down menus are right-aligned with the corresponding menu-bar item; zero if the menus are left-aligned.
			/// </summary>
			SM_MENUDROPALIGNMENT=40,
			/// <summary>
			/// Nonzero if the Microsoft Windows for Pen computing extensions are installed; zero otherwise.
			/// </summary>
			SM_PENWINDOWS=41,
			/// <summary>
			/// Nonzero if User32.dll supports DBCS; zero otherwise. (WinMe/95/98): Unicode
			/// </summary>
			SM_DBCSENABLED=42,
			/// <summary>
			/// Number of buttons on mouse, or zero if no mouse is installed.
			/// </summary>
			SM_CMOUSEBUTTONS=43,
			/// <summary>
			/// Identical Values Changed After Windows NT 4.0  
			/// </summary>
			SM_CXFIXEDFRAME=SM_CXDLGFRAME,
			/// <summary>
			/// Identical Values Changed After Windows NT 4.0
			/// </summary>
			SM_CYFIXEDFRAME=SM_CYDLGFRAME,
			/// <summary>
			/// Identical Values Changed After Windows NT 4.0
			/// </summary>
			SM_CXSIZEFRAME=SM_CXFRAME,
			/// <summary>
			/// Identical Values Changed After Windows NT 4.0
			/// </summary>
			SM_CYSIZEFRAME=SM_CYFRAME,
			/// <summary>
			/// Nonzero if security is present; zero otherwise.
			/// </summary>
			SM_SECURE=44,
			/// <summary>
			/// Width of a 3-D border, in pixels. This is the 3-D counterpart of SM_CXBORDER
			/// </summary>
			SM_CXEDGE=45,
			/// <summary>
			/// Height of a 3-D border, in pixels. This is the 3-D counterpart of SM_CYBORDER
			/// </summary>
			SM_CYEDGE=46,
			/// <summary>
			/// Width of a grid cell for a minimized window, in pixels. Each minimized window fits into a rectangle this size when arranged. This value is always greater than or equal to SM_CXMINIMIZED.
			/// </summary>
			SM_CXMINSPACING=47,
			/// <summary>
			/// Height of a grid cell for a minimized window, in pixels. Each minimized window fits into a rectangle this size when arranged. This value is always greater than or equal to SM_CYMINIMIZED.
			/// </summary>
			SM_CYMINSPACING=48,
			/// <summary>
			/// Recommended width of a small icon, in pixels. Small icons typically appear in window captions and in small icon view
			/// </summary>
			SM_CXSMICON=49,
			/// <summary>
			/// Recommended height of a small icon, in pixels. Small icons typically appear in window captions and in small icon view.
			/// </summary>
			SM_CYSMICON=50,
			/// <summary>
			/// Height of a small caption, in pixels
			/// </summary>
			SM_CYSMCAPTION=51,
			/// <summary>
			/// Width of small caption buttons, in pixels.
			/// </summary>
			SM_CXSMSIZE=52,
			/// <summary>
			/// Height of small caption buttons, in pixels.
			/// </summary>
			SM_CYSMSIZE=53,
			/// <summary>
			/// Width of menu bar buttons, such as the child window close button used in the multiple document interface, in pixels.
			/// </summary>
			SM_CXMENUSIZE=54,
			/// <summary>
			/// Height of menu bar buttons, such as the child window close button used in the multiple document interface, in pixels.
			/// </summary>
			SM_CYMENUSIZE=55,
			/// <summary>
			/// Flags specifying how the system arranged minimized windows
			/// </summary>
			SM_ARRANGE=56,
			/// <summary>
			/// Width of a minimized window, in pixels.
			/// </summary>
			SM_CXMINIMIZED=57,
			/// <summary>
			/// Height of a minimized window, in pixels.
			/// </summary>
			SM_CYMINIMIZED=58,
			/// <summary>
			/// Default maximum width of a window that has a caption and sizing borders, in pixels. This metric refers to the entire desktop. The user cannot drag the window frame to a size larger than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
			/// </summary>
			SM_CXMAXTRACK=59,
			/// <summary>
			/// Default maximum height of a window that has a caption and sizing borders, in pixels. This metric refers to the entire desktop. The user cannot drag the window frame to a size larger than these dimensions. A window can override this value by processing the WM_GETMINMAXINFO message.
			/// </summary>
			SM_CYMAXTRACK=60,
			/// <summary>
			/// Default width, in pixels, of a maximized top-level window on the primary display monitor.
			/// </summary>
			SM_CXMAXIMIZED=61,
			/// <summary>
			/// Default height, in pixels, of a maximized top-level window on the primary display monitor.
			/// </summary>
			SM_CYMAXIMIZED=62,
			/// <summary>
			/// Least significant bit is set if a network is present; otherwise, it is cleared. The other bits are reserved for future use
			/// </summary>
			SM_NETWORK=63,
			/// <summary>
			/// Value that specifies how the system was started: 0-normal, 1-failsafe, 2-failsafe /w net
			/// </summary>
			SM_CLEANBOOT=67,
			/// <summary>
			/// Width of a rectangle centered on a drag point to allow for limited movement of the mouse pointer before a drag operation begins, in pixels.
			/// </summary>
			SM_CXDRAG=68,
			/// <summary>
			/// Height of a rectangle centered on a drag point to allow for limited movement of the mouse pointer before a drag operation begins. This value is in pixels. It allows the user to click and release the mouse button easily without unintentionally starting a drag operation.
			/// </summary>
			SM_CYDRAG=69,
			/// <summary>
			/// Nonzero if the user requires an application to present information visually in situations where it would otherwise present the information only in audible form; zero otherwise.
			/// </summary>
			SM_SHOWSOUNDS=70,
			/// <summary>
			/// Width of the default menu check-mark bitmap, in pixels.
			/// </summary>
			SM_CXMENUCHECK=71,
			/// <summary>
			/// Height of the default menu check-mark bitmap, in pixels.
			/// </summary>
			SM_CYMENUCHECK=72,
			/// <summary>
			/// Nonzero if the computer has a low-end (slow) processor; zero otherwise
			/// </summary>
			SM_SLOWMACHINE=73,
			/// <summary>
			/// Nonzero if the system is enabled for Hebrew and Arabic languages, zero if not.
			/// </summary>
			SM_MIDEASTENABLED=74,
			/// <summary>
			/// Nonzero if a mouse is installed; zero otherwise. This value is rarely zero, because of support for virtual mice and because some systems detect the presence of the port instead of the presence of a mouse.
			/// </summary>
			SM_MOUSEPRESENT=19,
			/// <summary>
			/// Windows 2000 (v5.0+) Coordinate of the top of the virtual screen
			/// </summary>
			SM_XVIRTUALSCREEN=76,
			/// <summary>
			/// Windows 2000 (v5.0+) Coordinate of the left of the virtual screen
			/// </summary>
			SM_YVIRTUALSCREEN=77,
			/// <summary>
			/// Windows 2000 (v5.0+) Width of the virtual screen
			/// </summary>
			SM_CXVIRTUALSCREEN=78,
			/// <summary>
			/// Windows 2000 (v5.0+) Height of the virtual screen
			/// </summary>
			SM_CYVIRTUALSCREEN=79,
			/// <summary>
			/// Number of display monitors on the desktop
			/// </summary>
			SM_CMONITORS=80,
			/// <summary>
			/// Windows XP (v5.1+) Nonzero if all the display monitors have the same color format, zero otherwise. Note that two displays can have the same bit depth, but different color formats. For example, the red, green, and blue pixels can be encoded with different numbers of bits, or those bits can be located in different places in a pixel's color value.
			/// </summary>
			SM_SAMEDISPLAYFORMAT=81,
			/// <summary>
			/// Windows XP (v5.1+) Nonzero if Input Method Manager/Input Method Editor features are enabled; zero otherwise
			/// </summary>
			SM_IMMENABLED=82,
			/// <summary>
			/// Windows XP (v5.1+) Width of the left and right edges of the focus rectangle drawn by DrawFocusRect. This value is in pixels.
			/// </summary>
			SM_CXFOCUSBORDER=83,
			/// <summary>
			/// Windows XP (v5.1+) Height of the top and bottom edges of the focus rectangle drawn by DrawFocusRect. This value is in pixels.
			/// </summary>
			SM_CYFOCUSBORDER=84,
			/// <summary>
			/// Nonzero if the current operating system is the Windows XP Tablet PC edition, zero if not.
			/// </summary>
			SM_TABLETPC=86,
			/// <summary>
			/// Nonzero if the current operating system is the Windows XP, Media Center Edition, zero if not.
			/// </summary>
			SM_MEDIACENTER=87,
			/// <summary>
			/// Metrics Other
			/// </summary>
			SM_CMETRICS_OTHER=76,
			/// <summary>
			/// Metrics Windows 2000
			/// </summary>
			SM_CMETRICS_2000=83,
			/// <summary>
			/// Metrics Windows NT
			/// </summary>
			SM_CMETRICS_NT=88,
			/// <summary>
			/// Windows XP (v5.1+) This system metric is used in a Terminal Services environment. If the calling process is associated with a Terminal Services client session, the return value is nonzero. If the calling process is associated with the Terminal Server console session, the return value is zero. The console session is not necessarily the physical console - see WTSGetActiveConsoleSessionId for more information.
			/// </summary>
			SM_REMOTESESSION=0x1000,
			/// <summary>
			/// Windows XP (v5.1+) Nonzero if the current session is shutting down; zero otherwise
			/// </summary>
			SM_SHUTTINGDOWN=0x2000,
			/// <summary>
			/// Windows XP (v5.1+) This system metric is used in a Terminal Services environment. Its value is nonzero if the current session is remotely controlled; zero otherwise
			/// </summary>
			SM_REMOTECONTROL=0x2001
		}

		// See http://msdn.microsoft.com/en-us/library/aa969530(v=vs.85).aspx
		public enum DWMWINDOWATTRIBUTE {
			DWMWA_NCRENDERING_ENABLED = 1,
			DWMWA_NCRENDERING_POLICY,
			DWMWA_TRANSITIONS_FORCEDISABLED,
			DWMWA_ALLOW_NCPAINT,
			DWMWA_CAPTION_BUTTON_BOUNDS,
			DWMWA_NONCLIENT_RTL_LAYOUT,
			DWMWA_FORCE_ICONIC_REPRESENTATION,
			DWMWA_FLIP3D_POLICY,
			DWMWA_EXTENDED_FRAME_BOUNDS, // This is the one we need for retrieving the Window size since Windows Vista
			DWMWA_HAS_ICONIC_BITMAP,
			DWMWA_DISALLOW_PEEK,
			DWMWA_LAST
		}
		
		public enum GetWindow_Cmd : uint {
			GW_HWNDFIRST = 0,
			GW_HWNDLAST = 1,
			GW_HWNDNEXT = 2,
			GW_HWNDPREV = 3,
			GW_OWNER = 4,
			GW_CHILD = 5,
			GW_ENABLEDPOPUP = 6
		}

		#endregion

		#region UnManagedMethods
		private class UnManagedMethods {
			[DllImport("user32", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public extern static bool IsWindowVisible(IntPtr hWnd);
			[DllImport("user32.dll", SetLastError = true, ExactSpelling=true, CharSet=CharSet.Auto)]
			public static extern IntPtr GetParent(IntPtr hWnd);
			[DllImport("user32", SetLastError = true)]
			public static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);
			[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
			public extern static int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);
			[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
			public extern static int GetWindowTextLength(IntPtr hWnd);
			[DllImport("user32", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool BringWindowToTop(IntPtr hWnd);
			[DllImport("user32", SetLastError = true)]
			public static extern IntPtr GetForegroundWindow();
			[DllImport("user32.dll", SetLastError = true)]
			public static extern IntPtr GetDesktopWindow();
			[DllImport("user32", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetForegroundWindow(IntPtr hWnd);
			[DllImport("user32", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public extern static bool IsIconic(IntPtr hWnd);
			[DllImport("user32", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public extern static bool IsZoomed(IntPtr hwnd);
			[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
			public extern static int GetClassName (IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
			[DllImport("user32", SetLastError = true)]
			public extern static int FlashWindow(IntPtr hWnd, ref FLASHWINFO pwfi);
			[DllImport("user32", SetLastError = true)]
			public extern static int GetWindowRect(IntPtr hWnd, out RECT lpRect);
			[DllImport("user32", SetLastError = true)]
			public extern static int GetClientRect(IntPtr hWnd, out RECT lpRect);
			[DllImport("user32", SetLastError = true)]
			public extern static int ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
			[DllImport("user32", SetLastError = true)]
	   		[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);
			[DllImport("user32", SetLastError=true)]
			public extern static int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
			[DllImport("user32", SetLastError=true)]
			public extern static uint GetWindowLong(IntPtr hwnd, int nIndex);
			[DllImport("user32", EntryPoint="GetWindowLongPtr", SetLastError=true)]
			public extern static IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);
			[DllImport("user32", SetLastError = true)]
			public static extern int GetSystemMetrics(SystemMetric smIndex);
			[DllImport("dwmapi", SetLastError = true)]
			public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT lpRect, int size);
			[return: MarshalAs(UnmanagedType.Bool)]
			[DllImport("user32.dll",SetLastError = true)]
			public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);
			
			// DWM 
			[DllImport("dwmapi.dll")]
			public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);
			[DllImport("dwmapi.dll")]
			public static extern int DwmUnregisterThumbnail(IntPtr thumb);
			[DllImport("dwmapi.dll")]
			public static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out WindowDetails.SIZE size);
			[DllImport("dwmapi.dll")]
			public static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

			public const int WM_COMMAND = 0x111;
			public const int WM_SYSCOMMAND = 0x112;
				
			public const int SC_RESTORE = 0xF120;
			public const int SC_CLOSE = 0xF060;
			public const int SC_MAXIMIZE = 0xF030;
			public const int SC_MINIMIZE = 0xF020;

			public const int GWL_ID = (-12);
			public const int GWL_STYLE = (-16);
			public const int GWL_EXSTYLE = (-20);

			public const int PW_DEFAULT = 0x00;
			public const int PW_CLIENTONLY = 0x01;

			/// <summary>
			/// Stop flashing. The system restores the window to its original state.
			/// </summary>
			public const int FLASHW_STOP = 0;
			/// <summary>
			/// Flash the window caption. 
			/// </summary>
			public const int FLASHW_CAPTION = 0x00000001;
			/// <summary>
			/// Flash the taskbar button.
			/// </summary>
			public const int FLASHW_TRAY = 0x00000002;
			/// <summary>
			/// Flash both the window caption and taskbar button.
			/// </summary>
			public const int FLASHW_ALL = (FLASHW_CAPTION | FLASHW_TRAY);
			/// <summary>
			/// Flash continuously, until the FLASHW_STOP flag is set.
			/// </summary>
			public const int FLASHW_TIMER = 0x00000004;
			/// <summary>
			/// Flash continuously until the window comes to the foreground. 
			/// </summary>
			public const int FLASHW_TIMERNOFG = 0x0000000C;
		}
		#endregion

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
		public override System.Int32 GetHashCode() {
			return (System.Int32)this.hWnd;
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
				IntPtr parenthWnd = UnManagedMethods.GetParent(Handle);
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
			foreach(WindowDetails window in windowsEnumerator.Items) {
				// Copy "Parent title"
				if (window.Text != null && window.Text.Trim().Length == 0) {
					window.Text = Text;
				}
				window.parent = this;
				childWindows.Add(window);
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
		/// <param name="classname">The classname to look for</param>
		/// <returns>null if nothing found, otherwise the WindowDetails instance of the child</returns>
		public WindowDetails GetWindow(GetWindow_Cmd cmd) {
			IntPtr tmphWnd = UnManagedMethods.GetWindow(Handle, cmd);
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
					UnManagedMethods.GetWindowText(this.hWnd, title, title.Capacity);
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
					UnManagedMethods.GetClassName(this.hWnd, classNameBuilder, classNameBuilder.Capacity);
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
				return UnManagedMethods.IsIconic(this.hWnd);
			}
			set {
				UnManagedMethods.SendMessage(
					this.hWnd, 
					UnManagedMethods.WM_SYSCOMMAND, 
					(IntPtr)UnManagedMethods.SC_MINIMIZE,
					IntPtr.Zero);
			}
		}
			
		/// <summary>
		/// Gets/Sets whether the window is maximised or not.
		/// </summary>
		public bool Maximised {
			get {
				return UnManagedMethods.IsZoomed(this.hWnd);
			}
			set {
				UnManagedMethods.SendMessage(
					this.hWnd,
					UnManagedMethods.WM_SYSCOMMAND, 
					(IntPtr)UnManagedMethods.SC_MAXIMIZE,
					IntPtr.Zero);
			}
		}

		/// <summary>
		/// Gets whether the window is visible.
		/// </summary>
		public bool Visible {
			get {
				return UnManagedMethods.IsWindowVisible(this.hWnd);
			}
		}

		/// <summary>
		/// Gets the bounding rectangle of the window
		/// </summary>
		public Rectangle ClientRectangle {
			get {
				Rectangle clientRect = Rectangle.Empty;

				if (Environment.OSVersion.Version.Major >= 6 && parent == null) {
					GetExtendedFrameBounds(out clientRect);
				}
				if (clientRect.IsEmpty) {
					GetClientRect(out clientRect);
				}

				if (this.Maximised) {
					LOG.Debug("Correcting for maximalization");
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
				UnManagedMethods.SendMessage(this.hWnd, UnManagedMethods.WM_SYSCOMMAND, (IntPtr)UnManagedMethods.SC_RESTORE, IntPtr.Zero);
			}
			UnManagedMethods.BringWindowToTop(this.hWnd);
			UnManagedMethods.SetForegroundWindow(this.hWnd);
		}

		public WindowStyleFlags WindowStyle {
			get {
				return (WindowStyleFlags)UnManagedMethods.GetWindowLong(this.hWnd, UnManagedMethods.GWL_STYLE);
			}
		}

		
		public ExtendedWindowStyleFlags ExtendedWindowStyle {
			get {
				return (ExtendedWindowStyleFlags)UnManagedMethods.GetWindowLong(this.hWnd, UnManagedMethods.GWL_EXSTYLE);
			}
		}
		/// <summary>
		/// Return an Image representating the Window
		/// </summary>
		public Bitmap PrintWindow() {
			return PrintWindow(Rectangle.Empty);
		}
	
		/// <summary>
		/// Helper method to get the window size for DWM Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>
		private bool GetExtendedFrameBounds(out Rectangle rectangle) {
			RECT rect;
			int result = UnManagedMethods.DwmGetWindowAttribute(Handle, (int)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
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
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = UnManagedMethods.GetWindowInfo(Handle, ref windowInfo);
			if (result) {
				rectangle = windowInfo.rcWindow.ToRectangle();
			} else {
				rectangle =  Rectangle.Empty;
			}
			return result;
		}

		/// <summary>
		/// Helper method to get the Border size for GDI Windows
		/// </summary>
		/// <param name="rectangle">out Rectangle</param>
		/// <returns>bool true if it worked</returns>	
		private bool GetBorderSize(out Size size) {
			WINDOWINFO windowInfo = new WINDOWINFO();
			// Get the Window Info for this window
			bool result = UnManagedMethods.GetWindowInfo(Handle, ref windowInfo);
			if (result) {
				size = new Size((int)windowInfo.cxWindowBorders, (int)windowInfo.cyWindowBorders);
			} else {
				size = Size.Empty;
			}
			return result;
		}

		/// <summary>
		/// Return an Image representating the Window!
		/// As GDI+ draws it, it will be without Aero borders!
		/// </summary>
		public Bitmap PrintWindow(Rectangle requestedArea) {
			Rectangle clientRect = Rectangle.Empty;

			if (clientRect.IsEmpty) {
				GetClientRect(out clientRect);
			}
			// Start the capture
			bool printSucceeded = true;
			Bitmap returnImage = new Bitmap(clientRect.Width, clientRect.Height, PixelFormat.Format32bppArgb);
			using (Graphics graphics = Graphics.FromImage(returnImage)) {
				IntPtr hDCDest = graphics.GetHdc();
				printSucceeded = UnManagedMethods.PrintWindow(Handle, hDCDest, 0x0);
				graphics.ReleaseHdc(hDCDest);
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
				requestedArea = new Rectangle(size.Width, size.Height, clientRect.Width - (2 * size.Width), clientRect.Height - (2 * size.Height));
			}
			if (requestedArea != Rectangle.Empty) {
				Rectangle destRest = new Rectangle(Point.Empty, requestedArea.Size);
				Bitmap croppedImage = new Bitmap(requestedArea.Width, requestedArea.Height, returnImage.PixelFormat);
				using (Graphics graphics = Graphics.FromImage(croppedImage)) {
					graphics.DrawImage(returnImage, destRest, requestedArea, GraphicsUnit.Pixel);
				}
				Bitmap tmpImage = returnImage;
				returnImage = croppedImage;
				tmpImage.Dispose();
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
			IntPtr hWnd = UnManagedMethods.GetForegroundWindow();
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
			return new WindowDetails(UnManagedMethods.GetDesktopWindow());
		}
		
		/// <summary>
		/// Get all the top level windows
		/// </summary>
		/// <returns>List<WindowDetails> with all the top level windows</returns>
		public static List<WindowDetails> GetAllWindows() {
			List<WindowDetails> windows = new List<WindowDetails>();
			WindowsEnumerator windowsEnumerator = new WindowsEnumerator();
			windowsEnumerator.GetWindows();
			foreach(WindowDetails window in windowsEnumerator.Items) {
				windows.Add(window);
			}
			return windows;
		}
		
		/// <summary>
		/// Retrieve all top level windows with a certain title or classname
		/// </summary>
		/// <param name="titlePattern">The regexp to look for in the title</param>
		/// <param name="classnamePattern">The regexp to look for in the classname</param>
		/// <returns>List<WindowDetails> with all the found windows</returns>
		public static List<WindowDetails> FindWindow(string titlePattern, string classnamePattern) {
			return FindWindow(GetAllWindows(), titlePattern, classnamePattern);
		}
	}
	#endregion
}
