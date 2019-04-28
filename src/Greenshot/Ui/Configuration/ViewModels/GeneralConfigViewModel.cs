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

using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Components;

namespace Greenshot.Ui.Configuration.ViewModels
{
    public sealed class GeneralConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        public ICoreConfiguration CoreConfiguration { get; }

        public IGreenshotLanguage GreenshotLanguage { get; }

        public AuthenticationProvider AuthenticationProvider { get; }

        public GeneralConfigViewModel(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            AuthenticationProvider authenticationProvider
            )
        {
            CoreConfiguration = coreConfiguration;
            GreenshotLanguage = greenshotLanguage;
            AuthenticationProvider = authenticationProvider;
        }

        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(CoreConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsGeneral))
            };

            base.Initialize(config);
        }

        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        /// <summary>
        /// Change the expert mode
        /// </summary>
        public bool Expert
        {
            get => AuthenticationProvider?.HasPermissions(new[] { "Expert" }) == true;
            set
            {
                if (AuthenticationProvider == null)
                {
                    return;
                }
                if (value)
                {
                    AuthenticationProvider.AddPermission("Expert");
                }
                else
                {
                    AuthenticationProvider.RemovePermission("Expert");
                }
                NotifyOfPropertyChange();
            }

        }
    }
}
