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

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Configuration;
using Greenshot.Destinations;

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

        /// <summary>
        /// This provides the ICoreConfiguration to the view
        /// </summary>
        public ICoreConfiguration CoreConfiguration { get; }

        /// <summary>
        /// This provides the IConfigTranslations to the view
        /// </summary>
        public IConfigTranslations ConfigTranslations { get; }

        /// <summary>
        /// This provides the IGreenshotLanguage to the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="configTranslations">IConfigTranslations</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
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

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.InternalDestinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(CoreConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsDestinationClipboard))
            };
            DisplayName = typeof(ClipboardDestination).GetDesignation();

            UsedFormats.Clear();

            if (CoreConfiguration.ClipboardFormats.Any())
            {
                foreach (var clipboardFormat in CoreConfiguration.ClipboardFormats)
                {
                    UsedFormats.Add(clipboardFormat.ToString());
                }
            }
            
            AvailableFormats.Clear();
            foreach (var clipboardFormat in Enum.GetNames(typeof(ClipboardFormats)))
            {
                if (clipboardFormat == ClipboardFormats.NONE.ToString())
                {
                    continue;
                }
                AvailableFormats.Add(clipboardFormat);
            }
            base.Initialize(config);
        }

        /// <inheritdoc />
        public override void Commit()
        {
            CoreConfiguration.ClipboardFormats = UsedFormats.Select(format =>
            {
                if (!Enum.TryParse<ClipboardFormats>(format, true, out var clipboardFormat))
                {
                    return default;
                }
                return clipboardFormat;
            }).Where(format => format != ClipboardFormats.NONE).ToList();
            base.Commit();
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        /// <summary>
        /// The available clipboard formats
        /// </summary>
        public ObservableCollection<string> AvailableFormats { get; } = new ObservableCollection<string>();

        /// <summary>
        /// The used clipboard formats
        /// </summary>
        public ObservableCollection<string> UsedFormats { get; } = new ObservableCollection<string>();
        
    }
}
