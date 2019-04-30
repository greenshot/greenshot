// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Core.Enums;

namespace Greenshot.Addons.ViewModels
{
    /// <summary>
    /// A view model for showing the file configuration
    /// </summary>
    public sealed class FileConfigPartViewModel : Screen
    {
        private IDestinationFileConfiguration _destinationFileConfiguration;
        private bool _useOwnSettings;

        /// <summary>
        /// Provide the ICoreConfiguration to the view
        /// </summary>
        public ICoreConfiguration CoreConfiguration { get; }

        /// <summary>
        /// Provide the IGreenshotLanguage to the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        public FileConfigPartViewModel(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage
            )
        {
            CoreConfiguration = coreConfiguration;
            GreenshotLanguage = greenshotLanguage;
        }

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
                NotifyOfPropertyChange(nameof(AreSettingsEnabled));
                NotifyOfPropertyChange(nameof(FileConfiguration));
                NotifyOfPropertyChange(nameof(SelectedFormat));
                NotifyOfPropertyChange(nameof(JpegSettingsVisibility));
            }
        }

        /// <summary>
        /// Provide the IFileConfiguration to the view
        /// </summary>
        public IFileConfiguration FileConfiguration => 
            DestinationFileConfiguration?.UseOwnSettings == true
            ? (IFileConfiguration)DestinationFileConfiguration
            : CoreConfiguration;

        /// <summary>
        /// This needs to be set with the IDestinationFileConfiguration to have make local configuration for a destination possible
        /// </summary>
        public IDestinationFileConfiguration DestinationFileConfiguration
        {
            get => _destinationFileConfiguration;
            set
            {
                if (Equals(value, _destinationFileConfiguration))
                {
                    return;
                }
                _destinationFileConfiguration = value;
                NotifyOfPropertyChange(nameof(AreSettingsEnabled));
                NotifyOfPropertyChange(nameof(FileConfiguration));
                NotifyOfPropertyChange(nameof(OwnSettingsVisibility));
            }
        }

        /// <summary>
        /// This opens the directory selection dialog
        /// </summary>
        public void SelectOutputPath()
        {
            using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                // Get the storage location and replace the environment variables
                folderBrowserDialog.SelectedPath = FilenameHelper.FillVariables(CoreConfiguration.OutputFilePath, false);
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Only change if there is a change, otherwise we might overwrite the environment variables
                    if (folderBrowserDialog.SelectedPath != null && !folderBrowserDialog.SelectedPath.Equals(FilenameHelper.FillVariables(CoreConfiguration.OutputFilePath, false)))
                    {
                        CoreConfiguration.OutputFilePath = folderBrowserDialog.SelectedPath;
                    }
                }
            }
        }

        /// <summary>
        /// Specifies if the global settings can be modified, which is the case when there are is no DestinationFileConfiguration
        /// </summary>
        public bool AreSettingsEnabled => DestinationFileConfiguration == null || DestinationFileConfiguration.UseOwnSettings;

        /// <summary>
        /// If there is a DestinationFileConfiguration, the checkbox is shown
        /// </summary>
        public Visibility OwnSettingsVisibility => DestinationFileConfiguration != null? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// The globally selected format
        /// </summary>
        public OutputFormats SelectedFormat
        {
            get => FileConfiguration.OutputFileFormat;
            set
            {
                FileConfiguration.OutputFileFormat = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(JpegSettingsVisibility));
            }
        }

        /// <summary>
        /// The global jpeg quality settings visibility
        /// </summary>
        public Visibility JpegSettingsVisibility => SelectedFormat == OutputFormats.jpg ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// The formats available, with their translation
        /// </summary>
        public IDictionary<OutputFormats, string> Formats => GreenshotLanguage.TranslationValuesForEnum<OutputFormats>();
    }
}
