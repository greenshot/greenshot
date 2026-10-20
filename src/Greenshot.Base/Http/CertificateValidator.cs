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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Dapplo.Ini;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// The certificate rules of Greenshot: the platform validation, plus the hosts and thumbprints the user allowed in the core configuration.
    /// Used by the HttpService and, via <see cref="ServicePointManager"/>, by HttpWebRequest based libraries (Jira).
    /// </summary>
    public static class CertificateValidator
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CertificateValidator));
        // Looked up on use: RegisterGlobally runs before the configuration is loaded
        private static ICoreConfiguration Config => IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// Use these rules for everything that goes through ServicePointManager (HttpWebRequest), called once at startup.
        /// </summary>
        public static void RegisterGlobally()
        {
            ServicePointManager.ServerCertificateValidationCallback = Validate;
        }

        /// <summary>
        /// Validates server SSL/TLS certificates according to standard rules and configured exceptions in the Core configuration.
        /// </summary>
        /// <param name="sender">The sender object (e.g. HttpWebRequest or ServicePoint)</param>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="chain">The certificate chain</param>
        /// <param name="sslPolicyErrors">Any SSL policy errors identified by the platform</param>
        /// <returns>True if the certificate is accepted; otherwise, false.</returns>
        public static bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // If standard platform validation succeeded without any policy errors, accept
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (certificate == null)
            {
                Log.Warn($"SSL/TLS certificate validation failed: certificate is null (Policy errors: {sslPolicyErrors}).");
                return false;
            }

            // 1. Check certificate thumbprint allowlist
            var certThumbprint = (certificate as X509Certificate2)?.Thumbprint ?? certificate.GetCertHashString();
            if (!string.IsNullOrEmpty(certThumbprint) && Config?.AllowedCertificateThumbprints != null)
            {
                string normalizedThumbprint = certThumbprint.Replace(":", "").Replace(" ", "");
                foreach (var allowedThumbprint in Config.AllowedCertificateThumbprints)
                {
                    if (string.IsNullOrWhiteSpace(allowedThumbprint))
                    {
                        continue;
                    }

                    string normalizedAllowed = allowedThumbprint.Replace(":", "").Replace(" ", "");
                    if (string.Equals(normalizedAllowed, normalizedThumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Info($"SSL/TLS certificate validation exception accepted by thumbprint '{certThumbprint}' for subject '{certificate.Subject}'.");
                        return true;
                    }
                }
            }

            // 2. Extract host from sender (HttpWebRequest, ServicePoint, Uri, string)
            string host = null;
            if (sender is HttpWebRequest webRequest && webRequest.RequestUri != null)
            {
                host = webRequest.RequestUri.Host;
            }
            else if (sender is ServicePoint servicePoint && servicePoint.Address != null)
            {
                host = servicePoint.Address.Host;
            }
            else if (sender is Uri uri)
            {
                host = uri.Host;
            }
            else if (sender is string hostStr)
            {
                if (Uri.TryCreate(hostStr, UriKind.Absolute, out var parsedUri))
                {
                    host = parsedUri.Host;
                }
                else
                {
                    host = hostStr;
                }
            }

            // 3. Check allowed untrusted hosts allowlist
            if (!string.IsNullOrEmpty(host) && Config?.AllowedUntrustedCertificateHosts != null)
            {
                foreach (var pattern in Config.AllowedUntrustedCertificateHosts)
                {
                    if (string.IsNullOrWhiteSpace(pattern))
                    {
                        continue;
                    }

                    if (IsHostMatch(host, pattern.Trim()))
                    {
                        Log.Warn($"SSL/TLS certificate validation exception accepted for host '{host}' (Policy errors: {sslPolicyErrors}, Subject: '{certificate.Subject}').");
                        return true;
                    }
                }
            }

            Log.Warn($"SSL/TLS certificate validation failed for host '{(host ?? "unknown")}' (Policy errors: {sslPolicyErrors}, Subject: '{certificate.Subject}', Thumbprint: '{certThumbprint}').");
            return false;
        }

        /// <summary>
        /// Checks if a host matches a pattern (supports exact match and wildcard like *.example.com or *example.com).
        /// </summary>
        /// <param name="host">The target hostname</param>
        /// <param name="pattern">The configured pattern</param>
        /// <returns>True if host matches pattern, otherwise false</returns>
        public static bool IsHostMatch(string host, string pattern)
        {
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            // Strip port if specified in host or pattern (e.g. host:8443)
            int colonIndex = host.IndexOf(':');
            if (colonIndex >= 0)
            {
                host = host.Substring(0, colonIndex);
            }

            int patternColonIndex = pattern.IndexOf(':');
            if (patternColonIndex >= 0)
            {
                pattern = pattern.Substring(0, patternColonIndex);
            }

            if (string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (pattern.Contains("*") || pattern.Contains("?"))
            {
                string regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                try
                {
                    return Regex.IsMatch(host, regexPattern, RegexOptions.IgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
