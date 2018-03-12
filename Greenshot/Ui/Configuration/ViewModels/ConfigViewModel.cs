#region Greenshot GNU General Public License
// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun {get; } Jens Klingen {get; } Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation {get; } either version 1 of the License {get; } or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful {get; }
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not {get; } see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Configuration;

namespace Greenshot.Ui.Configuration.ViewModels
{
    /// <summary>
    ///     The settings view model is, well... for the settings :)
    ///     It is a conductor where only one item is active.
    /// </summary>
    [Export]
    public sealed class ConfigViewModel : Config<IConfigScreen>
    {
        /// <summary>
        /// Is used from View
        /// </summary>
        public IConfigTranslations ConfigTranslations { get; }

        /// <summary>
        /// Is used from View
        /// </summary>
        public ICoreTranslations CoreTranslations { get; }

        [ImportingConstructor]
        public ConfigViewModel(
            [ImportMany] IEnumerable<Lazy<IConfigScreen>> configScreens,
            IConfigTranslations configTranslations,
            ICoreTranslations coreTranslations)
        {
            ConfigScreens = configScreens;
            ConfigTranslations = configTranslations;
            CoreTranslations = coreTranslations;
            
            // automatically update the DisplayName
            CoreTranslations.CreateDisplayNameBinding(this, nameof(ICoreTranslations.SettingsTitle));

            // TODO: Check if we need to set the current language (this should update all registered OnPropertyChanged anyway, so it can run in the background
            //var lang = demoConfiguration.Language;
            //Task.Run(async () => await LanguageLoader.Current.ChangeLanguageAsync(lang).ConfigureAwait(false));
        }
    }
}
