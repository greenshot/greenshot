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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Core.Interfaces
{
	/// <summary>
	/// This interface describes the flow a capture takes: from source, via processor, to destination
	/// The flow itself takes care of calling the CaptureSource, CaptureProcessor and CaptureDestination inside the ExecuteAsync
	/// </summary>
	public interface ICaptureFlow : ICaptureModule
	{
		/// <summary>
		/// The current state
		/// </summary>
		CaptureFlowStates State { get; }

		/// <summary>
		/// Execute the capture flow.
		/// </summary>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		Task ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Progress of the flow
		/// </summary>
		IProgress<int> Progress { get; set; }

		/// <summary>
		/// This is the notification center which should be used by the destinations
		/// </summary>
		INotificationCenter NotificationCenter { get; }

		/// <summary>
		/// Defines the CaptureSource, which takes the capture
		/// </summary>
		ICaptureSource CaptureSource { get; set; }

		/// <summary>
		/// Defines the CaptureProcessor, which optionally processes the capture
		/// </summary>
		ICaptureProcessor CaptureProcessor { get; set; }

		/// <summary>
		/// Defines the CaptureDestination, which exports the capture
		/// </summary>
		ICaptureDestination CaptureDestination { get; set; }

		/// <summary>
		/// Contains the capture of this flow
		/// </summary>
		ICapture Capture { get; set; }
	}
}
