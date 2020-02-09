/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GreenshotPlugin.UnmanagedHelpers;

namespace GreenshotPlugin.Hooking
{
	/// <summary>
	/// The WinEventHook can register handlers to become important windows events
	/// This makes it possible to know a.o. when a window is created, moved, updated and closed.
	/// </summary>
	public class WindowsEventHook : IDisposable
	{
		private readonly WinEventDelegate _winEventHandler;
		private GCHandle _gcHandle;

		/// <summary>
		/// Used with Register hook
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="hwnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		public delegate void WinEventHandler(WinEvent eventType, IntPtr hwnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		/// <summary>
		/// Create a WindowsEventHook object
		/// </summary>
		public WindowsEventHook()
		{
			_winEventHandler = WinEventDelegateHandler;
			_gcHandle = GCHandle.Alloc(_winEventHandler);
		}

		[DllImport("user32", SetLastError = true)]
		private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
		[DllImport("user32", SetLastError = true)]
		private static extern IntPtr SetWinEventHook(WinEvent eventMin, WinEvent eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, int idProcess, int idThread, WinEventHookFlags dwFlags);

		/// <summary>
		/// Used with SetWinEventHook
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hwnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private delegate void WinEventDelegate(IntPtr hWinEventHook, WinEvent eventType, IntPtr hwnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		private readonly IDictionary<IntPtr, WinEventHandler> _winEventHandlers = new Dictionary<IntPtr, WinEventHandler>();

		/// <summary>
		/// Are hooks active?
		/// </summary>
		public bool IsHooked => _winEventHandlers.Count > 0;

		/// <summary>
		/// Hook a WinEvent
		/// </summary>
		/// <param name="winEvent"></param>
		/// <param name="winEventHandler"></param>
		/// <returns>true if success</returns>
		public void Hook(WinEvent winEvent, WinEventHandler winEventHandler)
		{
			Hook(winEvent, winEvent, winEventHandler);
		}

		/// <summary>
		/// Hook a WinEvent
		/// </summary>
		/// <param name="winEventStart"></param>
		/// <param name="winEventEnd"></param>
		/// <param name="winEventHandler"></param>
		public void Hook(WinEvent winEventStart, WinEvent winEventEnd, WinEventHandler winEventHandler)
		{
			var hookPtr = SetWinEventHook(winEventStart, winEventEnd, IntPtr.Zero, _winEventHandler, 0, 0, WinEventHookFlags.WINEVENT_SKIPOWNPROCESS | WinEventHookFlags.WINEVENT_OUTOFCONTEXT);
			_winEventHandlers.Add(hookPtr, winEventHandler);
		}

		/// <summary>
		/// Remove all hooks
		/// </summary>
		private void Unhook()
		{
			foreach (var hookPtr in _winEventHandlers.Keys)
			{
				if (hookPtr != IntPtr.Zero)
				{
					UnhookWinEvent(hookPtr);
				}
			}
			_winEventHandlers.Clear();
			_gcHandle.Free();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Unhook();
		}

		/// <summary>
		/// Call the WinEventHandler for this event
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventDelegateHandler(IntPtr hWinEventHook, WinEvent eventType, IntPtr hWnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			if (_winEventHandlers.TryGetValue(hWinEventHook, out var handler))
			{
				handler(eventType, hWnd, idObject, idChild, dwEventThread, dwmsEventTime);
			}
		}

	}

}
