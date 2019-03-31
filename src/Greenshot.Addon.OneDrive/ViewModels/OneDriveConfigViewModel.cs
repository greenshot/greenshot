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

using System.Collections.Generic;
using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addon.OneDrive.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addon.OneDrive.ViewModels
{
    /// <inherit />
    public sealed class OneDriveConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Used in the view to change the configuration
        /// </summary>
        public IOneDriveConfiguration OneDriveConfiguration { get; }

        /// <summary>
        /// Used in the view for the translations
        /// </summary>
        public IOneDriveLanguage OneDriveLanguage { get; }

        /// <summary>
        /// Used in the view for the translations
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// Used in the view to change the file configuration
        /// </summary>
        public FileConfigPartViewModel FileConfigPartViewModel { get; }

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="oneDriveConfiguration">IOneDriveConfiguration</param>
        /// <param name="oneDriveLanguage">IOneDriveLanguage</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="fileConfigPartViewModel">FileConfigPartViewModel</param>
        public OneDriveConfigViewModel(
            IOneDriveConfiguration oneDriveConfiguration,
            IOneDriveLanguage oneDriveLanguage,
            IGreenshotLanguage greenshotLanguage,
            FileConfigPartViewModel fileConfigPartViewModel
        )
        {
            OneDriveConfiguration = oneDriveConfiguration;
            OneDriveLanguage = oneDriveLanguage;
            GreenshotLanguage = greenshotLanguage;
            FileConfigPartViewModel = fileConfigPartViewModel;
        }

        /// <inherit />
        public override void Initialize(IConfig config)
        {
            FileConfigPartViewModel.DestinationFileConfiguration = OneDriveConfiguration;
            // Prepare disposables
            _disposables?.Dispose();
            
            // Place this config viewmodel under the Destinations parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(OneDriveConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                OneDriveLanguage.CreateDisplayNameBinding(this, nameof(IOneDriveLanguage.SettingsTitle))
            };
            base.Initialize(config);
        }

        /// <inherit />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        public OneDriveLinkType SelectedLinkType
        {
            get => OneDriveConfiguration.LinkType;
            set
            {
                OneDriveConfiguration.LinkType = value;
                NotifyOfPropertyChange();
            }
        }

        public IDictionary<OneDriveLinkType, string> LinkTypes => OneDriveLanguage.TranslationValuesForEnum<OneDriveLinkType>();

        public bool CanResetCredentials => OneDriveConfiguration.HasToken();

        /// <summary>
        /// Used to reset/clear the OAuth credentials
        /// </summary>
        public void ResetCredentials()
        {
            OneDriveConfiguration.ResetToken();
            NotifyOfPropertyChange(nameof(CanResetCredentials));
        }
    }
}
