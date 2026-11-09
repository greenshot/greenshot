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
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Configuration;

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// The OCR of Greenshot: the engine of the setting OcrEngine when it is ready, else Windows OCR
    /// </summary>
    public sealed class OcrEngineSelector : IOcrProvider
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OcrEngineSelector));
        private string _loggedFallback;

        /// <summary>
        /// All engines, Windows OCR first
        /// </summary>
        public static IReadOnlyList<IOcrEngine> Engines =>
            SimpleServiceProvider.Current.GetAllInstances<IOcrEngine>()
                .OrderBy(engine => engine.Id == Win10OcrProvider.EngineId ? 0 : 1)
                .ToList();

        /// <summary>
        /// The id of the configured engine, Windows when none is set
        /// </summary>
        public static string ConfiguredId
        {
            get
            {
                string configured = IniConfigRegistry.GetSection<IWin10Configuration>()?.OcrEngine;
                return string.IsNullOrWhiteSpace(configured) ? Win10OcrProvider.EngineId : configured.Trim();
            }
        }

        /// <summary>
        /// The engine which recognizes text now
        /// </summary>
        public IOcrProvider Current
        {
            get
            {
                var engines = Engines;
                string id = ConfiguredId;
                var configured = engines.FirstOrDefault(engine => string.Equals(engine.Id, id, StringComparison.OrdinalIgnoreCase));
                if (configured is { IsReady: true })
                {
                    return configured;
                }

                var fallback = engines.FirstOrDefault(engine => engine.IsReady);
                if (_loggedFallback != id)
                {
                    _loggedFallback = id;
                    Log.InfoFormat("OCR engine {0} is {1}, using {2}", id, configured == null ? "not available" : "not ready", fallback?.Id ?? "none");
                }
                return fallback;
            }
        }

        /// <inheritdoc />
        public Task<List<IOcrLineFeature>> DoOcrAsync(Image image, string languageTag = null) =>
            Current?.DoOcrAsync(image, languageTag) ?? Task.FromResult(new List<IOcrLineFeature>());

        /// <inheritdoc />
        public Task<List<IOcrLineFeature>> DoOcrAsync(ISurface surface, string languageTag = null) =>
            Current?.DoOcrAsync(surface, languageTag) ?? Task.FromResult(new List<IOcrLineFeature>());

        /// <inheritdoc />
        public IList<OcrLanguage> GetAvailableLanguages() => Current?.GetAvailableLanguages() ?? new List<OcrLanguage>();
    }
}
