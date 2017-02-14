using System;
using System.Collections.Generic;
using System.Text;
using Dapplo.Windows.Native;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// EnumWindows wrapper for .NET
	/// </summary>
	public class WindowsEnumerator {
		/// <summary>
		/// Returns the collection of windows returned by GetWindows
		/// </summary>
		public IList<WindowDetails> Items { get; private set; }

		/// <summary>
		/// Gets all top level windows on the system.
		/// </summary>
		public WindowsEnumerator GetWindows() {
			GetWindows(IntPtr.Zero, null);
			return this;
		}

		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="parent">Window Handle to get children for</param>
		public WindowsEnumerator GetWindows(WindowDetails parent)
		{
			GetWindows(parent?.Handle ?? IntPtr.Zero, null);
			return this;
		}

		/// <summary>
		/// Gets all child windows of the specified window
		/// </summary>
		/// <param name="hWndParent">Window Handle to get children for</param>
		/// <param name="classname">Window Classname to copy, use null to copy all</param>
		public WindowsEnumerator GetWindows(IntPtr hWndParent, string classname) {
			Items = new List<WindowDetails>();
			IList<WindowDetails> windows = new List<WindowDetails>();
			User32.EnumChildWindows(hWndParent, WindowEnum, IntPtr.Zero);

			bool hasParent = !IntPtr.Zero.Equals(hWndParent);
			string parentText = null;
			if (hasParent) {
				var title = new StringBuilder(260, 260);
				User32.GetWindowText(hWndParent, title, title.Capacity);
				parentText = title.ToString();
			}

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

		#region EnumWindows callback
		/// <summary>
		/// The enum Windows callback.
		/// </summary>
		/// <param name="hWnd">Window Handle</param>
		/// <param name="lParam">Application defined value</param>
		/// <returns>1 to continue enumeration, 0 to stop</returns>
		private bool WindowEnum(IntPtr hWnd, IntPtr lParam)
		{
			return OnWindowEnum(hWnd);
		}

		#endregion

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