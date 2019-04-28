// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;

namespace Greenshot.Destinations
{
    /// <summary>
    ///     The PickerDestination shows a context menu with all possible destinations, so the user can "pick" one
    /// </summary>
    [Destination("Picker", DestinationOrder.Picker)]
    public class PickerDestination : AbstractDestination
	{
	    private readonly DestinationHolder _destinationHolder;

	    public override string Description => GreenshotLanguage.SettingsDestinationPicker;

        public PickerDestination(
	        DestinationHolder destinationHolder,
            ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage
            ) : base(coreConfiguration, greenshotLanguage)
        {
            _destinationHolder = destinationHolder;
        }

        /// <summary>
        ///     Export the capture with the destination picker
        /// </summary>
        /// <param name="manuallyInitiated">Did the user select this destination?</param>
        /// <param name="surface">Surface to export</param>
        /// <param name="captureDetails">Details of the capture</param>
        /// <returns>true if export was made</returns>
        protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var pickerDestinations = new List<IDestination>();

            if (CoreConfiguration.PickerDestinations.Any())
		    {
		        foreach (var outputDestination in CoreConfiguration.PickerDestinations)
		        {
		            var pickerDestination = _destinationHolder.SortedActiveDestinations
		                .FirstOrDefault(destination => outputDestination == destination.Designation);

		            if (pickerDestination != null)
		            {
		                pickerDestinations.Add(pickerDestination);
		            }
		        }
		    }
		    else
		    {
		        foreach (var pickerDestination in _destinationHolder.SortedActiveDestinations
                    .Where(destination => !"Picker".Equals(destination.Designation)))
		        {
		            pickerDestinations.Add(pickerDestination);
		        }
		    }
            // No Processing, this is done in the selected destination (if anything was selected)
            return ShowPickerMenu(true, surface, captureDetails, pickerDestinations);
		}
	}
}