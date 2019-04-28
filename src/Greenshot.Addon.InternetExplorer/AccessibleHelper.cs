// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Accessibility;

namespace Greenshot.Addon.InternetExplorer
{
	/// <summary>
	///     See:
	///     http://social.msdn.microsoft.com/Forums/en-US/ieextensiondevelopment/thread/03a8c835-e9e4-405b-8345-6c3d36bc8941
	///     This should really be cleaned up, there is little OO behind this class!
	///     Maybe move the basic Accessible functions to WindowDetails!?
	/// </summary>
	public class Accessible
	{
		private const int IE_ACTIVE_TAB = 2097154;
		private const int CHILDID_SELF = 0;
		private readonly IAccessible accessible;

		public Accessible(IntPtr hWnd)
		{
			AccessibleObjectFromWindow(hWnd, OBJID.OBJID_WINDOW, ref accessible);
			if (accessible == null)
			{
				throw new Exception();
			}
		}

		private Accessible(IAccessible accessible)
		{
		    this.accessible = accessible ?? throw new ArgumentNullException(nameof(accessible));
		}

		private Accessible[] Children
		{
			get
			{
			    var res = GetAccessibleChildren(accessible, out _);
				if (res == null)
				{
					return new Accessible[0];
				}

				var list = new List<Accessible>(res.Length);
				foreach (var obj in res)
				{
				    if (obj is IAccessible localAccessible)
					{
						list.Add(new Accessible(localAccessible));
					}
				}
				return list.ToArray();
			}
		}

		private string Name => accessible.get_accName(CHILDID_SELF);

	    private int ChildCount => accessible.accChildCount;

	    /// <summary>
        /// Returns the URL for the active tab
        /// </summary>
		public string IEActiveTabUrl
		{
			get
			{
				foreach (var accessor in Children)
				{
					foreach (var child in accessor.Children)
					{
						foreach (var tab in child.Children)
						{
							var tabIndex = tab.accessible.get_accState(CHILDID_SELF);

						    if ((int) tabIndex != IE_ACTIVE_TAB)
						    {
						        continue;
						    }

						    var description = tab.accessible.get_accDescription(CHILDID_SELF);

						    if (string.IsNullOrEmpty(description))
						    {
						        continue;
						    }

						    if (description.Contains(Environment.NewLine))
						    {
						        var url = description.Substring(description.IndexOf(Environment.NewLine)).Trim();
						        return url;
						    }
						}
					}
				}
				return string.Empty;
			}
		}

        /// <summary>
        /// Get the index of the active tab
        /// </summary>
		public int IEActiveTabIndex
		{
			get
			{
				var index = 0;
				foreach (var accessor in Children)
				{
					foreach (var child in accessor.Children)
					{
						foreach (var tab in child.Children)
						{
							var tabIndex = tab.accessible.get_accState(0);

							if ((int) tabIndex == IE_ACTIVE_TAB)
							{
								return index;
							}
							index++;
						}
					}
				}
				return -1;
			}
		}

        /// <summary>
        /// Get the caption of the active tab
        /// </summary>
		public string IEActiveTabCaption
		{
			get
			{
				foreach (var accessor in Children)
				{
					foreach (var child in accessor.Children)
					{
						foreach (var tab in child.Children)
						{
							var tabIndex = tab.accessible.get_accState(0);

							if ((int) tabIndex == IE_ACTIVE_TAB)
							{
								return tab.Name;
							}
						}
					}
				}
				return string.Empty;
			}
		}

        /// <summary>
        /// Get the captions of all tabs
        /// </summary>
		public List<string> IETabCaptions
		{
			get
			{
				var captionList = new List<string>();

				foreach (var accessor in Children)
				{
					foreach (var child in accessor.Children)
					{
						foreach (var tab in child.Children)
						{
							captionList.Add(tab.Name);
						}
					}
				}

				// TODO: Why again?
				if (captionList.Count > 0)
				{
					captionList.RemoveAt(captionList.Count - 1);
				}

				return captionList;
			}
		}

