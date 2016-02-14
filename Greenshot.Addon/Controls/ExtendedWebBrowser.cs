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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Greenshot.Addon.Controls
{
	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComVisible(true), Guid("B722BCCB-4E68-101B-A2BC-00AA00404770")]
	public interface IOleCommandTarget
	{
		[return: MarshalAs(UnmanagedType.I4)]
		[PreserveSig]
		int QueryStatus([In, MarshalAs(UnmanagedType.LPStruct)] Guid pguidCmdGroup, int cCmds, IntPtr prgCmds, IntPtr pCmdText);

		[return: MarshalAs(UnmanagedType.I4)]
		[PreserveSig]
		int Exec([In, MarshalAs(UnmanagedType.LPStruct)] Guid pguidCmdGroup, int nCmdId, int nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut);
	}

	public class ExtendedWebBrowser : WebBrowser
	{
		protected class ExtendedWebBrowserSite : WebBrowserSite, IOleCommandTarget
		{
			private const int OleCmdIdShowScriptError = 40;

			private static readonly Guid GuidDocHostCommandHandler = new Guid("F38BC242-B950-11D1-8918-00C04FC2C836");

			private const int Ok = 0;
			private const int OleCmdErrorNotsupported = (-2147221248);

			public ExtendedWebBrowserSite(WebBrowser wb) : base(wb)
			{
			}

			#region IOleCommandTarget Members

			public int QueryStatus(Guid pguidCmdGroup, int cCmds, IntPtr prgCmds, IntPtr pCmdText)
			{
				return OleCmdErrorNotsupported;
			}

			public int Exec(Guid pguidCmdGroup, int nCmdId, int nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
			{
				if (pguidCmdGroup == GuidDocHostCommandHandler)
				{
					if (nCmdId == OleCmdIdShowScriptError)
					{
						// do not need to alter pvaOut as the docs says, enough to return S_OK here
						return Ok;
					}
				}

				return OleCmdErrorNotsupported;
			}

			#endregion
		}

		protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
		{
			return new ExtendedWebBrowserSite(this);
		}
	}
}