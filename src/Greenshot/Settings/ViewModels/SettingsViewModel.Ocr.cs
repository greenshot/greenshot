/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Languages;
using Greenshot.Configuration;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The OCR language on the Capture tab
    /// </summary>
    public partial class SettingsViewModel
    {
        private IWin10Configuration _win10Configuration;
        private IList<OcrLanguage> _ocrLanguages;

        /// <summary>
        /// Automatic (the languages of the Windows user profile), the installed OCR languages, and the configured language when it isn't installed (anymore)
        /// </summary>
        public IList<OcrLanguage> OcrLanguages => _ocrLanguages ??= CreateOcrLanguages();

        /// <summary>
        /// Only shown when there is an OCR
        /// </summary>
        public bool HasOcr => SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true) != null;

        /// <summary>
        /// The language tag of the OCR language, empty for automatic
        /// </summary>
        public string SelectedOcrLanguage
        {
            get
            {
                // The tag as the OCR lists it, the combo box compares case sensitive
                var configured = Win10Configuration?.OcrLanguage ?? string.Empty;
                return OcrLanguages.FirstOrDefault(language => string.Equals(language.LanguageTag, configured, StringComparison.OrdinalIgnoreCase))?.LanguageTag ?? configured;
            }
            set
            {
                if (Win10Configuration == null || string.Equals(Win10Configuration.OcrLanguage ?? string.Empty, value ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                Win10Configuration.OcrLanguage = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        private IWin10Configuration Win10Configuration => _win10Configuration ??= IniConfigRegistry.GetSection<IWin10Configuration>();

        private IList<OcrLanguage> CreateOcrLanguages()
        {
            var languages = new List<OcrLanguage>
            {
                new OcrLanguage(string.Empty, Texts.Settings.OcrLanguageAutomatic)
            };
            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider == null)
            {
                return languages;
            }

            try
            {
                languages.AddRange(ocrProvider.GetAvailableLanguages().OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't list the OCR languages", ex);
            }

            // Keep a configured language which isn't installed, so it isn't lost by opening the settings
            var configured = Win10Configuration?.OcrLanguage?.Trim();
            if (!string.IsNullOrWhiteSpace(configured) && !languages.Any(language => string.Equals(language.LanguageTag, configured, StringComparison.OrdinalIgnoreCase)))
            {
                languages.Add(new OcrLanguage(configured, $"{configured} ({Texts.Settings.OcrLanguageNotinstalled})"));
            }
            return languages;
        }
    }
}
