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
using Greenshot.Addon.Confluence.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Core.Enums;

namespace Greenshot.Addon.Confluence.ViewModels
{
    /// <summary>
    /// View model for the confluence configuration
    /// </summary>
    public sealed class ConfluenceConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        public IConfluenceConfiguration ConfluenceConfiguration { get; }
        public IConfluenceLanguage ConfluenceLanguage { get; }
        public IGreenshotLanguage GreenshotLanguage { get; }

        public ConfluenceConfigViewModel(
            IConfluenceConfiguration confluenceConfiguration,
            IConfluenceLanguage confluenceLanguage,
            IGreenshotLanguage greenshotLanguage)
        {
            ConfluenceConfiguration = confluenceConfiguration;
            ConfluenceLanguage = confluenceLanguage;
            GreenshotLanguage = greenshotLanguage;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(ConfluenceConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                ConfluenceLanguage.CreateDisplayNameBinding(this, nameof(IConfluenceLanguage.PluginSettings))
            };
            base.Initialize(config);
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        public OutputFormats SelectedUploadFormat
        {
            get => ConfluenceConfiguration.UploadFormat;
            set
            {
                ConfluenceConfiguration.UploadFormat = value;
                NotifyOfPropertyChange();
            }
        }

        public IDictionary<OutputFormats, string> UploadFormats => GreenshotLanguage.TranslationValuesForEnum<OutputFormats>();
    }
}
