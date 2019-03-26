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
using Greenshot.Addon.GooglePhotos.Configuration;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addon.GooglePhotos.ViewModels
{
    public sealed class GooglePhotosConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Configuration for the view
        /// </summary>
        public IGooglePhotosConfiguration GooglePhotosConfiguration { get; }

        /// <summary>
        /// Translations for the view
        /// </summary>
        public IGooglePhotosLanguage GooglePhotosLanguage { get; }

        /// <summary>
        /// FileConfigPartViewModel is used from the view
        /// TODO: Check if this is really true and needed
        /// </summary>
        public FileConfigPartViewModel FileConfigPartViewModel { get; }

        public GooglePhotosConfigViewModel(
            IGooglePhotosConfiguration googlePhotosConfiguration,
            IGooglePhotosLanguage googlePhotosLanguage,
            FileConfigPartViewModel fileConfigPartViewModel)
        {
            GooglePhotosConfiguration = googlePhotosConfiguration;
            GooglePhotosLanguage = googlePhotosLanguage;
            FileConfigPartViewModel = fileConfigPartViewModel;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            FileConfigPartViewModel.DestinationFileConfiguration = GooglePhotosConfiguration;
            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Destinations);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GooglePhotosLanguage.CreateDisplayNameBinding(this, nameof(IGooglePhotosLanguage.SettingsTitle))
            };

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(GooglePhotosConfiguration);

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
