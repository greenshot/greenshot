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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Addons.Components
{
    /// <summary>
    /// This holds the destinations, so other components can easily retrieve them
    /// </summary>
    public class DestinationHolder
    {
        private readonly IEnumerable<Lazy<IDestination, DestinationAttribute>> _destinations;
        private readonly IEnumerable<IDestinationProvider> _destinationProviders;

        /// <summary>
        /// This is the Instance of the DestinationHolder, used for some cases where DI doesn't work
        /// </summary>
        public static DestinationHolder Instance { get; internal set; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="destinations">IEnumerable with lazy IDestination</param>
        /// <param name="destinationProviders">IEnumerable with IDestinationProvider</param>
        public DestinationHolder(
            IEnumerable<Lazy<IDestination, DestinationAttribute>> destinations,
            IEnumerable<IDestinationProvider> destinationProviders
            )
        {
            Instance = this;
            _destinations = destinations;
            _destinationProviders = destinationProviders;
        }

        /// <summary>
        /// Return an IEnumerable with sorted IDestination entries
        /// </summary>
        public IEnumerable<IDestination> SortedActiveDestinations => _destinationProviders
                .SelectMany(provider => provider.Provide())
                .Concat(_destinations)
                .Where(destination => destination.Value.IsActive)
                .OrderBy(destination => destination.Metadata.Priority)
                .ThenBy(destination => destination.Value.Description)
                .Select(d => d.Value);

        /// <summary>
        /// Return an IEnumerable with all destinations
        /// </summary>
        public IEnumerable<Lazy<IDestination, DestinationAttribute>> AllDestinations => _destinationProviders
            .SelectMany(provider => provider.Provide())
            .Concat(_destinations);
    }
}
