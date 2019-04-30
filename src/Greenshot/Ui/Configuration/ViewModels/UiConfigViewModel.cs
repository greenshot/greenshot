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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Metro;
using Dapplo.Config.Intercepting;
using Dapplo.Config.Language;
using Dapplo.Utils.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Configuration;

namespace Greenshot.Ui.Configuration.ViewModels
{
    public sealed class UiConfigViewModel : SimpleConfigScreen
    {
        private readonly MetroThemeManager _metroThemeManager;
        private readonly LanguageContainer _languageContainer;

        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        ///     The available themes
        /// </summary>
        public ObservableCollection<string> AvailableThemes { get; set; } = new ObservableCollection<string>();

        /// <summary>
        ///     The available theme colors
        /// </summary>
        public ObservableCollection<string> AvailableThemeColors { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Used from the View
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        // TODO: Fix
        public IDictionary<string, string> AvailableLanguages { get; }

        /// <summary>
        ///     Can the login button be pressed?
        /// </summary>
        // TODO: Fix
        public bool CanChangeLanguage
            => !string.IsNullOrWhiteSpace(CoreConfiguration.Language) && CoreConfiguration.Language != _languageContainer.CurrentLanguage;

        public IMetroConfiguration MetroConfiguration { get; }
        public IConfigTranslations ConfigTranslations { get; }

        public ICoreConfiguration CoreConfiguration { get; }

        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// Default constructor for DI usage
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="configTranslations">IConfigTranslations</param>
        /// <param name="metroConfiguration">IMetroConfiguration</param>
        /// <param name="metroThemeManager">MetroThemeManager</param>
        public UiConfigViewModel(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            IConfigTranslations configTranslations,
            IMetroConfiguration metroConfiguration,
            MetroThemeManager metroThemeManager,
            LanguageContainer languageContainer
            )
        {
            AvailableLanguages = languageContainer.AvailableLanguages;
            _metroThemeManager = metroThemeManager;
            _languageContainer = languageContainer;
            CoreConfiguration = coreConfiguration;
            GreenshotLanguage = greenshotLanguage;
            ConfigTranslations = configTranslations;
            MetroConfiguration = metroConfiguration;
        }

        public override void Commit()
        {
            // Manually commit
            _metroThemeManager.ChangeTheme(MetroConfiguration.Theme, MetroConfiguration.ThemeColor);
            // Manually commit
            MetroConfiguration.CommitTransaction();

            CoreConfiguration.CommitTransaction();
            Execute.OnUIThread(async () => {
                await _languageContainer.ChangeLanguageAsync(CoreConfiguration.Language).ConfigureAwait(false); });

        }

        /// <inheritdoc />
        public override void Rollback()
        {
            MetroConfiguration.RollbackTransaction();
            _metroThemeManager.ChangeTheme(MetroConfiguration.Theme, MetroConfiguration.ThemeColor);
        }

        /// <inheritdoc />
        public override void Terminate()
        {
            MetroConfiguration.RollbackTransaction();
            _metroThemeManager.ChangeTheme(MetroConfiguration.Theme, MetroConfiguration.ThemeColor);
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            AvailableThemes.Clear();
            MetroThemeManager.AvailableThemes.ForEach(themeBaseColor => AvailableThemes.Add(themeBaseColor));
            MetroThemeManager.AvailableThemeColors.ForEach(colorScheme => AvailableThemeColors.Add(colorScheme));

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Ui);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(MetroConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsTitle))
            };

            // Automatically show theme changes
            _disposables.Add(
                MetroConfiguration.OnPropertyChanging(nameof(MetroConfiguration.Theme)).Subscribe(args =>
                {
                    if (args is PropertyChangingEventArgsEx propertyChangingEventArgsEx)
                    {
                        _metroThemeManager.ChangeTheme(propertyChangingEventArgsEx.NewValue as string, null);
                    }
                })
            );
            _disposables.Add(
                MetroConfiguration.OnPropertyChanging(nameof(MetroConfiguration.ThemeColor)).Subscribe(args =>
                {
                    if (args is PropertyChangingEventArgsEx propertyChangingEventArgsEx)
                    {
                        _metroThemeManager.ChangeTheme(null, propertyChangingEventArgsEx.NewValue as string);
                    }
                })
            );
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
