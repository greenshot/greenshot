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

using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addon.Dropbox.Configuration;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addon.Dropbox.ViewModels
{
    /// <summary>
    /// This defines the view model for the dropbox configuration
    /// </summary>
    public sealed class DropboxConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provide IDropboxConfiguration to the view
        /// </summary>
        public IDropboxConfiguration DropboxConfiguration { get; set; }

        /// <summary>
        /// Provide IDropboxLanguage to the view
        /// </summary>
        public IDropboxLanguage DropboxLanguage { get; set; }

        /// <summary>
        /// Provide FileConfigPartViewModel to the view
        /// </summary>
        public FileConfigPartViewModel FileConfigPartViewModel { get; private set; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="dropboxConfiguration">IDropboxConfiguration</param>
        /// <param name="dropboxLanguage">IDropboxLanguage</param>
        /// <param name="fileConfigPartViewModel">FileConfigPartViewModel</param>
        public DropboxConfigViewModel(
            IDropboxConfiguration dropboxConfiguration,
            IDropboxLanguage dropboxLanguage,
            FileConfigPartViewModel fileConfigPartViewModel
        )
        {
            DropboxConfiguration = dropboxConfiguration;
            DropboxLanguage = dropboxLanguage;
            FileConfigPartViewModel = fileConfigPartViewModel;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            FileConfigPartViewModel.DestinationFileConfiguration = DropboxConfiguration;

            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(DropboxConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                DropboxLanguage.CreateDisplayNameBinding(this, nameof(IDropboxLanguage.SettingsTitle))
            };

            base.Initialize(config);
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }
    }
}
