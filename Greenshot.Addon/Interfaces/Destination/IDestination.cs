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

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

#endregion

namespace Greenshot.Addon.Interfaces.Destination
{
	public interface IDestination : INotifyPropertyChanged
	{
		/// <summary>
		///     This is a collection of child destinations, shown in the destination picker
		/// </summary>
		ObservableCollection<IDestination> Children { get; }

		/// <summary>
		///     This is the technical name of the destination, used for excluding or storing the configuration
		/// </summary>
		string Designation { get; }

		/// <summary>
		///     Export a capture
		/// </summary>
		Func<IExportContext, ICapture, CancellationToken, Task<INotification>> Export { get; }

		/// <summary>
		///     This is the icon which is shown everywhere where the destination can be seen.
		///     Two known locations are the settings and the destination picker.
		/// </summary>
		Control Icon { get; }

		/// <summary>
		///     When set to false, the entry is disabled in the destination picker
		/// </summary>
		bool IsEnabled { get; }

		/// <summary>
		///     If the entry needs a shortcut in the destination picker, it can be set with this value
		/// </summary>
		string Shortcut { get; }

		/// <summary>
		///     This is the name of the destination in the settings and destination picker
		/// </summary>
		string Text { get; }

		/// <summary>
		///     This will be called before the item is shown, so it can update it's children etc.
		/// </summary>
		/// <param name="caller">IExportContext</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		Task RefreshAsync(IExportContext caller, CancellationToken cancellationToken = default(CancellationToken));
	}
}