/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Languages;
using Greenshot.Configuration;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Win10;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The OCR engine and language on the Capture tab
    /// </summary>
    public partial class SettingsViewModel
    {
        private IWin10Configuration _win10Configuration;
        private IList<OcrLanguage> _ocrLanguages;

        /// <summary>
        /// The OCR engines: Windows OCR and those of plugins
        /// </summary>
        public IList<OcrEngineChoice> OcrEngines => OcrEngineSelector.Engines.Select(engine => new OcrEngineChoice(engine.Id, engine.DisplayName)).ToList();

        /// <summary>
        /// The engine choice is only shown when a plugin adds one
        /// </summary>
        public bool HasOcrEngineChoice => OcrEngineSelector.Engines.Count > 1;

        /// <summary>
        /// The language choice only for an engine which has languages (Windows OCR)
        /// </summary>
        public bool HasOcrLanguageChoice => OcrLanguages.Count > 1 || string.Equals(OcrEngineSelector.ConfiguredId, Win10OcrProvider.EngineId, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The id of the OCR engine. An engine which isn't ready (e.g. needs a download) gets ready first, asking the user; when that is declined the choice goes back.
        /// </summary>
        public string SelectedOcrEngine
        {
            get => OcrEngineSelector.ConfiguredId;
            set
            {
                string previous = OcrEngineSelector.ConfiguredId;
                if (Win10Configuration == null || string.IsNullOrEmpty(value) || string.Equals(previous, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var engine = OcrEngineSelector.Engines.FirstOrDefault(candidate => string.Equals(candidate.Id, value, StringComparison.OrdinalIgnoreCase));
                if (engine == null)
                {
                    return;
                }

                SetOcrEngine(value);
                if (!engine.IsReady)
                {
                    PrepareOcrEngineAsync(engine, previous).FireAndLog("OCR engine", Log);
                }
            }
        }

        /// <summary>
        /// The engine may download for a while (its window can be closed meanwhile); the choice goes back only when it is still this engine
        /// </summary>
        private async Task PrepareOcrEngineAsync(IOcrEngine engine, string previous)
        {
            bool ready;
            try
            {
                ready = await engine.PrepareAsync(IntPtr.Zero, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Log.Warn($"OCR engine {engine.Id} couldn't get ready", ex);
                ready = false;
            }

            if (!ready && string.Equals(OcrEngineSelector.ConfiguredId, engine.Id, StringComparison.OrdinalIgnoreCase))
            {
                SetOcrEngine(previous);
            }
        }

        private void SetOcrEngine(string id)
        {
            Win10Configuration.OcrEngine = string.Equals(id, Win10OcrProvider.EngineId, StringComparison.OrdinalIgnoreCase) ? string.Empty : id;
            _ocrLanguages = null;
            OnPropertyChanged(nameof(SelectedOcrEngine));
            OnPropertyChanged(nameof(OcrLanguages));
            OnPropertyChanged(nameof(SelectedOcrLanguage));
            OnPropertyChanged(nameof(HasOcrLanguageChoice));
        }

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

    /// <summary>
    /// An OCR engine in the settings
    /// </summary>
    public sealed class OcrEngineChoice
    {
        public OcrEngineChoice(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }

        public string DisplayName { get; }
    }
}
