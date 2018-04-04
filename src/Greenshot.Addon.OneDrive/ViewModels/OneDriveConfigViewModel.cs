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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addon.OneDrive;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;

namespace Greenshot.Addon.OneDrive.ViewModels
{
    [Export(typeof(IConfigScreen))]
    public sealed class OneDriveConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        [Import]
        public IOneDriveConfiguration OneDriveConfiguration { get; set; }

        [Import]
        public IOneDriveLanguage OneDriveLanguage { get; set; }

        [Import]
        public IGreenshotLanguage GreenshotLanguage { get; set; }

        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(OneDriveConfiguration);

            // automatically update the DisplayName
            var languBinding = OneDriveLanguage.CreateDisplayNameBinding(this, nameof(IOneDriveLanguage.SettingsTitle));

            // Make sure the greenshotLanguageBinding is disposed when this is no longer active
            _disposables.Add(languBinding);

            base.Initialize(config);
        }

        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }


        public OutputFormats SelectedUploadFormat
        {
            get => OneDriveConfiguration.UploadFormat;
            set
            {
                OneDriveConfiguration.UploadFormat = value;
                NotifyOfPropertyChange();
            }
        }

        public IDictionary<OutputFormats, string> UploadFormats => GreenshotLanguage.TranslationValuesForEnum<OutputFormats>();

    }
}
