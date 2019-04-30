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
using Greenshot.Addons.ViewModels;

namespace Greenshot.Ui.Configuration.ViewModels
{
    /// <summary>
    /// This is the view model for the output settings
    /// </summary>
    public sealed class OutputConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provide the ICoreConfiguration to the view
        /// </summary>
        public ICoreConfiguration CoreConfiguration { get; set; }

        /// <summary>
        /// Provide the IGreenshotLanguage to the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; set; }

        /// <summary>
        /// Provide the FileConfigPartViewModel to the view
        /// </summary>
        public FileConfigPartViewModel FileConfigPartViewModel { get; set; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="fileConfigPartViewModel">FileConfigPartViewModel</param>
        public OutputConfigViewModel(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            FileConfigPartViewModel fileConfigPartViewModel)
        {
            CoreConfiguration = coreConfiguration;
            GreenshotLanguage = greenshotLanguage;
            FileConfigPartViewModel = fileConfigPartViewModel;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(CoreConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsOutput))
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
