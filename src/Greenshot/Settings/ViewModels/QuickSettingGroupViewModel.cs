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
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// A section of the quick settings window
    /// </summary>
    public sealed class QuickSettingGroupViewModel
    {
        public QuickSettingGroupViewModel(QuickSettingGroup group, QuickSettingsViewModel owner, Action<string> openSettings)
        {
            Title = group.Title;
            Settings = group.Settings.Select(setting => new QuickSettingViewModel(setting, owner)).ToList();
            if (group.MoreSettingsTab != null)
            {
                string tab = group.MoreSettingsTab;
                MoreCommand = new RelayCommand(() => openSettings(tab));
            }
        }

        /// <summary>
        /// The heading, null for the settings without a group
        /// </summary>
        public string Title { get; }

        public bool HasTitle => Title != null;

        public IReadOnlyList<QuickSettingViewModel> Settings { get; }

        /// <summary>
        /// Opens the page of the settings with the rest of the group, null when the group is complete
        /// </summary>
        public ICommand MoreCommand { get; }

        public bool HasMore => MoreCommand != null;

        public string MoreText => Texts.Core.QuicksettingsAutomaticstepsMore;

        public override string ToString() => Title ?? string.Empty;
    }
}
