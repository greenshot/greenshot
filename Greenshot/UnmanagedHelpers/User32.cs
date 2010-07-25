/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.InteropServices;
using System.Text;

namespace Greenshot.UnmanagedHelpers {
	/// <summary>
	/// User32 Wrappers
	/// </summary>
	public class User32 {
		#region Structures and constants
		public const Int32 CURSOR_SHOWING = 0x00000001;
		
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT {
			public Int32 x;
			public Int32 y;
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
		
		#endregion
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr ReleaseDC(IntPtr hWnd,IntPtr hDC);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();
		
		[DllImport("User32.dll", SetLastError = true)]
		public static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("User32.dll", SetLastError = true)]
		public static extern void ReleaseDC(IntPtr dc);

		[DllImport("User32.dll", SetLastError = true)]
   		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);
   
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetClipboardOwner();
		
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool AttachConsole(uint dwProcessId);
		public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;  // default value if not specifing a process ID
		
		[DllImport("kernel32")]
		public static extern bool AllocConsole();
		
		/// uiFlags: 0 - Count of GDI objects
	    /// uiFlags: 1 - Count of USER objects
	    /// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
	    /// - Win32 USER objects:
	    ///      - WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
	    /// - Other USER objects (windows, menus)
	    ///
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

		public static uint GetGuiResourcesGDICount() {
		    return GetGuiResources(Process.GetCurrentProcess().Handle, 0);
		}

		public static uint GetGuiResourcesUserCount() {
		    return GetGuiResources(Process.GetCurrentProcess().Handle, 1);
		}
		
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

        #region icon
		[DllImport("user32.dll")]
		public static extern IntPtr CopyIcon(IntPtr hIcon);

		[DllImport("user32.dll")]
		public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CursorInfo cursorInfo);

        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon, out IconInfo iconInfo);
        #endregion
    }
}
