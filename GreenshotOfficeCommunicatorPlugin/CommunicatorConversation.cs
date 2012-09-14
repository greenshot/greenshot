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
using System.Text;
using CommunicatorAPI;
using System.Runtime.InteropServices;
using GreenshotPlugin.Core;

namespace GreenshotOfficeCommunicatorPlugin {
	public class CommunicatorConversation {
		private IMessengerConversationWndAdvanced imWindow = null;
		private long hwnd = 0;
		private Queue<string> queuedText = new Queue<string>();

		public CommunicatorConversation(long hwnd) {
			this.hwnd = hwnd;
		}

		public bool isActive {
			get {
				return imWindow != null;
			}
		}

		public IMessengerConversationWndAdvanced ImWindow {
			get {
				return imWindow;
			}
			set {
				imWindow = value;
				if (imWindow != null) {
					while (queuedText.Count > 0 && imWindow != null) {
						SendTextMessage(queuedText.Dequeue());
					}
				} else {
					hwnd = 0;
				}
			}
		}

		// Send text only if the window object matches the desired window handle
		public void SendTextMessage(string msg) {
			if (imWindow != null) {
				imWindow.SendText(msg);
			} else {
				queuedText.Enqueue(msg);
			}
		}

		public string History {
			get {
				return imWindow.History;
			}
		}

		public string Title {
			get {
				if (imWindow != null) {
					return new WindowDetails(new IntPtr(hwnd)).Text;
				}
				return null;
			}
		}
	}
}
