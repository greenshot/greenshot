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
using System.Collections.Concurrent;
using Dapplo.Ini.Internationalization.Interfaces;
using log4net;

namespace Greenshot.Base.Languages
{
    /// <summary>
    /// Writes what happens with the language packs to the log; a missing text is logged once per key.
    /// </summary>
    internal sealed class LanguageLogListener : LanguageConfigListenerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Texts));
        private readonly ConcurrentDictionary<string, bool> _reportedKeys = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public override void OnLanguageResolved(string requested, string resolved)
        {
            if (!string.Equals(requested, resolved, StringComparison.OrdinalIgnoreCase))
            {
                Log.InfoFormat("No language pack for {0}, using {1}", requested, resolved);
            }
        }

        public override void OnTranslationNotFound(string sectionName, string key)
        {
            if (_reportedKeys.TryAdd($"{sectionName}/{key}", true))
            {
                Log.WarnFormat("No text for {0} in [{1}]", key, sectionName);
            }
        }

        public override void OnFormatFailed(string sectionName, string key, Exception exception)
        {
            Log.Warn($"Couldn't format the text {key} in [{sectionName}]", exception);
        }

        public override void OnFileLoaded(string filePath) => Log.DebugFormat("Loaded language pack {0}", filePath);

        public override void OnReloaded(string language) => Log.InfoFormat("Language switched to {0}", language);

        public override void OnError(string operation, Exception exception) => Log.Error($"Language packs: {operation} failed", exception);
    }
}
