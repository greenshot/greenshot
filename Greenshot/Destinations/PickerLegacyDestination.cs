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

using System.Collections.Generic;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Dapplo.Config.Ini;
using log4net;
using System.Threading.Tasks;
using System.Threading;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;

namespace Greenshot.Destinations
{
	/// <summary>
	/// The PickerDestination shows a context menu with all possible destinations, so the user can "pick" one
	/// </summary>
	public class PickerLegacyDestination : AbstractLegacyDestination
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (PickerLegacyDestination));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();

		public override string Designation
		{
			get
			{
				return BuildInDestinationEnum.Picker.ToString();
			}
		}

		public override string Description
		{
			get
			{
				return language.SettingsDestinationPicker;
			}
		}

		public override int Priority
		{
			get
			{
				return 1;
			}
		}


		/// <summary>
		/// Export the capture with the destination picker
		/// </summary>
		/// <param name="manuallyInitiated">Did the user select this destination?</param>
		/// <param name="surface">Surface to export</param>
		/// <param name="captureDetails">Details of the capture</param>
		/// <returns>true if export was made</returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			IList<ILegacyDestination> destinations = new List<ILegacyDestination>();
			foreach (ILegacyDestination destination in DestinationHelper.GetAllDestinations())
			{
				if ("Picker".Equals(destination.Designation))
				{
					continue;
				}
				if (!destination.IsActive)
				{
					continue;
				}
				destinations.Add(destination);
			}

			// No Processing, this is done in the selected destination (if anything was selected)
			return await ShowPickerMenuAsync(true, surface, captureDetails, destinations, token);
		}
	}
}