        /// <summary>
        /// Get the urls of all tabs
        /// </summary>
		public IEnumerable<string> IETabUrls
		{
			get
			{
				foreach (var accessor in Children)
				{
					foreach (var child in accessor.Children)
					{
						foreach (var tab in child.Children)
						{
							var tabIndex = tab.accessible.get_accState(CHILDID_SELF);
							var description = tab.accessible.get_accDescription(CHILDID_SELF);
						    if (string.IsNullOrEmpty(description))
						    {
						        continue;
						    }

						    if (!description.Contains(Environment.NewLine))
						    {
						        continue;
						    }

						    var url = description.Substring(description.IndexOf(Environment.NewLine)).Trim();
						    yield return url;
						}
					}
				}
			}
		}

        /// <summary>
        /// Count the tabs
        /// </summary>
		public int IETabCount
		{
			get
			{
				foreach (var accessor in Children)
				{
					foreach (var child in accessor.Children)
					{
						foreach (var tab in child.Children)
						{
							return child.ChildCount - 1;
						}
					}
				}
				return 0;
			}
		}

        /// <summary>
        /// Activate the specified tab
        /// </summary>
        /// <param name="tabCaptionToActivate">string</param>
		public void ActivateIETab(string tabCaptionToActivate)
		{
			foreach (var accessor in Children)
			{
				foreach (var child in accessor.Children)
				{
					foreach (var tab in child.Children)
					{
					    if (tab.Name != tabCaptionToActivate)
					    {
					        continue;
					    }

					    tab.Activate();
					    return;
					}
				}
			}
		}

        /// <summary>
        /// Close the specified tabs
        /// </summary>
        /// <param name="tabCaptionToClose">string</param>
		public void CloseIETab(string tabCaptionToClose)
		{
			foreach (var accessor in Children)
			{
				foreach (var child in accessor.Children)
				{
					foreach (var tab in child.Children)
					{
					    if (tab.Name != tabCaptionToClose)
					    {
					        continue;
					    }

					    foreach (var  CloseTab in tab.Children)
					    {
					        CloseTab.Activate();
					    }
					    return;
					}
				}
			}
		}

        /// <summary>
        /// Active the IE tab
        /// </summary>
        /// <param name="tabIndexToActivate">int</param>
		public void ActivateIETab(int tabIndexToActivate)
		{
			var index = 0;
			foreach (var accessor in Children)
			{
				foreach (var child in accessor.Children)
				{
					foreach (var tab in child.Children)
					{
						if (tabIndexToActivate >= child.ChildCount - 1)
						{
							return;
						}

						if (index == tabIndexToActivate)
						{
							tab.Activate();
							return;
						}
						index++;
					}
				}
			}
		}

		private void Activate()
		{
			accessible.accDoDefaultAction(CHILDID_SELF);
		}

		private static object[] GetAccessibleChildren(IAccessible ao, out int childs)
		{
			childs = 0;
			object[] ret = null;
			var count = ao.accChildCount;

			if (count > 0)
			{
				ret = new object[count];
				AccessibleChildren(ao, 0, count, ret, out childs);
			}
			return ret;
		}

		private enum OBJID : uint
		{
			OBJID_WINDOW = 0x00000000
		}

        private static int AccessibleObjectFromWindow(IntPtr hWnd, OBJID idObject, ref IAccessible acc)
		{
			var guid = new Guid("{618736e0-3c3d-11cf-810c-00aa00389b71}"); // IAccessible
			object obj = null;
			var num = AccessibleObjectFromWindow(hWnd, (uint) idObject, ref guid, ref obj);
			acc = (IAccessible) obj;
			return num;
		}

		[DllImport("oleacc.dll")]
		private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid, [In] [Out] [MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

		[DllImport("oleacc.dll")]
		private static extern int AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren,
			[In] [Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren, out int pcObtained);

		[DllImport("oleacc.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public static extern object ObjectFromLresult(UIntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);
    }
}