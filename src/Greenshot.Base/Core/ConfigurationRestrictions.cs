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
using System.IO;
using System.Linq;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Which plugins are loaded and which destinations are available. The administrator blocks with the Blocked and Include lists in
    /// greenshot-fixed.ini, the user switches more off with the Exclude lists, see docs/vNext/plugins-and-destinations.md.
    /// </summary>
    public static class ConfigurationRestrictions
    {
        private const string PluginFilePrefix = "Greenshot.Plugin.";

        /// <summary>
        /// The plugins Greenshot ships, so they can be blocked before they are installed
        /// </summary>
        public static IReadOnlyList<string> ShippedPluginNames { get; } = new[]
        {
            "Box", "Confluence", "CustomUploader", "Dropbox", "ExternalCommand", "Jira", "Office", "Pdf", "RecipeEditor", "Zxing"
        };

        /// <summary>
        /// The identifiers IncludePlugins and ExcludePlugins used (AssemblyPluginIdentifierAttribute), other values were already the name
        /// </summary>
        private static readonly Dictionary<string, string> LegacyPluginIdentifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Box Plugin"] = "Box",
            ["Confluence Plugin"] = "Confluence",
            ["Custom uploader Plugin"] = "CustomUploader",
            ["Dropbox Plugin"] = "Dropbox",
            ["External command Plugin"] = "ExternalCommand",
            ["Jira Plugin"] = "Jira",
            ["Office Plugin"] = "Office",
            ["PDF Plugin"] = "Pdf"
        };

        /// <summary>
        /// The name of a plugin from its file: Box for Greenshot.Plugin.Box.dll
        /// </summary>
        public static string PluginNameFromFile(string pluginFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(pluginFile) ?? string.Empty;
            return fileName.StartsWith(PluginFilePrefix, StringComparison.OrdinalIgnoreCase) ? fileName.Substring(PluginFilePrefix.Length) : fileName;
        }

        /// <summary>
        /// The name of a plugin for a value of IncludePlugins or ExcludePlugins
        /// </summary>
        public static string PluginNameFromLegacyIdentifier(string identifier)
        {
            var trimmed = identifier?.Trim() ?? string.Empty;
            return LegacyPluginIdentifiers.TryGetValue(trimmed, out var name) ? name : trimmed;
        }

        /// <summary>
        /// True when the plugin with the name may be loaded: neither the administrator nor the user excluded it
        /// </summary>
        public static bool IsPluginAllowed(ICoreConfiguration configuration, string pluginName) =>
            !IsPluginBlockedByAdministrator(configuration, pluginName)
            && (configuration == null || !Names(configuration.ExcludePluginNames).Contains(pluginName, StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// True when BlockedPluginNames, IncludePluginNames or the older IncludePlugins / ExcludePlugins don't allow the plugin, the user can't change that
        /// </summary>
        public static bool IsPluginBlockedByAdministrator(ICoreConfiguration configuration, string pluginName)
        {
            if (configuration == null)
            {
                return false;
            }

            var blocked = Names(configuration.BlockedPluginNames).Concat(Names(configuration.ExcludePlugins).Select(PluginNameFromLegacyIdentifier));
            if (blocked.Contains(pluginName, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            var included = Names(configuration.IncludePluginNames).Concat(Names(configuration.IncludePlugins).Select(PluginNameFromLegacyIdentifier)).ToList();
            return included.Count > 0 && !included.Contains(pluginName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when the destination with the designation may be used: neither the administrator nor the user excluded it
        /// </summary>
        public static bool IsDestinationAllowed(ICoreConfiguration configuration, string designation) =>
            !IsDestinationBlockedByAdministrator(configuration, designation)
            && (configuration == null || !Names(configuration.ExcludeDestinations).Contains(designation, StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// True when BlockedDestinations or IncludeDestinations don't allow the destination, the user can't change that
        /// </summary>
        public static bool IsDestinationBlockedByAdministrator(ICoreConfiguration configuration, string designation)
        {
            if (configuration == null)
            {
                return false;
            }

            if (Names(configuration.BlockedDestinations).Contains(designation, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            var included = Names(configuration.IncludeDestinations).ToList();
            return included.Count > 0 && !included.Contains(designation, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when greenshot-fixed.ini sets ExcludePluginNames (also empty): the user can't switch plugins off
        /// </summary>
        public static bool ArePluginExclusionsFixed(ICoreConfiguration configuration) =>
            configuration != null && configuration.IsConstant(nameof(ICoreConfiguration.ExcludePluginNames));

        /// <summary>
        /// True when greenshot-fixed.ini sets ExcludeDestinations (also empty): the user can't switch destinations off
        /// </summary>
        public static bool AreDestinationExclusionsFixed(ICoreConfiguration configuration) =>
            configuration != null && configuration.IsConstant(nameof(ICoreConfiguration.ExcludeDestinations));

        /// <summary>
        /// A list with the choice for the listed entries, keeping the entries of the current list which weren't listed, e.g. a plugin which isn't installed
        /// </summary>
        public static List<string> ReplaceListed(IEnumerable<string> current, IEnumerable<string> listed, IEnumerable<string> chosen)
        {
            var listedSet = new HashSet<string>(listed, StringComparer.OrdinalIgnoreCase);
            return Names(current).Where(name => !listedSet.Contains(name)).Concat(chosen).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// The entries of a list, without empty ones (an empty value in the ini file is one empty entry)
        /// </summary>
        public static IEnumerable<string> Names(IEnumerable<string> values) =>
            values?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()) ?? Enumerable.Empty<string>();
    }
}
