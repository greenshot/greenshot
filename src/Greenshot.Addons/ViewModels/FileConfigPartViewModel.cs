#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;

namespace Greenshot.Addons.ViewModels
{
    /// <summary>
    /// A view model for showing the file configuration
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public sealed class FileConfigPartViewModel : Screen
    {
        private IDestinationFileConfiguration _destinationFileConfiguration;
        private bool _useOwnSettings;

        [Import]
        public ICoreConfiguration FileConfiguration { get; set; }

        [Import]
        public IGreenshotLanguage GreenshotLanguage { get; set; }

        /// <summary>
        /// A NPC wrapper for the UseOwnSettings in the IDestinationFileConfiguration, as this doesn't work when ITransactionalProperties is used
        /// </summary>
        public bool UseOwnSettings
        {
            get => _useOwnSettings;
            set
            {
                if (value == _useOwnSettings) return;
                _useOwnSettings = value;
                if (_destinationFileConfiguration != null)
                {
                    _destinationFileConfiguration.UseOwnSettings = value;
                }
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// This needs to be set with the IDestinationFileConfiguration to have make local configuration for a destination possible
        /// </summary>
        public IDestinationFileConfiguration DestinationFileConfiguration
        {
            get => _destinationFileConfiguration;
            set
            {
                if (Equals(value, _destinationFileConfiguration)) return;
                _destinationFileConfiguration = value;
                NotifyOfPropertyChange(nameof(DestinationFileConfigurationVisiblity));
                NotifyOfPropertyChange(nameof(AreGlobalSettingsEnabled));
            }
        }

        /// <summary>
        /// Specifies if the global settings can be modified, which is the case when there are is no DestinationFileConfiguration
        /// </summary>
        public bool AreGlobalSettingsEnabled => DestinationFileConfiguration == null;

        /// <summary>
        /// If there is a DestinationFileConfiguration, the configuration is shown
        /// </summary>
        public Visibility DestinationFileConfigurationVisiblity => DestinationFileConfiguration == null ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// The globally selected format
        /// </summary>
        public OutputFormats SelectedFormat
        {
            get => FileConfiguration.OutputFileFormat ;
            set
            {
                FileConfiguration.OutputFileFormat = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(GlobalJpegSettingsVisibility));
            }
        }

        /// <summary>
        /// The global jpeg quality settings visibility
        /// </summary>
        public Visibility GlobalJpegSettingsVisibility => SelectedFormat == OutputFormats.jpg ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// The locally selected format
        /// </summary>
        public OutputFormats DestinationSelectedFormat
        {
            get => DestinationFileConfiguration.OutputFileFormat;
            set
            {
                DestinationFileConfiguration.OutputFileFormat = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(DestinationJpegSettingsVisibility));
            }
        }

        /// <summary>
        /// The global jpeg quality settings visibility
        /// </summary>
        public Visibility DestinationJpegSettingsVisibility => DestinationSelectedFormat == OutputFormats.jpg ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// The formats available, with their translation
        /// </summary>
        public IDictionary<OutputFormats, string> Formats => GreenshotLanguage.TranslationValuesForEnum<OutputFormats>();
    }
}
