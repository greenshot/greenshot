/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using Greenshot.Base.Core;
using Dapplo.Ini;

namespace Greenshot.Plugin.Box;

public partial class BoxConfigurationImpl : IBoxConfiguration
{
    public void OnAfterLoad()
    {
        var coreConfiguration = IniConfigRegistry.GetSection<ICoreConfiguration>();
        // Not LastSaveWithVersion: this section can be added after an auto-save already replaced it with the current version
        bool isUpgradeFrom12 = coreConfiguration.LoadedWithVersion?.StartsWith("1.2") ?? false;
        // Clear token when we upgrade from 1.2 to 1.3 as it is no longer valid, discussed in #421
        if (isUpgradeFrom12)
        {
            // We have an upgrade, remove all previous credentials.
            RefreshToken = null;
            AccessToken = null;
        }

        // The RefreshToken stays encrypted in the section, it's decrypted where it's used.
        // Decrypting here and encrypting in OnBeforeSave via SetRawValue doesn't work: SetRawValue also updates the property,
        // so after every save the in-memory token was the encrypted one.
    }
}
