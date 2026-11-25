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
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Greenshot.Configuration;

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// The OCR of Greenshot: the engine of the setting OcrEngine when it is ready, else Windows OCR (and the user is asked whether
    /// the chosen engine gets ready again)
    /// </summary>
    public sealed class OcrEngineSelector : IOcrProvider
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OcrEngineSelector));
        private readonly Func<IReadOnlyList<IOcrEngine>> _engines;
        private readonly Func<string> _configuredId;
        private readonly Func<IUiDispatcher> _ui;
        private readonly Action _useWindowsOcr;
        private string _loggedFallback;
        private int _asking;

        /// <summary>
        /// The selector of Greenshot: the registered engines, the setting OcrEngine
        /// </summary>
        public OcrEngineSelector() : this(() => Engines, () => ConfiguredId, () => SimpleServiceProvider.Current.GetInstance<IUiDispatcher>(isOptional: true), UseWindowsOcr)
        {
        }

        /// <summary>
        /// For tests: where the engines and the chosen id come from, the UI thread, and what makes Windows OCR the chosen engine
        /// </summary>
        internal OcrEngineSelector(Func<IReadOnlyList<IOcrEngine>> engines, Func<string> configuredId, Func<IUiDispatcher> ui, Action useWindowsOcr)
        {
            _engines = engines;
            _configuredId = configuredId;
            _ui = ui;
            _useWindowsOcr = useWindowsOcr;
        }

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
                var engines = _engines();
                string id = _configuredId();
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
            RecognizeAsync(engine => engine.DoOcrAsync(image, languageTag));

        /// <inheritdoc />
        public Task<List<IOcrLineFeature>> DoOcrAsync(ISurface surface, string languageTag = null) =>
            RecognizeAsync(engine => engine.DoOcrAsync(surface, languageTag));

        /// <summary>
        /// The text of the chosen engine. When it can't be used (its files were deleted, or found changed while loading them)
        /// another engine reads this capture and the user is asked once: get the chosen engine ready again, or use Windows OCR.
        /// </summary>
        private async Task<List<IOcrLineFeature>> RecognizeAsync(Func<IOcrProvider, Task<List<IOcrLineFeature>>> recognize)
        {
            var chosen = Chosen;
            var engine = Current;
            if (engine == null) return new List<IOcrLineFeature>();
            if (chosen != null && !ReferenceEquals(engine, chosen))
            {
                AskToRepair(chosen);
            }

            var lines = await recognize(engine).ConfigureAwait(false);
            if (ReferenceEquals(engine, chosen) && !chosen.IsReady)
            {
                // It found out while reading: its files aren't the expected ones
                AskToRepair(chosen);
                var fallback = Current;
                if (fallback != null && !ReferenceEquals(fallback, chosen))
                {
                    lines = await recognize(fallback).ConfigureAwait(false);
                }
            }

            return lines ?? new List<IOcrLineFeature>();
        }

        /// <summary>
        /// The configured engine when it isn't Windows OCR
        /// </summary>
        private IOcrEngine Chosen
        {
            get
            {
                string id = _configuredId();
                if (string.Equals(id, Win10OcrProvider.EngineId, StringComparison.OrdinalIgnoreCase)) return null;
                return _engines().FirstOrDefault(engine => string.Equals(engine.Id, id, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Ask (on the UI thread, while the OCR goes on) whether the engine gets ready again; no: the setting becomes Windows OCR.
        /// Only one question at a time.
        /// </summary>
        private void AskToRepair(IOcrEngine engine)
        {
            if (Interlocked.Exchange(ref _asking, 1) == 1) return;
            var ui = _ui();
            if (ui == null)
            {
                Interlocked.Exchange(ref _asking, 0);
                return;
            }

            Repairing = RepairAsync(ui, engine);
            Repairing.FireAndLog("OCR engine repair", Log);
        }

        private async Task RepairAsync(IUiDispatcher ui, IOcrEngine engine)
        {
            try
            {
                bool ready = await ui.InvokeAsync(() => engine.PrepareAsync(IntPtr.Zero, true, CancellationToken.None)).ConfigureAwait(false);
                if (!ready && string.Equals(_configuredId(), engine.Id, StringComparison.OrdinalIgnoreCase))
                {
                    Log.InfoFormat("OCR engine {0} isn't used anymore, Windows OCR recognizes text", engine.Id);
                    _useWindowsOcr();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _asking, 0);
            }
        }

        private static void UseWindowsOcr()
        {
            if (IniConfigRegistry.GetSection<IWin10Configuration>() is not { } configuration) return;
            configuration.OcrEngine = string.Empty;
            IniConfigRegistry.Get()?.Save();
        }

        /// <summary>
        /// The question was answered (tests wait for it)
        /// </summary>
        internal Task Repairing { get; private set; } = Task.CompletedTask;

        /// <inheritdoc />
        public IList<OcrLanguage> GetAvailableLanguages() => Current?.GetAvailableLanguages() ?? new List<OcrLanguage>();
    }
}
