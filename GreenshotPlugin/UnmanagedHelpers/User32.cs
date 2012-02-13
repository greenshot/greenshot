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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace GreenshotPlugin.UnmanagedHelpers {
	public delegate int EnumWindowsProc(IntPtr hwnd, int lParam);

	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct POINT {
		public int X;
		public int Y;

		public POINT(int x, int y) {
			this.X = x;
			this.Y = y;
		}
		public POINT(Point point) {
			this.X = point.X;
			this.Y = point.Y;
		}

		public static implicit operator System.Drawing.Point(POINT p) {
			return new System.Drawing.Point(p.X, p.Y);
		}

		public static implicit operator POINT(System.Drawing.Point p) {
			return new POINT(p.X, p.Y);
		}

		public Point ToPoint() {
			return new Point(X, Y);
		}

		override public string ToString() {
			return X +","+Y;
		}
	}

	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct RECT {
		private int _Left;
		private int _Top;
		private int _Right;
		private int _Bottom;
	
		public RECT(RECT rectangle) : this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom) {
		}
		public RECT(Rectangle rectangle) : this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom) {
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

	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct WindowInfo {
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
		public WindowInfo(Boolean ? filler) : this() {
			cbSize = (UInt32)(Marshal.SizeOf(typeof( WindowInfo )));
		}
	}

	/// <summary>
	/// Contains information about the placement of a window on the screen.
	/// </summary>
	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct WindowPlacement {
		/// <summary>
		/// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
		/// <para>
		/// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
		/// </para>
		/// </summary>
		public int Length;
		
		/// <summary>
		/// Specifies flags that control the position of the minimized window and the method by which the window is restored.
		/// </summary>
		public WindowPlacementFlags Flags;
		
		/// <summary>
		/// The current show state of the window.
		/// </summary>
		public ShowWindowCommand ShowCmd;
		
		/// <summary>
		/// The coordinates of the window's upper-left corner when the window is minimized.
		/// </summary>
		public POINT MinPosition;
		
		/// <summary>
		/// The coordinates of the window's upper-left corner when the window is maximized.
		/// </summary>
		public POINT MaxPosition;
		
		/// <summary>
		/// The window's coordinates when the window is in the restored position.
		/// </summary>
		public RECT NormalPosition;

		/// <summary>
		/// Gets the default (empty) value.
		/// </summary>
		public static WindowPlacement Default {
			get {
				WindowPlacement result = new WindowPlacement();
				result.Length = Marshal.SizeOf( result );
				return result;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CursorInfo {
		public Int32 cbSize;
		public Int32 flags;
		public IntPtr hCursor;
		public POINT ptScreenPos;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct IconInfo {
		public bool fIcon;
		public Int32 xHotspot;
		public Int32 yHotspot;
		public IntPtr hbmMask;
		public IntPtr hbmColor;
	}
	
	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct SCROLLINFO {
		public int cbSize;
		public int fMask;
		public int nMin;
		public int nMax;
		public int nPage;
		public int nPos;
		public int nTrackPos;
	}

	/// <summary>
	/// User32 Wrappers
	/// </summary>
	public class User32 {
		public const int WM_COMMAND = 0x111;
		public const int WM_SYSCOMMAND = 0x112;
			
		public const int SC_RESTORE = 0xF120;
		public const int SC_CLOSE = 0xF060;
		public const int SC_MAXIMIZE = 0xF030;
		public const int SC_MINIMIZE = 0xF020;

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
			
		public const Int32 CURSOR_SHOWING = 0x00000001;
				
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool IsWindowVisible(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out IntPtr processId);
		[DllImport("user32", SetLastError = true, ExactSpelling=true, CharSet=CharSet.Auto)]
		public static extern IntPtr GetParent(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCommand uCmd);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
		public extern static int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
		public extern static int GetWindowTextLength(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		public static extern uint GetSysColor(int nIndex);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool BringWindowToTop(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetDesktopWindow();
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement lpwndpl);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool IsIconic(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool IsZoomed(IntPtr hwnd);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
		public extern static int GetClassName (IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);
		[DllImport("user32", SetLastError=true)]
		public extern static int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
		[DllImport("user32", SetLastError=true, EntryPoint = "SendMessageA")]
		public static extern bool SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
		[DllImport("user32", SetLastError=true)]
		public extern static uint GetWindowLong(IntPtr hwnd, int index);
		[DllImport("user32", SetLastError = true)]
		public static extern int SetWindowLong(IntPtr hWnd, int index, uint styleFlags);
		[DllImport("user32", EntryPoint="GetWindowLongPtr", SetLastError=true)]
		public extern static IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowInfo pwi);
		[DllImport("user32", SetLastError = true)]
		public extern static int EnumWindows(EnumWindowsProc lpEnumFunc, int lParam);
		[DllImport("user32", SetLastError = true)]
		public extern static int EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, int lParam);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowScrollBar(IntPtr hwnd, ScrollBarDirection scrollBar, bool show);
		[DllImport("user32", SetLastError = true)]
		public static extern int SetScrollPos(IntPtr hWnd, System.Windows.Forms.Orientation nBar, int nPos, bool bRedraw);
		[DllImport("user32", SetLastError=true, EntryPoint = "PostMessageA")]
		public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
		[DllImport("user32", SetLastError = true)]
		public static extern RegionResult GetWindowRgn(IntPtr hWnd, IntPtr hRgn);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetWindowDC(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr ReleaseDC(IntPtr hWnd,IntPtr hDC);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, WindowPos uFlags);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetTopWindow(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("user32", SetLastError = true)]
		public static extern void ReleaseDC(IntPtr dc);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetClipboardOwner();
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

		[DllImport("user32", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
		
		/// uiFlags: 0 - Count of GDI objects
		/// uiFlags: 1 - Count of USER objects
		/// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
		/// - Win32 USER objects:
		///	- 	WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
		/// - Other USER objects (windows, menus)
		///
		[DllImport("user32", SetLastError = true)]
		public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

		public static uint GetGuiResourcesGDICount() {
			return GetGuiResources(Process.GetCurrentProcess().Handle, 0);
		}

		public static uint GetGuiResourcesUserCount() {
			return GetGuiResources(Process.GetCurrentProcess().Handle, 1);
		}

		[DllImport("user32", EntryPoint = "RegisterWindowMessageA", SetLastError = true)]
		public static extern uint RegisterWindowMessage(string lpString);
		[DllImport("user32", SetLastError=true, CharSet=CharSet.Auto)]
		public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out UIntPtr lpdwResult);

		/// <summary>
		/// Helper method to create a Win32 exception with the windows message in it
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <returns>Exception</returns>
		public static Exception CreateWin32Exception(string method) {
			Win32Exception exceptionToThrow = new Win32Exception();
 			exceptionToThrow.Data.Add("Method", method);
			return exceptionToThrow;
		}
		[DllImport("user32", SetLastError = true)]
		public static extern bool GetPhysicalCursorPos(out POINT cursorLocation);

		[DllImport("user32", SetLastError=true)]
		public static extern int MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref POINT lpPoints, [MarshalAs(UnmanagedType.U4)] int cPoints);

		#region icon
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr CopyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		public static extern bool GetCursorInfo(out CursorInfo cursorInfo);

		[DllImport("user32", SetLastError = true)]
		public static extern bool GetIconInfo(IntPtr hIcon, out IconInfo iconInfo);
		
		[DllImport("user32", SetLastError = true)]
		public static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetCapture(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReleaseCapture();

		[DllImport("user32", SetLastError = true)]
		public static extern int GetSystemMetrics(SystemMetric index);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr CreateIconIndirect(ref IconInfo icon);


		#endregion
	}
}
