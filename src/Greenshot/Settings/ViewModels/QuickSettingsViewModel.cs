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
using System.Windows.Input;
using Greenshot.Base.Wpf;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The quick settings window: the groups of <see cref="QuickSettings"/>, each value is stored as soon as it changes
    /// </summary>
    public sealed class QuickSettingsViewModel
    {
        /// <param name="groups">The quick settings, see <see cref="QuickSettings.Create"/></param>
        /// <param name="openSettings">Opens a page of the settings (null: the first one) and closes the window</param>
        public QuickSettingsViewModel(IEnumerable<QuickSettingGroup> groups, Action<string> openSettings)
        {
            Groups = groups.Select(group => new QuickSettingGroupViewModel(group, this, openSettings)).ToList();
            OpenSettingsCommand = new RelayCommand(() => openSettings(null));
        }

        public IReadOnlyList<QuickSettingGroupViewModel> Groups { get; }

        /// <summary>
        /// Opens the settings window (all settings)
        /// </summary>
        public ICommand OpenSettingsCommand { get; }

        /// <summary>
        /// Reads every value again: one change can change another one (the destination picker clears the destinations),
        /// and the tray menu or the settings may have changed them
        /// </summary>
        public void Refresh()
        {
            foreach (var setting in Groups.SelectMany(group => group.Settings))
            {
                setting.Refresh();
            }
        }
    }
}
