//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Windows.Forms;
using Dapplo.Windows.Enums;

#endregion

namespace Greenshot.Helpers
{
	/// <summary>
	///     This IMessageFilter filters out all WM_INPUTLANGCHANGEREQUEST messages which go to a handle which is >32 bits.
	///     The need for this is documented here: http://stackoverflow.com/a/32021586
	///     Unfortunately there is an error in the code example, should use HWnd instead of LParam for the handle.
	/// </summary>
	public class WmInputLangChangeRequestFilter : IMessageFilter
	{
		public bool PreFilterMessage(ref Message m)
		{
			if ((m.Msg == (int) WindowsMessages.WM_INPUTLANGCHANGEREQUEST) || (m.Msg == (int) WindowsMessages.WM_INPUTLANGCHANGE))
			{
				return m.LParam.ToInt64() > 0x7FFFFFFF;
			}
			return false;
		}
	}
}