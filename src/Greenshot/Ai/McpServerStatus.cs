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
using System.Diagnostics;
using System.IO;

namespace Greenshot.Ai
{
    /// <summary>
    /// A place where greenshot-mcp.exe may be, for the settings: next to Greenshot.exe, and in Debug builds the
    /// AiToolsMcpServerPaths locations (the same places <see cref="AiToolCaller.IsTrustedMcpServer"/> accepts).
    /// </summary>
    public sealed class McpServerStatus
    {
        /// <summary>
        /// The download page of greenshot-mcp (a separate release download)
        /// </summary>
        public const string DownloadUrl = "https://github.com/greenshot/greenshot/releases";

        public string Path { get; private set; }

        public bool Exists { get; private set; }

        /// <summary>
        /// One of the AiToolsMcpServerPaths (Debug builds only)
        /// </summary>
        public bool IsAdditional { get; private set; }

        public string Version { get; private set; }

        /// <summary>
        /// Found, but not the version of this Greenshot
        /// </summary>
        public bool IsOtherVersion { get; private set; }

        public string Symbol => Exists ? "✔" : "✘";

        public string Text
        {
            get
            {
                string where = IsAdditional ? "in an AiToolsMcpServerPaths location (Debug build)" : "next to Greenshot.exe";
                if (!Exists)
                {
                    return $"{AiToolCaller.McpServerFileName} was not found {where}";
                }
                return IsOtherVersion
                    ? $"{AiToolCaller.McpServerFileName} was found {where}, but it is version {Version}: use the one for this Greenshot"
                    : $"{AiToolCaller.McpServerFileName} was found {where}" + (string.IsNullOrEmpty(Version) ? "" : $" (version {Version})");
            }
        }

        /// <summary>
        /// The places greenshot-mcp.exe is looked for, Greenshot's own directory first
        /// </summary>
        public static IReadOnlyList<McpServerStatus> Find()
        {
            string greenshotDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string greenshotVersion = GetVersion(System.IO.Path.Combine(greenshotDirectory, "Greenshot.exe"));
            var result = new List<McpServerStatus> { Create(System.IO.Path.Combine(greenshotDirectory, AiToolCaller.McpServerFileName), false, greenshotVersion) };
            foreach (string additional in AiToolCaller.GetAdditionalMcpServerPaths() ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(additional))
                {
                    continue;
                }
                string path = additional.Trim().Trim('"');
                if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    path = System.IO.Path.Combine(path, AiToolCaller.McpServerFileName);
                }
                result.Add(Create(path, true, greenshotVersion));
            }
            return result;
        }

        private static McpServerStatus Create(string path, bool isAdditional, string greenshotVersion)
        {
            bool exists = File.Exists(path);
            string version = exists ? GetVersion(path) : null;
            return new McpServerStatus
            {
                Path = path,
                Exists = exists,
                IsAdditional = isAdditional,
                Version = version,
                IsOtherVersion = !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(greenshotVersion) &&
                                 !string.Equals(version, greenshotVersion, StringComparison.OrdinalIgnoreCase)
            };
        }

        private static string GetVersion(string path)
        {
            try
            {
                return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
