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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// A switch (check box) or a choice (list) of the quick settings window, the value is stored right away
    /// </summary>
    public sealed class QuickSettingViewModel : INotifyPropertyChanged
    {
        private readonly QuickSetting _setting;
        private readonly QuickSettingsViewModel _owner;

        public QuickSettingViewModel(QuickSetting setting, QuickSettingsViewModel owner)
        {
            _setting = setting;
            _owner = owner;
            Choices = setting.Choices?.ToList() ?? new List<QuickSettingChoice>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Label => _setting.Label;

        /// <summary>
        /// The tool tip, null when there is none
        /// </summary>
        public string Description => _setting.Description;

        public bool IsChoice => _setting.IsChoice;

        public bool IsSwitch => !_setting.IsChoice;

        public bool IsOn
        {
            get => _setting.IsOn;
            set
            {
                if (value == _setting.IsOn)
                {
                    return;
                }
                _setting.SetOn(value);
                _owner?.Refresh();
            }
        }

        public IReadOnlyList<QuickSettingChoice> Choices { get; }

        public QuickSettingChoice SelectedChoice
        {
            get => Choices.FirstOrDefault(choice => choice.IsSelected);
            set
            {
                if (value == null || value.IsSelected)
                {
                    return;
                }
                value.Select();
                _owner?.Refresh();
            }
        }

        /// <summary>
        /// The value may have changed: tell the view
        /// </summary>
        public void Refresh()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOn)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedChoice)));
        }

        public override string ToString() => Label;
    }
}
