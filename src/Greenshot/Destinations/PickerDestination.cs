/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;

namespace Greenshot.Destinations
{
    /// <summary>
    /// The PickerDestination shows a context menu with all possible destinations, so the user can "pick" one
    /// </summary>
    public class PickerDestination : AbstractDestination
    {
        public override string Designation => nameof(WellKnownDestinations.Picker);

        public override string Description => Language.GetString(LangKey.settings_destination_picker);

        public override int Priority => 1;

        private readonly Func<IDestination, bool> _isNotPickerPredicate = (destination) => !destination.Designation.Equals(nameof(WellKnownDestinations.Picker));

        /// <summary>
        /// Export the capture with the destination picker
        /// </summary>
        /// <param name="manuallyInitiated">Did the user select this destination?</param>
        /// <param name="surface">Surface to export</param>
        /// <param name="captureDetails">Details of the capture</param>
        /// <returns>true if export was made</returns>
        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            List<IDestination> destinations = DestinationHelper.GetAllDestinations(true)
                .Where(_isNotPickerPredicate)
                .ToList();

            // No Processing, this is done in the selected destination (if anything was selected)
            return ShowPickerMenu(true, surface, captureDetails, destinations);
        }
    }
}