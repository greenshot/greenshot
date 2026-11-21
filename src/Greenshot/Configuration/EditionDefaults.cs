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
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core;

namespace Greenshot.Configuration
{
    /// <summary>
    /// The defaults of an edition which differ from the compiled ones, registered as a value source of the Default layer:
    /// they replace the compiled defaults and the values of greenshot-defaults.ini, but every value the user set wins.
    /// Greenshot Light starts with the LowMemory memory profile: it is the small tray tool, so no DirectX device or prepared editor
    /// is kept ready and WPF renders in software. The user can turn the profile up in the settings.
    /// </summary>
    public sealed class EditionDefaults : IValueSource
    {
        private const string CoreSection = "Core";
        private readonly IReadOnlyDictionary<string, string> _coreValues;

        public EditionDefaults(MemoryProfile memoryProfile)
        {
            _coreValues = MemoryProfiles.IniValuesOf(memoryProfile);
        }

        /// <summary>
        /// The defaults never change while Greenshot runs
        /// </summary>
        public event EventHandler<ValueChangedEventArgs> ValueChanged
        {
            add { }
            remove { }
        }

        public bool TryGetValue(string sectionName, string key, out string value)
        {
            value = null;
            return string.Equals(sectionName, CoreSection, StringComparison.OrdinalIgnoreCase)
                   && key != null
                   && _coreValues.TryGetValue(key, out value);
        }
    }
}
