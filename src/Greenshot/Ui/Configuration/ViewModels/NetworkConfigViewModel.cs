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
using System.Windows;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Security;
using Greenshot.Addons;
using Greenshot.Addons.Core;

namespace Greenshot.Ui.Configuration.ViewModels
{
    /// <summary>
    /// This is the vide model for the network configuration
    /// </summary>
    public sealed class NetworkConfigViewModel : AuthenticatedConfigNode<Visibility>
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provides the IHttpConfiguration for the view
        /// </summary>
        public IHttpConfiguration HttpConfiguration { get; }

        /// <summary>
        /// Provides the IGreenshotLanguage for the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="httpConfiguration"></param>
        /// <param name="greenshotLanguage"></param>
        public NetworkConfigViewModel(
            IHttpConfiguration httpConfiguration,
            IGreenshotLanguage greenshotLanguage
            )
        {
            HttpConfiguration = httpConfiguration;
            GreenshotLanguage = greenshotLanguage;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(HttpConfiguration);

            this.VisibleOnPermissions("Expert");

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsNetwork))
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
