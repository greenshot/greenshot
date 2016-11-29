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
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Implementations
{
	/// <summary>
	/// This implements the most basic capture flow
	/// </summary>
	public class SimpleCaptureFlow : ICaptureFlow
	{
		/// <inheritdoc />
		public CaptureFlowStates State { get; private set; } = CaptureFlowStates.Init;


		/// <summary>
		/// A very simple implementation, just call the optional CaptureSource, CaptureProcessor and CaptureDestination
		/// </summary>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public async Task ExecuteAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			try
			{
				if (CaptureSource != null)
				{
					State = CaptureFlowStates.TakingCapture;
					await CaptureSource.TakeCaptureAsync(this, cancellationToken);
				}

				if (CaptureProcessor != null)
				{
					State = CaptureFlowStates.ProcessingCapture;
					await CaptureProcessor.ProcessCaptureAsync(this, cancellationToken);
				}

				if (CaptureDestination != null)
				{
					State = CaptureFlowStates.ExportingCapture;
					await CaptureDestination.ExportCaptureAsync(this, cancellationToken);
				}
				State = CaptureFlowStates.Success;

				// Inform the supplied NotificationCenter of success
				NotificationCenter?.Notify(this, CreateSuccessNotification());
			}
			catch (Exception ex)
			{
				State = CaptureFlowStates.Failed;
				// Inform the supplied NotificationCenter of fail
				NotificationCenter?.Notify(this, CreateFailedNotification(ex));
			}

		}

		/// <summary>
		/// Build a notification for a fail
		/// </summary>
		/// <param name="exception">Exception</param>
		/// <returns>INotification</returns>
		private INotification CreateFailedNotification(Exception exception)
		{
			var notification = new Notification
			{
				ErrorText = exception.Message,
				NotificationType = exception is OperationCanceledException ? NotificationTypes.Cancel : NotificationTypes.Fail
			};

			switch (State)
			{
				case CaptureFlowStates.Init:
					notification.NotificationSourceType = NotificationSourceTypes.CaptureFlow;
					notification.Source = Name;
					break;
				case CaptureFlowStates.TakingCapture:
					notification.NotificationSourceType = NotificationSourceTypes.CaptureSource;
					notification.Source = CaptureSource.Name;
					break;
				case CaptureFlowStates.ProcessingCapture:
					notification.NotificationSourceType = NotificationSourceTypes.CaptureProcessor;
					notification.Source = CaptureProcessor.Name;
					break;
				case CaptureFlowStates.ExportingCapture:
					notification.NotificationSourceType = NotificationSourceTypes.CaptureDestination;
					notification.Source = CaptureDestination.Name;
					break;
			}
			return notification;
		}

		/// <summary>
		/// Build an INotification for success
		/// </summary>
		/// <returns>INotification</returns>
		private INotification CreateSuccessNotification()
		{
			var notification = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = Name,
				NotificationSourceType = NotificationSourceTypes.CaptureFlow
			};
			return notification;
		}


		/// <inheritdoc />
		public IProgress<int> Progress { get; set; }

		/// <inheritdoc />
		public INotificationCenter NotificationCenter { get; set; }

		/// <inheritdoc />
		public ICaptureSource CaptureSource { get; set; }

		/// <inheritdoc />
		public ICaptureProcessor CaptureProcessor { get; set; }

		/// <inheritdoc />
		public ICaptureDestination CaptureDestination { get; set; }

		/// <inheritdoc />
		public ICapture Capture { get; set; }

		/// <inheritdoc />
		public string Name { get; set; }
	}
}
