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

        public static DestinationHolder Instance { get; internal set; }

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
