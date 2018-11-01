#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#if !NETCOREAPP30
#region Usings

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Accessibility;
using Dapplo.Log;

#endregion

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
		private static readonly LogSource Log = new LogSource();
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
				int num;
				var res = GetAccessibleChildren(accessible, out num);
				if (res == null)
				{
					return new Accessible[0];
				}

				var list = new List<Accessible>(res.Length);
				foreach (var obj in res)
				{
					var accessible = obj as IAccessible;
					if (accessible != null)
					{
						list.Add(new Accessible(accessible));
					}
				}
				return list.ToArray();
			}
		}

		private string Name
		{
			get { return accessible.get_accName(CHILDID_SELF); }
		}

		private int ChildCount
		{
			get { return accessible.accChildCount; }
		}

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

							if ((int) tabIndex == IE_ACTIVE_TAB)
							{
								var description = tab.accessible.get_accDescription(CHILDID_SELF);

								if (!string.IsNullOrEmpty(description))
								{
									if (description.Contains(Environment.NewLine))
									{
										var url = description.Substring(description.IndexOf(Environment.NewLine)).Trim();
										return url;
									}
								}
							}
						}
					}
				}
				return string.Empty;
			}
		}

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
							if (!string.IsNullOrEmpty(description))
							{
								if (description.Contains(Environment.NewLine))
								{
									var url = description.Substring(description.IndexOf(Environment.NewLine)).Trim();
									yield return url;
								}
							}
						}
					}
				}
			}
		}

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

		public void ActivateIETab(string tabCaptionToActivate)
		{
			foreach (var accessor in Children)
			{
				foreach (var child in accessor.Children)
				{
					foreach (var tab in child.Children)
					{
						if (tab.Name == tabCaptionToActivate)
						{
							tab.Activate();
							return;
						}
					}
				}
			}
		}

		public void CloseIETab(string tabCaptionToClose)
		{
			foreach (var accessor in Children)
			{
				foreach (var child in accessor.Children)
				{
					foreach (var tab in child.Children)
					{
						if (tab.Name == tabCaptionToClose)
						{
							foreach (var  CloseTab in tab.Children)
							{
								CloseTab.Activate();
							}
							return;
						}
					}
				}
			}
		}

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

		#region Interop

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

		#endregion
	}
}
#endif