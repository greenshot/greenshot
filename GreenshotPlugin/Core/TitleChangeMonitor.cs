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

using GreenshotPlugin.UnmanagedHelpers;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GreenshotPlugin.Core {

	/// <summary>
	/// Event arguments for the TitleChangeEvent
	/// </summary>
	public class TitleChangeEventArgs : EventArgs {
		public IntPtr HWnd {
			get;
			set;
		}
		public string Title {
			get;
			set;
		}
	}

	/// <summary>
	/// Delegate for the title change event
	/// </summary>
	/// <param name="e"></param>
	public delegate void TitleChangeEventDelegate(object sender, TitleChangeEventArgs e);

	/// <summary>
	/// Monitor all title changes
	/// </summary>
	public class TitleChangeMonitor : IDisposable {
		private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;

		private WinEventDelegate _winEventHandler;
		private GCHandle _gcHandle;
		private IntPtr _hook;

		public event TitleChangeEventDelegate TitleChangeEvent;

		public TitleChangeMonitor() {
			if (_winEventHandler == null) {
				_winEventHandler = WinEventHandler;
				_gcHandle = GCHandle.Alloc(_winEventHandler);
			}
			_hook = User32.SetWinEventHook(WinEvent.EVENT_OBJECT_NAMECHANGE, WinEvent.EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, _winEventHandler, 0, 0, WinEventHookFlags.WINEVENT_SKIPOWNPROCESS | WinEventHookFlags.WINEVENT_OUTOFCONTEXT);
		}

		/// <summary>
		/// WinEventDelegate for the creation & destruction
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventHandler(IntPtr hWinEventHook, WinEvent eventType, IntPtr hWnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
			if (hWnd == IntPtr.Zero || idObject != EventObjects.OBJID_WINDOW) {
				return;
			}
			if (eventType == WinEvent.EVENT_OBJECT_NAMECHANGE) {
				string newTitle = GetText(hWnd);
				if (TitleChangeEvent != null) {
					TitleChangeEvent(this, new TitleChangeEventArgs { HWnd = hWnd, Title = newTitle });
				}
			}
		}

		/// <summary>
		/// Retrieve the windows title, also called Text
		/// </summary>
		/// <param name="hWnd">IntPtr for the window</param>
		/// <returns>string</returns>
		private static string GetText(IntPtr hWnd) {
			StringBuilder title = new StringBuilder(260, 260);
			int length = User32.GetWindowText(hWnd, title, title.Capacity);
			if (length == 0) {
				return null;
			}
			return title.ToString();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// free managed resources
					if (_hook != IntPtr.Zero)
					{
						User32.UnhookWinEvent(_hook);
					}
				}
				// free native resources if there are any.
				if (_gcHandle != null)
				{
					_gcHandle.Free();
					_gcHandle = default(GCHandle);
				}
				disposedValue = true;
			}
		}

		~TitleChangeMonitor() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

	}
}
