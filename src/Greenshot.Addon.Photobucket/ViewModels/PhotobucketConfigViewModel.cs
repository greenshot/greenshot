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
using Greenshot.Addon.Photobucket.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addon.Photobucket.ViewModels
{
    /// <summary>
    /// This is the view model for the photobucket config
    /// </summary>
    public sealed class PhotobucketConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provide the IPhotobucketConfiguration to the view
        /// </summary>
        public IPhotobucketConfiguration PhotobucketConfiguration { get; }
        /// <summary>
        /// Provide the IPhotobucketLanguage to the view
        /// </summary>
        public IPhotobucketLanguage PhotobucketLanguage { get; }
        /// <summary>
        /// Provide the IGreenshotLanguage to the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }
        /// <summary>
        /// Provide the FileConfigPartViewModel to the view
        /// </summary>
        public FileConfigPartViewModel FileConfigPartViewModel { get; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="photobucketConfiguration">IPhotobucketConfiguration</param>
        /// <param name="photobucketLanguage">IPhotobucketLanguage</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="fileConfigPartViewModel">FileConfigPartViewModel</param>
        public PhotobucketConfigViewModel(
            IPhotobucketConfiguration photobucketConfiguration,
            IPhotobucketLanguage photobucketLanguage,
            IGreenshotLanguage greenshotLanguage,
            FileConfigPartViewModel fileConfigPartViewModel)
        {
            PhotobucketConfiguration = photobucketConfiguration;
            PhotobucketLanguage = photobucketLanguage;
            GreenshotLanguage = greenshotLanguage;
            FileConfigPartViewModel = fileConfigPartViewModel;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            FileConfigPartViewModel.DestinationFileConfiguration = PhotobucketConfiguration;
            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(PhotobucketConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                PhotobucketLanguage.CreateDisplayNameBinding(this, nameof(IPhotobucketLanguage.SettingsTitle))
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
