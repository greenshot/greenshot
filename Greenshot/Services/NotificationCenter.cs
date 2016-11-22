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

using System.ComponentModel.Composition;
using Caliburn.Micro;
using Greenshot.Addon.Interfaces;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.Services
{
	/// <summary>
	///     This is the notification center, all uploads, save etc go through here
	///     If code needs to do something with this information, register the OnNotification.
	///     This can e.g. be used for Toasts in Windows 10
	/// </summary>
	[Export(typeof(INotificationCenter))]
	public class NotificationCenter : INotificationCenter
	{
		[Import]
		private IEventAggregator EventAggregator { get; set; }

		public void Notify(object sender, INotification notification)
		{
			EventAggregator.PublishOnUIThread(notification);
		}
	}
}