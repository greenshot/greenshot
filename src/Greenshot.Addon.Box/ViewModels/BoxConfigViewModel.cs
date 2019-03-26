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
using Greenshot.Addon.Box.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addon.Box.ViewModels
{
    public sealed class BoxConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        public IBoxConfiguration BoxConfiguration { get; }

        public IBoxLanguage BoxLanguage { get; }

        public IGreenshotLanguage GreenshotLanguage { get; }

        public FileConfigPartViewModel FileConfigPartViewModel { get; }

        public BoxConfigViewModel(
            IBoxConfiguration boxConfiguration,
            IBoxLanguage boxLanguage,
            IGreenshotLanguage greenshotLanguage,
            FileConfigPartViewModel fileConfigPartViewModel
        )
        {
            BoxConfiguration = boxConfiguration;
            BoxLanguage = boxLanguage;
            GreenshotLanguage = greenshotLanguage;
            FileConfigPartViewModel = fileConfigPartViewModel;
        }

        public override void Initialize(IConfig config)
        {
            FileConfigPartViewModel.DestinationFileConfiguration = BoxConfiguration;

            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(BoxConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                BoxLanguage.CreateDisplayNameBinding(this, nameof(IBoxLanguage.SettingsTitle))
            };

            base.Initialize(config);
        }

        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }
    }
}
