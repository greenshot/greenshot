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
		
		private const int WM_HOTKEY = 0x312;
		public const int VK_SNAPSHOT = 0x2C;
		
		#region enums
		public enum Modifiers : int {
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
		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey (int hwnd, int id, int fsModifiers, int vk);
		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey (int hwnd, int id);
		#endregion
		
		public static bool RegisterHotKey(int hnd, int modifierKeyCode, int virtualKeyCode, HotKeyHandler handler) {
			if (RegisterHotKey(hnd, hotKeyCounter, modifierKeyCode, virtualKeyCode)) {
				keyHandlers.Add(hotKeyCounter, handler);
				hotKeyCounter++;
				return true;
			} else {
				LOG.Warn(String.Format("Couldn't register hotkey modifier {0} virtualKeyCode {1}", modifierKeyCode, virtualKeyCode));
				return false;
			}
		}
		
		public static void UnregisterHotkeys(int hnd) {
			foreach(int hotkey in keyHandlers.Keys) {
				UnregisterHotKey(hnd, hotkey);
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
