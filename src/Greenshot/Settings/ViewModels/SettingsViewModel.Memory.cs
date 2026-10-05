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
using System.ComponentModel;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The memory and speed settings on the Expert tab: one dial (the profile) and the settings it sets, which can be changed one by one
    /// </summary>
    public partial class SettingsViewModel
    {
        private IList<MemoryProfileOption> _memoryProfileOptions;

        /// <summary>
        /// The profiles of the dial, Custom is only shown, it can't be chosen
        /// </summary>
        public IList<MemoryProfileOption> MemoryProfileOptions => _memoryProfileOptions ??= new List<MemoryProfileOption>
        {
            new MemoryProfileOption(MemoryProfile.Fast, Texts.Settings.MemoryProfileFast),
            new MemoryProfileOption(MemoryProfile.Balanced, Texts.Settings.MemoryProfileBalanced),
            new MemoryProfileOption(MemoryProfile.LowMemory, Texts.Settings.MemoryProfileLowmemory),
            new MemoryProfileOption(MemoryProfile.Custom, Texts.Settings.MemoryProfileCustom)
        };

        /// <summary>
        /// The profile which matches the settings; choosing one sets them (the check boxes follow)
        /// </summary>
        public MemoryProfile SelectedMemoryProfile
        {
            get => MemoryProfiles.Detect(CoreConfiguration);
            set
            {
                if (value == MemoryProfile.Custom || value == MemoryProfiles.Detect(CoreConfiguration))
                {
                    return;
                }

                MemoryProfiles.Apply(CoreConfiguration, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMemoryProfileName));
            }
        }

        /// <summary>
        /// The name of the profile which matches the settings, shown in the header of the collapsed group
        /// </summary>
        public string SelectedMemoryProfileName
        {
            get
            {
                var selected = SelectedMemoryProfile;
                return MemoryProfileOptions.FirstOrDefault(option => option.Profile == selected)?.DisplayName;
            }
        }

        /// <summary>
        /// The dial shows the profile the settings match, also when a single setting changes
        /// </summary>
        private void InitializeMemoryProfile()
        {
            // Weak: the configuration lives as long as Greenshot, this view model only as long as the settings window
            PropertyChangedEventManager.AddHandler(CoreConfiguration, OnMemorySettingChanged, string.Empty);
        }

        private void OnMemorySettingChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ICoreConfiguration.HardwareRendering):
                case nameof(ICoreConfiguration.KeepGraphicsCaptureReady):
                case nameof(ICoreConfiguration.PrewarmCapture):
                case nameof(ICoreConfiguration.PrewarmEditor):
                case nameof(ICoreConfiguration.BufferPoolLimit):
                case nameof(ICoreConfiguration.MinimizeWorkingSetSize):
                case null:
                case "":
                    OnPropertyChanged(nameof(SelectedMemoryProfile));
                    OnPropertyChanged(nameof(SelectedMemoryProfileName));
                    break;
            }
        }
    }
}
