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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Metro;
using Dapplo.Utils.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Configuration;

namespace Greenshot.Ui.Configuration.ViewModels
{
    public sealed class UiConfigViewModel : SimpleConfigScreen
    {
        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        ///     The avaible theme accents
        /// </summary>
        public ObservableCollection<Tuple<ThemeAccents, string>> AvailableThemeAccents { get; set; } = new ObservableCollection<Tuple<ThemeAccents, string>>();

        /// <summary>
        ///     The avaible themes
        /// </summary>
        public ObservableCollection<Tuple<Themes, string>> AvailableThemes { get; set; } = new ObservableCollection<Tuple<Themes, string>>();

        /// <summary>
        /// Used from the View
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        // TODO: Fix
        public IDictionary<string, string> AvailableLanguages => new Dictionary<string, string>();//LanguageLoader.Current.AvailableLanguages;

        /// <summary>
        ///     Can the login button be pressed?
        /// </summary>
        // TODO: Fix
        public bool CanChangeLanguage
            => !string.IsNullOrWhiteSpace(CoreConfiguration.Language); // && CoreConfiguration.Language != LanguageLoader.Current.CurrentLanguage;

        public IMetroConfiguration MetroConfiguration { get; }

        public IConfigTranslations ConfigTranslations { get; }

        public ICoreConfiguration CoreConfiguration { get; }

        public IGreenshotLanguage GreenshotLanguage { get; }

        private MetroWindowManager MetroWindowManager { get; }

        public UiConfigViewModel(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            IConfigTranslations configTranslations,
            IMetroConfiguration metroConfiguration,
            MetroWindowManager metroWindowManager

            )
        {
            CoreConfiguration = coreConfiguration;
            GreenshotLanguage = greenshotLanguage;
            ConfigTranslations = configTranslations;
            MetroConfiguration = metroConfiguration;
            MetroWindowManager = metroWindowManager;
        }

        public override void Commit()
        {
            // Manually commit
            MetroConfiguration.CommitTransaction();
            MetroWindowManager.ChangeTheme(MetroConfiguration.Theme);
            MetroWindowManager.ChangeThemeAccent(MetroConfiguration.ThemeAccent);

            CoreConfiguration.CommitTransaction();
            // TODO: Fix
            //Execute.OnUIThread(async () => { await LanguageLoader.Current.ChangeLanguageAsync(CoreConfiguration.Language).ConfigureAwait(false); });

        }

        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            AvailableThemeAccents.Clear();
            foreach (var themeAccent in Enum.GetValues(typeof(ThemeAccents)).Cast<ThemeAccents>())
            {
                var translation = themeAccent.EnumValueOf();
                AvailableThemeAccents.Add(new Tuple<ThemeAccents, string>(themeAccent, translation));
            }

            AvailableThemes.Clear();
            foreach (var theme in Enum.GetValues(typeof(Themes)).Cast<Themes>())
            {
                var translation = theme.EnumValueOf();
                AvailableThemes.Add(new Tuple<Themes, string>(theme, translation));
            }

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.Ui);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(MetroConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsTitle))
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
