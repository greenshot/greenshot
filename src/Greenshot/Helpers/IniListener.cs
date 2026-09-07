/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

#if DEBUG
using System;
using Dapplo.Ini.Interfaces;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// A debug-only listener for Dapplo.Ini that logs all configuration lifecycle events to the
    /// debug log. Register it via <c>IniConfigRegistry.ForFile(...).AddListener(new IniListener())</c>
    /// to help track issues with .ini file loading.
    /// </summary>
    internal sealed class IniListener : IIniConfigListener
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IniListener));

        /// <inheritdoc />
        public void OnFileLoaded(string filePath)
        {
            Log.DebugFormat("[Dapplo.Ini] Loaded: {0}", filePath);
        }

        /// <inheritdoc />
        public void OnFileNotFound(string fileName)
        {
            Log.DebugFormat("[Dapplo.Ini] File not found: {0}", fileName);
        }

        /// <inheritdoc />
        public void OnSaved(string filePath)
        {
            Log.DebugFormat("[Dapplo.Ini] Saved: {0}", filePath);
        }

        /// <inheritdoc />
        public void OnReloaded(string filePath)
        {
            Log.DebugFormat("[Dapplo.Ini] Reloaded: {0}", filePath);
        }

        /// <inheritdoc />
        public void OnError(string operation, Exception exception)
        {
            Log.DebugFormat("[Dapplo.Ini] Error during '{0}': {1}", operation, exception);
        }

        /// <inheritdoc />
        public void OnUnknownKey(string sectionName, string key, string rawValue)
        {
            Log.DebugFormat("[Dapplo.Ini] Unknown key in [{0}]: {1} = {2}", sectionName, key, rawValue);
        }

        /// <inheritdoc />
        public void OnValueConversionFailed(string sectionName, string key, string rawValue, Exception exception)
        {
            Log.DebugFormat("[Dapplo.Ini] Value conversion failed in [{0}] for key '{1}' (raw: '{2}'): {3}", sectionName, key, rawValue, exception);
        }
    }
}
#endif
