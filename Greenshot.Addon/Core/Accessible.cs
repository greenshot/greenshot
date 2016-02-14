/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using Accessibility;

namespace Greenshot.Addon.Core
{
	/// <summary>
	/// See: http://social.msdn.microsoft.com/Forums/en-US/ieextensiondevelopment/thread/03a8c835-e9e4-405b-8345-6c3d36bc8941
	/// This should really be cleaned up, there is little OO behind this class!
	/// Maybe move the basic Accessible functions to WindowDetails!?
	/// </summary>
	public class Accessible
	{
		#region Interop

		private static int AccessibleObjectFromWindow(IntPtr hWnd, OBJID idObject, ref IAccessible acc)
		{
			Guid guid = new Guid("{618736e0-3c3d-11cf-810c-00aa00389b71}"); // IAccessible
			object obj = null;
			int num = AccessibleObjectFromWindow(hWnd, (uint) idObject, ref guid, ref obj);
			acc = (IAccessible) obj;
			return num;
		}

		[DllImport("oleacc.dll")]
		private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

		[DllImport("oleacc.dll")]
		private static extern int AccessibleChildren(IAccessible paccContainer, int iChildStart, int cChildren, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] rgvarChildren, out int pcObtained);

		[DllImport("oleacc.dll", PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		public static extern object ObjectFromLresult(UIntPtr lResult, [MarshalAs(UnmanagedType.LPStruct)] Guid refiid, IntPtr wParam);

		#endregion

		private enum OBJID : uint
		{
			OBJID_WINDOW = 0x00000000,
		}

		private const int IE_ACTIVE_TAB = 2097154;
		private const int CHILDID_SELF = 0;
		private readonly IAccessible _accessible;

		private Accessible[] Children
		{
			get
			{
				int num;
				var res = GetAccessibleChildren(_accessible, out num);
				if (res == null)
				{
					return new Accessible[0];
				}

				var list = new List<Accessible>(res.Length);
				foreach (var obj in res)
				{
					var acc = obj as IAccessible;
					if (acc != null)
					{
						list.Add(new Accessible(acc));
					}
				}
				return list.ToArray();
			}
		}

		private string Name => _accessible.get_accName(CHILDID_SELF);

		private int ChildCount => _accessible.accChildCount;

		public Accessible(IntPtr hWnd)
		{
			AccessibleObjectFromWindow(hWnd, OBJID.OBJID_WINDOW, ref _accessible);
			if (_accessible == null)
			{
				throw new Exception();
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
							object tabIndex = tab._accessible.get_accState(0);

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

				if (captionList.Count > 0)
				{
					captionList.RemoveAt(captionList.Count - 1);
				}

				return captionList;
			}
		}


		public List<string> IETabUrls
		{
			get
			{
				var urlList = new List<string>();

				foreach (var accessor in Children)
				{
					foreach (var child in accessor.Children)
					{
						foreach (var tab in child.Children)
						{
							var description = tab._accessible.get_accDescription(CHILDID_SELF);
							if (!string.IsNullOrEmpty(description))
							{
								if (description.Contains(Environment.NewLine))
								{
									var url = description.Substring(description.IndexOf(Environment.NewLine, StringComparison.Ordinal)).Trim();
									urlList.Add(url);
								}
							}
						}
					}
				}

				return urlList;
			}
		}

		private Accessible(IAccessible acc)
		{
			if (acc == null)
			{
				throw new Exception();
			}
			_accessible = acc;
		}

		private void Activate()
		{
			_accessible.accDoDefaultAction(CHILDID_SELF);
		}

		private static object[] GetAccessibleChildren(IAccessible ao, out int childs)
		{
			childs = 0;
			object[] ret = null;
			int count = ao.accChildCount;

			if (count > 0)
			{
				ret = new object[count];
				AccessibleChildren(ao, 0, count, ret, out childs);
			}
			return ret;
		}
	}
}