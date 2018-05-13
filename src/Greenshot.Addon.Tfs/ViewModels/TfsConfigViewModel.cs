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

using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addon.Tfs.ViewModels
{
    public sealed class TfsConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;
        
        public ITfsConfiguration TfsConfiguration { get; }
        
        public ITfsLanguage TfsLanguage { get; }
        
        public IGreenshotLanguage GreenshotLanguage { get; }
        
        public TfsClient TfsClient { get; }

        public FileConfigPartViewModel FileConfigPartViewModel { get; }

        public TfsConfigViewModel(
            ITfsConfiguration tfsConfiguration,
            ITfsLanguage tfsLanguage,
            IGreenshotLanguage greenshotLanguage,
            TfsClient tfsClient,
            FileConfigPartViewModel fileConfigPartViewModel)
        {
            TfsConfiguration = tfsConfiguration;
            TfsLanguage = tfsLanguage;
            GreenshotLanguage = greenshotLanguage;
            TfsClient = tfsClient;
            FileConfigPartViewModel = fileConfigPartViewModel;
        }
        public override void Initialize(IConfig config)
        {
            FileConfigPartViewModel.DestinationFileConfiguration = TfsConfiguration;
            // Prepare disposables
            _disposables?.Dispose();
            
            // Place this config viewmodel under the Destinations parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(TfsConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                TfsLanguage.CreateDisplayNameBinding(this, nameof(ITfsLanguage.SettingsTitle))
            };
            base.Initialize(config);
        }

        protected override void OnDeactivate(bool close)
        {
            var ignoreTask = TfsClient.UpdateWorkItems();
            _disposables.Dispose();
            base.OnDeactivate(close);
        }
    }
}
