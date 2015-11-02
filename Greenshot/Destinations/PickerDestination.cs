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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Greenshot.Windows;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using System.Threading;

namespace Greenshot.Destinations
{
	/// <summary>
	/// The PickerDestination shows a context menu with all possible destinations, so the user can "pick" one
	/// </summary>
	[Destination(_pickerDesignation)]
	public class PickerDestination : AbstractDestination
	{
		private const string _pickerDesignation = "Picker";

		[Import]
		public ICoreConfiguration CoreConfiguration
		{
			get;
			set;
		}

		[Import]
		public IGreenshotLanguage GreenshotLanguage
		{
			get;
			set;
		}

		[Import]
		public ExportFactory<ExportWindow> ExportWindowFactory
		{
			get;
			set;
		} 

		[ImportMany(AllowRecomposition = true)]
		public IEnumerable<Lazy<IDestination, IDestinationMetadata>> Destinations
		{
			get;
			set;
		}

		public override string Designation
		{
			get
			{
				return _pickerDesignation;
			}
		}
		public override string Text
		{
			get
			{
				return GreenshotLanguage.SettingsDestinationPicker;
			}

			set
			{
			}
		}

		public PickerDestination()
		{
			Export = async (capture, token) => await ShowExport(capture, token);
		}

		private async Task<INotification> ShowExport(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			using (var exportWindow = ExportWindowFactory.CreateExport())
			{
				foreach (var destination in Destinations.Where(destination => destination.Metadata.Name != _pickerDesignation))
				{
					exportWindow.Value.Children.Add(destination.Value);
				}
				if (exportWindow.Value.ShowDialog() == true)
				{
					return await exportWindow.Value.SelectedDestination.Export(capture, token);
				}
			}
			return new Notification
			{
				NotificationType = NotificationTypes.Cancel,
				Source = _pickerDesignation,
				SourceType = SourceTypes.Destination,
				Text = "Cancelled"
			};
		}
	}
}