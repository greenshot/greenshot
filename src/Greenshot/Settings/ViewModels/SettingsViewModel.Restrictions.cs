/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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


using System.Collections.Generic;
using System.Linq;
using System.Text;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// Which destinations (and plugins) can be used: the check boxes of the Destinations and Plugins tabs write
    /// ExcludeDestinations / ExcludePluginNames, and the administrator settings show them as lines for greenshot-fixed.ini.
    /// </summary>
    public partial class SettingsViewModel
    {
        /// <summary>
        /// greenshot-fixed.ini decides which destinations are available
        /// </summary>
        public bool AreDestinationsFixed => ConfigurationRestrictions.AreDestinationsFixed(CoreConfiguration);

        /// <summary>
        /// The registered destinations which are available, or only excluded by the configuration, without the picker
        /// </summary>
        private List<IDestination> GetListedDestinations() =>
            (SimpleServiceProvider.Current.GetAllInstances<IDestination>() ?? Enumerable.Empty<IDestination>())
                .Where(destination => destination != null && (!ConfigurationRestrictions.IsDestinationAllowed(CoreConfiguration, destination.Designation) || IsAvailable(destination)))
                .OrderBy(destination => destination, DestinationComparer.Instance)
                .ToList();

        private static bool IsAvailable(IDestination destination)
        {
            try
            {
                return destination.IsAvailableFor(null);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Write the check boxes to the include / exclude lists, entries which aren't listed (e.g. of a plugin which isn't installed) stay
        /// </summary>
        public void SaveRestrictions()
        {
            if (!AreDestinationsFixed && Destinations != null)
            {
                var listed = Destinations.Select(item => item.Destination.Designation).ToList();
                CoreConfiguration.ExcludeDestinations = ConfigurationRestrictions.ReplaceListed(CoreConfiguration.ExcludeDestinations, listed,
                    Destinations.Where(item => !item.IsAllowed).Select(item => item.Destination.Designation));
                if (ConfigurationRestrictions.Names(CoreConfiguration.IncludeDestinations).Any())
                {
                    CoreConfiguration.IncludeDestinations = ConfigurationRestrictions.ReplaceListed(CoreConfiguration.IncludeDestinations, listed,
                        Destinations.Where(item => item.IsAllowed).Select(item => item.Destination.Designation));
                }
            }
#if !GREENSHOT_LIGHT
            SavePluginRestrictions();
#endif
        }

        /// <summary>
        /// The lines for greenshot-fixed.ini with the current check boxes
        /// </summary>
        /// <param name="onlyAllowChecked">true writes the include lists, which also block what is added later</param>
        public string BuildAdministratorSettings(bool onlyAllowChecked)
        {
            var text = new StringBuilder();
            text.AppendLine("[Core]");
#if !GREENSHOT_LIGHT
            AppendPluginRestrictions(text, onlyAllowChecked);
#endif
            // Both lists for "only allow": an empty include list allows everything
            var listed = Destinations.Select(item => item.Destination.Designation).ToList();
            var excluded = Destinations.Where(item => !item.IsAllowed).Select(item => item.Destination.Designation);
            AppendList(text, nameof(ICoreConfiguration.ExcludeDestinations), ConfigurationRestrictions.ReplaceListed(CoreConfiguration.ExcludeDestinations, listed, excluded));
            if (onlyAllowChecked)
            {
                // The picker has no check box, it stays available
                var allowed = Destinations.Where(item => item.IsAllowed).Select(item => item.Destination.Designation)
                    .Append(nameof(WellKnownDestinations.Picker));
                AppendList(text, nameof(ICoreConfiguration.IncludeDestinations), ConfigurationRestrictions.ReplaceListed(CoreConfiguration.IncludeDestinations, listed, allowed));
            }

            return text.ToString();
        }

        private static void AppendList(StringBuilder text, string key, IEnumerable<string> values) => text.AppendLine($"{key}={string.Join(",", values)}");
    }
}
