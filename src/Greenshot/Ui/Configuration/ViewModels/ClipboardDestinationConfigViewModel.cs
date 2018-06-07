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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Configuration;

namespace Greenshot.Ui.Configuration.ViewModels
{
    /// <summary>
    /// The ViewModel for the ClipboardDestination configuration
    /// </summary>
    public sealed class ClipboardDestinationConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        public ICoreConfiguration CoreConfiguration { get; }
        public IConfigTranslations ConfigTranslations { get; }
        public IGreenshotLanguage GreenshotLanguage { get; }

        public ClipboardDestinationConfigViewModel(
            ICoreConfiguration coreConfiguration,
            IConfigTranslations configTranslations,
            IGreenshotLanguage greenshotLanguage
            )
        {
            ConfigTranslations = configTranslations;
            GreenshotLanguage = greenshotLanguage;
            CoreConfiguration = coreConfiguration;
        }

        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Destinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(CoreConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsDestinationClipboard))
            };

            UsedDestinations.Clear();

            if (CoreConfiguration.ClipboardFormats.Any())
            {
                foreach (var clipboardFormat in CoreConfiguration.ClipboardFormats)
                {
                    UsedDestinations.Add(clipboardFormat.ToString());
                }
            }
            
            AvailableDestinations.Clear();
            foreach (var clipboardFormat in Enum.GetNames(typeof(ClipboardFormats)))
            {
                if (clipboardFormat == ClipboardFormats.NONE.ToString())
                {
                    continue;
                }
                AvailableDestinations.Add(clipboardFormat);
            }
            base.Initialize(config);
        }

        public override void Commit()
        {
            CoreConfiguration.ClipboardFormats = UsedDestinations.Select(format =>
            {
                if (!Enum.TryParse<ClipboardFormats>(format, true, out var clipboardFormat))
                {
                    return default;
                }
                return clipboardFormat;
            }).Where(format => format != ClipboardFormats.NONE).ToList();
            base.Commit();
        }

        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        public ObservableCollection<string> AvailableDestinations { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> UsedDestinations { get; } = new ObservableCollection<string>();
        
    }
}
