/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Text;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// Which destinations (and plugins) can be used: the check boxes of the Destinations and Plugins tabs write the user's
    /// ExcludeDestinations / ExcludePluginNames, and the administrator settings show them as lines for greenshot-fixed.ini.
    /// </summary>
    public partial class SettingsViewModel
    {
        /// <summary>
        /// greenshot-fixed.ini doesn't let the user switch destinations off
        /// </summary>
        public bool AreDestinationExclusionsFixed => ConfigurationRestrictions.AreDestinationExclusionsFixed(CoreConfiguration);

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
        /// Add a destination by its name, e.g. of a plugin which isn't installed: unchecked to block it, checked to allow it
        /// </summary>
        /// <returns>false when the name is empty or already listed</returns>
        public bool AddDestinationName(string designation, bool isAllowed)
        {
            designation = designation?.Trim();
            if (string.IsNullOrEmpty(designation) || string.Equals(designation, nameof(WellKnownDestinations.Picker), StringComparison.OrdinalIgnoreCase)
                || Destinations.Any(item => string.Equals(item.Designation, designation, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            AddUnregisteredDestination(designation).IsAllowed = isAllowed;
            return true;
        }

        private DestinationViewModel AddUnregisteredDestination(string designation)
        {
            var item = new DestinationViewModel
            {
                Designation = designation,
                Description = designation,
                IsAllowed = ConfigurationRestrictions.IsDestinationAllowed(CoreConfiguration, designation),
                CanChangeAllowed = !AreDestinationExclusionsFixed && !ConfigurationRestrictions.IsDestinationBlockedByAdministrator(CoreConfiguration, designation)
            };
            Destinations.Add(item);
            return item;
        }

        /// <summary>
        /// Write the check boxes the user can change to the exclude lists, entries which aren't listed (e.g. of a plugin which isn't installed) stay
        /// </summary>
        public void SaveRestrictions()
        {
            if (!AreDestinationExclusionsFixed && Destinations != null)
            {
                var changeable = Destinations.Where(item => item.CanChangeAllowed).ToList();
                CoreConfiguration.ExcludeDestinations = ConfigurationRestrictions.ReplaceListed(CoreConfiguration.ExcludeDestinations,
                    changeable.Select(item => item.Designation),
                    changeable.Where(item => !item.IsAllowed).Select(item => item.Designation));
            }
#if !GREENSHOT_LIGHT
            SavePluginRestrictions();
#endif
        }

        /// <summary>
        /// The lines for greenshot-fixed.ini with the current check boxes
        /// </summary>
        /// <param name="allowOnlyChecked">true: only what is checked is allowed, everything else (also unknown names) is blocked; false: what isn't checked is blocked</param>
        /// <param name="noUserExclusions">true fixes the user's exclude lists empty, the user can't switch anything off</param>
        /// <param name="noUnsignedQuestion">true: a plugin which isn't signed like Greenshot.exe only loads when its hash is approved here</param>
        public string BuildAdministratorSettings(bool allowOnlyChecked, bool noUserExclusions, bool noUnsignedQuestion)
        {
            var text = new StringBuilder();
            text.AppendLine("[Core]");
#if !GREENSHOT_LIGHT
            AppendPluginRestrictions(text, allowOnlyChecked);
            if (noUserExclusions)
            {
                AppendList(text, nameof(ICoreConfiguration.ExcludePluginNames), Enumerable.Empty<string>());
            }

            // The plugins approved on this PC, which aren't signed like Greenshot.exe
            var approvedHashes = ConfigurationRestrictions.Names(CoreConfiguration.PluginApprovedHashes).ToList();
            if (approvedHashes.Count > 0)
            {
                AppendList(text, nameof(ICoreConfiguration.PluginApprovedHashes), approvedHashes);
            }

            if (noUnsignedQuestion)
            {
                text.AppendLine($"{nameof(ICoreConfiguration.AskForUnsignedPlugins)}=False");
            }
#endif
            if (allowOnlyChecked)
            {
                // The picker has no check box, it stays available
                AppendList(text, nameof(ICoreConfiguration.AllowedDestinations),
                    Destinations.Where(item => item.IsAllowed).Select(item => item.Designation).Append(nameof(WellKnownDestinations.Picker)));
            }
            else
            {
                AppendList(text, nameof(ICoreConfiguration.BlockedDestinations), Destinations.Where(item => !item.IsAllowed).Select(item => item.Designation));
            }

            if (noUserExclusions)
            {
                AppendList(text, nameof(ICoreConfiguration.ExcludeDestinations), Enumerable.Empty<string>());
            }

            return text.ToString();
        }

        private static void AppendList(StringBuilder text, string key, IEnumerable<string> values) => text.AppendLine($"{key}={string.Join(",", values)}");
    }
}
