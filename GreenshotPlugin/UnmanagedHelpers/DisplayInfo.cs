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

using System;
using System.Collections.Generic;
using System.Drawing;

namespace GreenshotPlugin.UnmanagedHelpers
{
	/// <summary>
	/// The DisplayInfo class is like the Screen class, only not cached.
	/// </summary>
	public class DisplayInfo
	{
		private const uint MONITORINFOF_PRIMARY = 1;

		public bool IsPrimary
		{
			get;
			set;
		}

		public int ScreenHeight
		{
			get;
			set;
		}

		public int ScreenWidth
		{
			get;
			set;
		}

		public Rectangle Bounds
		{
			get;
			set;
		}

		public Rectangle WorkingArea
		{
			get;
			set;
		}

		public string DeviceName
		{
			get;
			set;
		}

		/// <summary>
		/// Returns the number of Displays using the Win32 functions
		/// </summary>
		/// <returns>collection of Display Info</returns>
		public static IList<DisplayInfo> AllDisplays()
		{
			var result = new List<DisplayInfo>();
			User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
			{
				var monitorInfoEx = new MonitorInfoEx();
				monitorInfoEx.Init();
				bool success = User32.GetMonitorInfo(hMonitor, ref monitorInfoEx);
				if (success)
				{
					DisplayInfo displayInfo = new DisplayInfo();
					displayInfo.ScreenWidth = Math.Abs(monitorInfoEx.Monitor.Right - monitorInfoEx.Monitor.Left);
					displayInfo.ScreenHeight = Math.Abs(monitorInfoEx.Monitor.Bottom - monitorInfoEx.Monitor.Top);
					displayInfo.Bounds = monitorInfoEx.Monitor.ToRectangle();
					displayInfo.WorkingArea = monitorInfoEx.WorkArea.ToRectangle();
					displayInfo.IsPrimary = (monitorInfoEx.Flags | MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY;
					result.Add(displayInfo);
				}
				return true;
			}, IntPtr.Zero);
			return result;
		}

		/// <summary>
		/// Implementation like Screen.GetBounds
		/// https://msdn.microsoft.com/en-us/library/6d7ws9s4(v=vs.110).aspx
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public static Rectangle GetBounds(Point point)
		{
			DisplayInfo returnValue = null;
			foreach (var display in AllDisplays())
			{
				if (display.IsPrimary && returnValue == null)
				{
					returnValue = display;
				}
				if (display.Bounds.Contains(point))
				{
					returnValue = display;
				}
			}
			return returnValue.Bounds;
		}
	}
}