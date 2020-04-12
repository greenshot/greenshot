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
using GreenshotPlugin.Core;
using GreenshotPlugin.UnmanagedHelpers.Enums;

namespace GreenshotPlugin.Hooking
{
	/// <summary>
	/// Event arguments for the WindowOpenCloseEvent
	/// </summary>
	public class WindowOpenCloseEventArgs : EventArgs
	{
		public bool IsOpen { get; set; }
		/// <summary>
		/// HWnd of the window which has a changed title
		/// </summary>
		public IntPtr HWnd
		{
			get;
			set;
		}

		/// <summary>
		/// Title which is changed
		/// </summary>
		public string Title
		{
			get;
			set;
		}

		public string ClassName { get; set; }
	}
	/// <summary>
	/// Delegate for the window open close event
	/// </summary>
	/// <param name="eventArgs"></param>
	public delegate void WindowOpenCloseEventDelegate(WindowOpenCloseEventArgs eventArgs);

	/// <summary>
	/// Monitor all new and destroyed windows
	/// </summary>
	public sealed class WindowsOpenCloseMonitor : IDisposable
	{
		private WindowsEventHook _hook;
		private readonly object _lockObject = new object();
		// ReSharper disable once InconsistentNaming
		private event WindowOpenCloseEventDelegate _windowOpenCloseEvent;

		/// <summary>
		/// Add / remove event handler to the title monitor
		/// </summary>
		public event WindowOpenCloseEventDelegate WindowOpenCloseChangeEvent
		{
			add
			{
				lock (_lockObject)
				{
					if (_hook == null)
					{
						_hook = new WindowsEventHook();
						_hook.Hook(WinEvent.EVENT_OBJECT_CREATE, WinEvent.EVENT_OBJECT_DESTROY, WinEventHandler);
					}
					_windowOpenCloseEvent += value;
				}
			}
			remove
			{
				lock (_lockObject)
				{
					_windowOpenCloseEvent -= value;
					if (_windowOpenCloseEvent == null || _windowOpenCloseEvent.GetInvocationList().Length == 0)
					{
						if (_hook != null)
						{
							_hook.Dispose();
							_hook = null;
						}
					}
				}
			}
		}

		/// <summary>
		/// WinEventDelegate for the creation and destruction
		/// </summary>
		/// <param name="eventType"></param>
		/// <param name="hWnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		private void WinEventHandler(WinEvent eventType, IntPtr hWnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			if (hWnd == IntPtr.Zero || idObject != EventObjects.OBJID_WINDOW)
			{
				return;
			}
			if (eventType == WinEvent.EVENT_OBJECT_CREATE)
			{
				if (_windowOpenCloseEvent != null)
				{
					var windowsDetails = new WindowDetails(hWnd);
					_windowOpenCloseEvent(new WindowOpenCloseEventArgs { HWnd = hWnd, IsOpen = true, Title = windowsDetails.Text, ClassName = windowsDetails.ClassName });
				}
			}
			if (eventType == WinEvent.EVENT_OBJECT_DESTROY)
			{
				_windowOpenCloseEvent?.Invoke(new WindowOpenCloseEventArgs { HWnd = hWnd, IsOpen = false });
			}
		}

		private bool _disposedValue; // To detect redundant calls

		/// <summary>
		/// Dispose the underlying hook
		/// </summary>
		public void Dispose(bool disposing)
		{
			if (_disposedValue)
			{
				return;
			}
			lock (_lockObject)
			{
				_hook?.Dispose();
			}
			_disposedValue = true;
		}

		/// <summary>
		/// Make sure the finalizer disposes the underlying hook
		/// </summary>
		~WindowsOpenCloseMonitor()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		/// <summary>
		/// Dispose the underlying hook
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

}
