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

namespace Greenshot.Destinations
{
	/// <summary>
	/// The PickerDestination shows a context menu with all possible destinations, so the user can "pick" one
	/// </summary>
	[Destination("Picker")]
	public class PickerDestination : AbstractDestination
	{
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

		public ExportFactory<ExportWindow> ExportWindowFactory
		{
			get;
			set;
		} 

		[Import(AllowRecomposition = true)]
		public IEnumerable<Lazy<IDestination, IDestinationMetadata>> Destinations
		{
			get;
			set;
		}

		public PickerDestination()
		{
			Text = GreenshotLanguage.SettingsDestinationPicker;
			Export = async capture => await ShowExport(capture);
		}

		private async Task<ExportInformation> ShowExport(ICapture capture)
		{
			using (var exportWindow = ExportWindowFactory.CreateExport())
			{
				foreach (var destination in Destinations.Where(destination => destination.Metadata.Name != "Picker"))
				{
					exportWindow.Value.Children.Add(destination.Value);
				}
				if (exportWindow.Value.ShowDialog() == true)
				{
					return await exportWindow.Value.SelectedDestination.Export(capture);
				}
			}
			return null;
		}
	}
}