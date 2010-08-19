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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Greenshot.Plugin;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of HotkeyHelper.
	/// </summary>
	public class HotkeyHelper {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(HotkeyHelper));
		
		private const uint WM_HOTKEY = 0x312;
		public const uint VK_SNAPSHOT = 0x2C;
		
		#region enums
		public enum Modifiers : uint {
			NONE = 0,
			ALT = 1,
			CTRL = 2,
			SHIFT = 4,
			WIN = 8
		}
		#endregion

		// Holds the list of hotkeys
		private static Dictionary<int, HotKeyHandler> keyHandlers = new Dictionary<int, HotKeyHandler>();
		private static int hotKeyCounter = 0;

		private HotkeyHelper() {
		}

		#region User32
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint virtualKeyCode);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
		#endregion
		
		/// <summary>
		/// Register a hotkey
		/// </summary>
		/// <param name="hWnd">The window which will get the event</param>
		/// <param name="modifierKeyCode">The modifier, e.g.: Modifiers.CTRL, Modifiers.NONE or Modifiers.ALT</param>
		/// <param name="virtualKeyCode">The virtual key code</param>
		/// <param name="handler">A HotKeyHandler, this will be called to handle the hotkey press</param>
		/// <returns></returns>
		public static bool RegisterHotKey(IntPtr hWnd, uint modifierKeyCode, uint virtualKeyCode, HotKeyHandler handler) {
			if (RegisterHotKey(hWnd, hotKeyCounter, modifierKeyCode, virtualKeyCode)) {
				keyHandlers.Add(hotKeyCounter, handler);
				hotKeyCounter++;
				return true;
			} else {
				LOG.Warn(String.Format("Couldn't register hotkey modifier {0} virtualKeyCode {1}", modifierKeyCode, virtualKeyCode));
				return false;
			}
		}
		
		public static void UnregisterHotkeys(IntPtr hWnd) {
			foreach(int hotkey in keyHandlers.Keys) {
				UnregisterHotKey(hWnd, hotkey);
			}
			// Remove all key handlers
			keyHandlers.Clear();
		}
		
		public static void HandleMessages(ref Message m) {
			if (m.Msg == WM_HOTKEY) {
				// Call handler
				keyHandlers[(int)m.WParam]();
			}
		}

	}
}
