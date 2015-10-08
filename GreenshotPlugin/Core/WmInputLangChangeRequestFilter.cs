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

using GreenshotPlugin.UnmanagedHelpers;
using System.Windows.Forms;
using log4net;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// This IMessageFilter filters out all WM_INPUTLANGCHANGEREQUEST messages which go to a handle which is >32 bits.
	/// The need for this is documented here: http://stackoverflow.com/a/32021586
	/// Unfortunately there is an error in the code example, should use HWnd instead of LParam for the handle.
	/// </summary>
	public class WmInputLangChangeRequestFilter : IMessageFilter
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(WmInputLangChangeRequestFilter));

		public bool PreFilterMessage(ref Message m)
		{
			WindowsMessages message = (WindowsMessages)m.Msg;
			if (message == WindowsMessages.WM_INPUTLANGCHANGEREQUEST || message == WindowsMessages.WM_INPUTLANGCHANGE)
			{
				LOG.WarnFormat("Filtering: {0}, {1:X} - {2:X} - {3:X}", message, m.LParam.ToInt64(), m.WParam.ToInt64(), m.HWnd.ToInt64());
				return (m.LParam.ToInt64() | 0xFFFFFFFF) != 0;
			}
			return false;
		}
	}
}