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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using log4net;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.UnmanagedHelpers.Enums;
using GreenshotPlugin.UnmanagedHelpers.Structs;

namespace GreenshotPlugin.UnmanagedHelpers {
    /// <summary>
	/// User32 Wrappers
	/// </summary>
	public static class User32 {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(User32));
		private static bool _CanCallGetPhysicalCursorPos = true;
		public const int SC_RESTORE = 0xF120;
		public const int SC_MAXIMIZE = 0xF030;
		public const int SC_MINIMIZE = 0xF020;

        // For MonitorFromWindow
		public const int MONITOR_DEFAULTTONULL = 0;
		public const int MONITOR_DEFAULTTONEAREST = 2;
		public const int CURSOR_SHOWING = 0x00000001;

		/// <summary>
		/// Determines whether the specified window handle identifies an existing window.
		/// </summary>
		/// <param name="hWnd">A handle to the window to be tested.</param>
		/// <returns>
		/// If the window handle identifies an existing window, the return value is true.
		/// If the window handle does not identify an existing window, the return value is false.
		/// </returns>
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindowVisible(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
        [DllImport("user32", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);
        [DllImport("user32")]
        public static extern IntPtr AttachThreadInput(int idAttach, int idAttachTo, int fAttach);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int cch);

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
		public static extern bool IsIconic(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsZoomed(IntPtr hWnd);
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetClassName (IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
		[DllImport("user32", SetLastError = true)]
		public static extern uint GetClassLong(IntPtr hWnd, int nIndex);
		[DllImport("user32", SetLastError = true, EntryPoint = "GetClassLongPtr")]
		public static extern IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PrintWindow(IntPtr hWnd, IntPtr hDc, uint nFlags);
		[DllImport("user32", CharSet=CharSet.Unicode, SetLastError=true)]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLong")]
		public static extern int GetWindowLong(IntPtr hWnd, int index);
		[DllImport("user32", SetLastError = true, EntryPoint = "GetWindowLongPtr")]
		public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
		[DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int SetWindowLong(IntPtr hWnd, int index, int styleFlags);
		[DllImport("user32", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
		public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr styleFlags);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr MonitorFromWindow(IntPtr hWnd, MonitorFrom dwFlags);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowInfo(IntPtr hWnd, ref WindowInfo pwi);

        [DllImport("user32", SetLastError = true)]
		public static extern int EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32", SetLastError = true)]
		public static extern RegionResult GetWindowRgn(IntPtr hWnd, SafeHandle hRgn);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, WindowPos uFlags);

        [DllImport("user32", SetLastError = true)]
		public static extern IntPtr GetClipboardOwner();

        // Added for finding Metro apps, Greenshot 1.1
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

		/// uiFlags: 0 - Count of GDI objects
		/// uiFlags: 1 - Count of USER objects
		/// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
		/// - Win32 USER objects:
		///	- 	WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
		/// - Other USER objects (windows, menus)
		///
		[DllImport("user32", SetLastError = true)]
		public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern uint RegisterWindowMessage(string lpString);
		[DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out UIntPtr lpdwResult);
		[DllImport("user32", SetLastError = true)]
		private static extern bool GetPhysicalCursorPos(out POINT cursorLocation);

        /// <summary>
		/// The following is used for Icon handling
		/// </summary>
		/// <param name="hIcon"></param>
		/// <returns></returns>
		[DllImport("user32", SetLastError = true)]
		public static extern SafeIconHandle CopyIcon(IntPtr hIcon);
		[DllImport("user32", SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);
		[DllImport("user32", SetLastError = true)]
		public static extern bool GetCursorInfo(out CursorInfo cursorInfo);
		[DllImport("user32", SetLastError = true)]
		public static extern bool GetIconInfo(SafeIconHandle iconHandle, out IconInfo iconInfo);
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr SetCapture(IntPtr hWnd);
		[DllImport("user32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReleaseCapture();
		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

		[DllImport("user32", SetLastError = true)]
		public static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, DesktopAccessRight dwDesiredAccess);
		[DllImport("user32", SetLastError = true)]
		public static extern bool SetThreadDesktop(IntPtr hDesktop);
		[DllImport("user32", SetLastError = true)]
		public static extern bool CloseDesktop(IntPtr hDesktop);

        /// <summary>
		/// Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7.
		/// </summary>
		/// <returns>Point with cursor location, relative to the origin of the monitor setup
		/// (i.e. negative coordinates arepossible in multiscreen setups)</returns>
		public static Point GetCursorLocation() {
			if (Environment.OSVersion.Version.Major >= 6 && _CanCallGetPhysicalCursorPos) {
                try {
					if (GetPhysicalCursorPos(out var cursorLocation)) {
						return new Point(cursorLocation.X, cursorLocation.Y);
					}

                    Win32Error error = Win32.GetLastErrorCode();
                    LOG.ErrorFormat("Error retrieving PhysicalCursorPos : {0}", Win32.GetMessage(error));
                } catch (Exception ex) {
					LOG.Error("Exception retrieving PhysicalCursorPos, no longer calling this. Cause :", ex);
					_CanCallGetPhysicalCursorPos = false;
				}
			}
			return new Point(Cursor.Position.X, Cursor.Position.Y);
		}

		/// <summary>
		/// Wrapper for the GetClassLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hWnd">IntPtr</param>
		/// <param name="nIndex">int</param>
		/// <returns>IntPtr</returns>
		public static IntPtr GetClassLongWrapper(IntPtr hWnd, int nIndex) {
			if (IntPtr.Size > 4) {
				return GetClassLongPtr(hWnd, nIndex);
			}  else {
				return new IntPtr(GetClassLong(hWnd, nIndex));
			}
		}

		/// <summary>
		/// Wrapper for the GetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="nIndex"></param>
		/// <returns></returns>
		public static long GetWindowLongWrapper(IntPtr hWnd, int nIndex) {
			if (IntPtr.Size == 8) {
				return GetWindowLongPtr(hWnd, nIndex).ToInt64();
			} else {
				return GetWindowLong(hWnd, nIndex);
			}
		}

		/// <summary>
		/// Wrapper for the SetWindowLong which decides if the system is 64-bit or not and calls the right one.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="nIndex"></param>
		/// <param name="styleFlags"></param>
		public static void SetWindowLongWrapper(IntPtr hWnd, int nIndex, IntPtr styleFlags) {
			if (IntPtr.Size == 8) {
				SetWindowLongPtr(hWnd, nIndex, styleFlags);
			} else {
				SetWindowLong(hWnd, nIndex, styleFlags.ToInt32());
			}
		}

		public static uint GetGuiResourcesGDICount()
        {
            using var currentProcess = Process.GetCurrentProcess();
            return GetGuiResources(currentProcess.Handle, 0);
        }

		public static uint GetGuiResourcesUserCount()
        {
            using var currentProcess = Process.GetCurrentProcess();
            return GetGuiResources(currentProcess.Handle, 1);
        }

		/// <summary>
		/// Helper method to create a Win32 exception with the windows message in it
		/// </summary>
		/// <param name="method">string with current method</param>
		/// <returns>Exception</returns>
		public static Exception CreateWin32Exception(string method) {
			var exceptionToThrow = new Win32Exception();
			exceptionToThrow.Data.Add("Method", method);
			return exceptionToThrow;
		}
	}
}
