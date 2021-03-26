﻿/*
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

using GreenshotPlugin.UnmanagedHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace GreenshotPlugin.Core {
    /// <summary>
	/// EnumWindows wrapper for .NET
	/// </summary>
	public class WindowsEnumerator {
		/// <summary>
		/// Returns the collection of windows returned by GetWindows
		/// </summary>
		public IList<WindowDetails> Items { get; private set; }

        /// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="hWndParent">Window Handle to get children for</param>
		/// <param name="classname">Window Classname to copy, use null to copy all</param>
		public WindowsEnumerator GetWindows(IntPtr hWndParent, string classname) {
			Items = new List<WindowDetails>();
			User32.EnumChildWindows(hWndParent, WindowEnum, IntPtr.Zero);

			bool hasParent = !IntPtr.Zero.Equals(hWndParent);
			string parentText = null;
			if (hasParent) {
				var title = new StringBuilder(260, 260);
				User32.GetWindowText(hWndParent, title, title.Capacity);
				parentText = title.ToString();
			}

            var windows = new List<WindowDetails>();
			foreach (var window in Items) {
				if (hasParent) {
					window.Text = parentText;
					window.ParentHandle = hWndParent;
				}
				if (classname == null || window.ClassName.Equals(classname)) {
					windows.Add(window);
				}
			}
			Items = windows;
			return this;
		}

        /// <summary>
		/// The enum Windows callback.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <param name="lParam">Application defined value</param>
		/// <returns>1 to continue enumeration, 0 to stop</returns>
		private int WindowEnum(IntPtr hWnd, int lParam)
        {
            return OnWindowEnum(hWnd) ? 1 : 0;
        }

        /// <summary>
		/// Called whenever a new window is about to be added
		/// by the Window enumeration called from GetWindows.
		/// If overriding this function, return true to continue
		/// enumeration or false to stop.  If you do not call
		/// the base implementation the Items collection will
		/// be empty.
		/// </summary>
		/// <param name="hWnd">Window handle to add</param>
		/// <returns>True to continue enumeration, False to stop</returns>
        private bool OnWindowEnum(IntPtr hWnd) {
			if (!WindowDetails.IsIgnoreHandle(hWnd)) {
				Items.Add(new WindowDetails(hWnd));
			}
			return true;
		}
	}
}
