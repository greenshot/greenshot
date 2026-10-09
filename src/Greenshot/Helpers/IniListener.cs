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
using Dapplo.Ini.Interfaces;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Listener for Dapplo.Ini which logs the configuration lifecycle events.
    /// Registered in every build: errors of background work (auto-save, reload after a file change, save on exit)
    /// are only reported through <see cref="OnError"/>, the rest is logged on the debug level.
    /// </summary>
    internal sealed class IniListener : IniConfigListenerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IniListener));

        /// <inheritdoc />
        public override void OnFileLoaded(string filePath)
        {
            Log.DebugFormat("[Dapplo.Ini] Loaded: {0}", filePath);
        }

        /// <inheritdoc />
        public override void OnFileNotFound(string fileName)
        {
            Log.DebugFormat("[Dapplo.Ini] File not found: {0}", fileName);
        }

        /// <inheritdoc />
        public override void OnSaved(string filePath)
        {
            Log.DebugFormat("[Dapplo.Ini] Saved: {0}", filePath);
        }

        /// <inheritdoc />
        public override void OnReloaded(string filePath)
        {
            Log.DebugFormat("[Dapplo.Ini] Reloaded: {0}", filePath);
        }

        /// <inheritdoc />
        public override void OnError(string operation, Exception exception)
        {
            Log.Error($"[Dapplo.Ini] Error during '{operation}'", exception);
        }

        /// <inheritdoc />
        public override void OnUnknownKey(string sectionName, string key, string rawValue)
        {
            Log.DebugFormat("[Dapplo.Ini] Unknown key in [{0}]: {1} = {2}", sectionName, key, rawValue);
        }

        /// <inheritdoc />
        public override void OnValueConversionFailed(string sectionName, string key, string rawValue, Exception exception)
        {
            Log.WarnFormat("[Dapplo.Ini] Value conversion failed in [{0}] for key '{1}' (raw: '{2}'), using the default: {3}", sectionName, key, rawValue, exception.Message);
        }

        /// <inheritdoc />
        public override void OnSectionAdded(string sectionName, bool loaded)
        {
            Log.DebugFormat("[Dapplo.Ini] Section [{0}] added {1}", sectionName, loaded ? "and loaded" : "before the load");
        }
    }
}
