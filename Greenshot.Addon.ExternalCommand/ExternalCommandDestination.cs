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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;
using MahApps.Metro.IconPacks;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
	[Destination("ExternalCommand")]
	[PartNotDiscoverable]
	public sealed class ExternalCommandDestination : AbstractDestination
	{
		private readonly CommandSettings _settings;

		public ExternalCommandDestination(CommandSettings settings)
		{
			_settings = settings;
		}

		[Import]
		private IExternalCommandConfiguration ExternalCommandConfiguration { get; set; }

		[Import]
		private IExternalCommandLanguage ExternalCommandLanguage { get; set; }

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = _settings.Name,
				NotificationSourceType = NotificationSourceTypes.Destination,
				Text = $"Exported to {_settings.Name}"
			};

			var outputSettings = new SurfaceOutputSettings(_settings.Format);

			string fullPath = capture.CaptureDetails.Filename;
			if (fullPath == null)
			{
				fullPath = capture.SaveNamedTmpFile(capture.CaptureDetails, outputSettings);
			}

			await ProcessStarter.CallExternalCommandAsync(_settings, returnValue, fullPath, token);
			return returnValue;
		}

		/// <summary>
		///     Setup
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = _settings.Name;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = _settings.Name;
			// TODO: Set Icon

			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.App
			};

			//Icon = PluginUtils.GetCachedExeIcon(_settings.Commandline, 0).ToBitmapSource();
		}
	}
}