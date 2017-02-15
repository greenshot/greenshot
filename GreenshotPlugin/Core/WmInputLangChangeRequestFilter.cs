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

using System.Windows.Forms;
using Dapplo.Windows.Enums;
using log4net;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     This IMessageFilter filters out all WM_INPUTLANGCHANGEREQUEST messages which go to a handle which is >32 bits.
	///     The need for this is documented here: http://stackoverflow.com/a/32021586
	/// </summary>
	public class WmInputLangChangeRequestFilter : IMessageFilter
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(WmInputLangChangeRequestFilter));

		/// <summary>
		///     This will do some filtering
		/// </summary>
		/// <param name="m">Message</param>
		/// <returns>true if the message should be filtered</returns>
		public bool PreFilterMessage(ref Message m)
		{
			return PreFilterMessageExternal(ref m);
		}

		/// <summary>
		///     Also used in the MainForm WndProc
		/// </summary>
		/// <param name="m">Message</param>
		/// <returns>true if the message should be filtered</returns>
		public static bool PreFilterMessageExternal(ref Message m)
		{
			var message = (WindowsMessages) m.Msg;
			if (message == WindowsMessages.WM_INPUTLANGCHANGEREQUEST || message == WindowsMessages.WM_INPUTLANGCHANGE)
			{
				Log.WarnFormat("Filtering: {0}, {1:X} - {2:X} - {3:X}", message, m.LParam.ToInt64(), m.WParam.ToInt64(), m.HWnd.ToInt64());
				// For now we always return true
				return true;
				// But it could look something like this:
				//return (m.LParam.ToInt64() | 0x7FFFFFFF) != 0;
			}
			return false;
		}
	}
}