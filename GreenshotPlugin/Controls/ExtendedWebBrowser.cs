#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Windows.Forms;
using GreenshotPlugin.Interop;

#endregion

namespace GreenshotPlugin.Controls
{
	public class ExtendedWebBrowser : WebBrowser
	{
		protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
		{
			return new ExtendedWebBrowserSite(this);
		}

		protected class ExtendedWebBrowserSite : WebBrowserSite, IOleCommandTarget
		{
			private const int OLECMDID_SHOWSCRIPTERROR = 40;
			private const int OLECMDID_SHOWMESSAGE = 41;

			private const int S_OK = 0;
			private const int OLECMDERR_E_NOTSUPPORTED = -2147221248;
			private const int OLECMDERR_E_UNKNOWNGROUP = -2147221244;

			private static readonly Guid CGID_DocHostCommandHandler = new Guid("F38BC242-B950-11D1-8918-00C04FC2C836");

			public ExtendedWebBrowserSite(WebBrowser wb) : base(wb)
			{
			}

			#region IOleCommandTarget Members

			public int QueryStatus(Guid pguidCmdGroup, int cCmds, IntPtr prgCmds, IntPtr pCmdText)
			{
				return OLECMDERR_E_NOTSUPPORTED;
			}

			public int Exec(Guid pguidCmdGroup, int nCmdID, int nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
			{
				if (pguidCmdGroup == CGID_DocHostCommandHandler)
				{
					if (nCmdID == OLECMDID_SHOWSCRIPTERROR)
					{
						// do not need to alter pvaOut as the docs says, enough to return S_OK here
						return S_OK;
					}
				}

				return OLECMDERR_E_NOTSUPPORTED;
			}

			#endregion
		}
	}
}