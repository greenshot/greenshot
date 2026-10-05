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
using System.IO;
using log4net;
using Newtonsoft.Json.Linq;

namespace Greenshot.Ipc.BrowserExtension
{
    /// <summary>
    /// Decides which browser extensions may talk to Greenshot over a native_messaging connection.
    /// The browser only starts greenshot-proxy.exe for extensions listed in the native messaging host manifest,
    /// and the proxy passes the caller's origin on in HELLO. Greenshot checks that origin against the same list,
    /// so a host manifest registered elsewhere (pointing at greenshot-proxy.exe) does not open Greenshot to other extensions.
    /// Allowed are the official extension IDs plus whatever the host manifests next to Greenshot allow
    /// (the self-service debug page writes a development extension ID into org.greenshot.proxy.json).
    /// </summary>
    public sealed class ExtensionOriginPolicy
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExtensionOriginPolicy));

        /// <summary>Host manifest for Chromium browsers (Chrome, Edge): "allowed_origins"</summary>
        public const string ChromeManifestFileName = "org.greenshot.proxy.json";

        /// <summary>Host manifest for Firefox: "allowed_extensions"</summary>
        public const string FirefoxManifestFileName = "org.greenshot.proxy-firefox.json";

        /// <summary>The published Greenshot extensions</summary>
        public static readonly IReadOnlyList<string> OfficialOrigins = new[]
        {
            "chrome-extension://knldjmfmopnpolahpmmgbagdohdnhkik/",
            "greenshot@getgreenshot.org"
        };

        private readonly string _manifestDirectory;

        /// <param name="manifestDirectory">Directory with the host manifests, null to allow only the official extensions</param>
        public ExtensionOriginPolicy(string manifestDirectory)
        {
            _manifestDirectory = manifestDirectory;
        }

        /// <summary>Policy for the installed Greenshot: the manifests are installed next to Greenshot.exe and greenshot-proxy.exe</summary>
        public static ExtensionOriginPolicy ForApplicationDirectory() => new ExtensionOriginPolicy(AppDomain.CurrentDomain.BaseDirectory);

        /// <summary>
        /// True when the origin (Chromium: "chrome-extension://id/", Firefox: the extension id) is allowed.
        /// The manifests are read on every call; this happens once per browser connection, and picks up changes without a restart.
        /// </summary>
        public bool IsAllowed(string origin)
        {
            string normalized = Normalize(origin);
            if (normalized == null)
            {
                return false;
            }
            foreach (string allowed in GetAllowedOrigins())
            {
                if (string.Equals(normalized, Normalize(allowed), StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The official origins plus the ones from the host manifests</summary>
        public IEnumerable<string> GetAllowedOrigins()
        {
            foreach (string origin in OfficialOrigins)
            {
                yield return origin;
            }
            if (string.IsNullOrEmpty(_manifestDirectory))
            {
                yield break;
            }
            foreach (string origin in ReadManifestList(Path.Combine(_manifestDirectory, ChromeManifestFileName), "allowed_origins"))
            {
                yield return origin;
            }
            foreach (string origin in ReadManifestList(Path.Combine(_manifestDirectory, FirefoxManifestFileName), "allowed_extensions"))
            {
                yield return origin;
            }
        }

        private static IEnumerable<string> ReadManifestList(string manifestPath, string propertyName)
        {
            var result = new List<string>();
            try
            {
                if (!File.Exists(manifestPath))
                {
                    return result;
                }
                if (JObject.Parse(File.ReadAllText(manifestPath))[propertyName] is JArray entries)
                {
                    foreach (var entry in entries)
                    {
                        if (entry.Type == JTokenType.String)
                        {
                            result.Add((string)entry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not read the native messaging host manifest '{manifestPath}'", ex);
            }
            return result;
        }

        /// <summary>
        /// Chromium origins are compared case-insensitively (extension IDs are lower case a-p) with a trailing slash,
        /// Firefox extension IDs exactly. Wildcards are not supported, just like in the browser manifests.
        /// </summary>
        private static string Normalize(string origin)
        {
            if (string.IsNullOrWhiteSpace(origin))
            {
                return null;
            }
            origin = origin.Trim();
            if (origin.Contains("*"))
            {
                return null;
            }
            if (origin.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase))
            {
                origin = origin.ToLowerInvariant();
                if (!origin.EndsWith("/", StringComparison.Ordinal))
                {
                    origin += "/";
                }
            }
            return origin;
        }
    }
}
