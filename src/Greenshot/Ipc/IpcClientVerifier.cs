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
using System.IO;
using System.IO.Pipes;

namespace Greenshot.Ipc
{
    /// <summary>
    /// Checks that the program on the other end of a pipe connection is one of Greenshot's own executables, from Greenshot's
    /// directory (the stand-alone editor: from its folder below it), and that it announced a source this executable uses. Other programs can't talk to the pipe directly;
    /// they can only start these executables, which then decide the source (e.g. greenshot-cli.exe is always "cli").
    /// Source "mcp" is checked by AiToolCaller, which also identifies the AI tool.
    /// </summary>
    public static class IpcClientVerifier
    {
        private static readonly Dictionary<string, string[]> SourcesByExecutable = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // A second Greenshot instance forwards its command line
            ["Greenshot.exe"] = new[] { IpcSources.Cli },
            // greenshot-cli.exe, the console front end
            ["greenshot-cli.exe"] = new[] { IpcSources.Cli },
            // greenshot-proxy.exe: greenshot: URLs, Explorer "Open with" and the browser extension (native messaging)
            ["greenshot-proxy.exe"] = new[] { IpcSources.UrlScheme, IpcSources.OpenWith, IpcSources.NativeMessaging },
            // greenshot-editor.exe, the stand-alone editor: it runs on another .NET and has its own folder below Greenshot's
            [StandaloneEditor] = new[] { IpcSources.Editor }
        };

        /// <summary>
        /// The stand-alone editor, relative to Greenshot's directory
        /// </summary>
        public const string StandaloneEditor = @"Editor\greenshot-editor.exe";

        /// <summary>
        /// Verifies the client of the connection for every source except "mcp".
        /// </summary>
        public static bool Verify(NamedPipeServerStream pipe, string source, out string error)
        {
            if (!PipeClientProcess.TryGetClientProcess(pipe, out _, out string clientPath))
            {
                error = "Could not identify the program of the connection.";
                return false;
            }
            return IsAllowed(clientPath, source, AppDomain.CurrentDomain.BaseDirectory, out error);
        }

        /// <summary>
        /// The name of the program as it is listed: its file name when it is in Greenshot's directory, with the folder in front
        /// when it is in a folder directly below. Null for a program anywhere else.
        /// </summary>
        private static string RelativeName(string clientPath, string greenshotDirectory)
        {
            if (string.IsNullOrEmpty(clientPath))
            {
                return null;
            }

            string greenshot = PipeClientProcess.NormalizeDirectory(greenshotDirectory);
            string directory = Path.GetDirectoryName(clientPath);
            if (string.Equals(PipeClientProcess.NormalizeDirectory(directory), greenshot, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFileName(clientPath);
            }

            string parent = string.IsNullOrEmpty(directory) ? null : Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (parent != null && string.Equals(PipeClientProcess.NormalizeDirectory(parent), greenshot, StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), Path.GetFileName(clientPath));
            }

            return null;
        }

        /// <summary>
        /// True when the executable is one of Greenshot's, in Greenshot's directory, and may use the source.
        /// </summary>
        internal static bool IsAllowed(string clientPath, string source, string greenshotDirectory, out string error)
        {
            string fileName = RelativeName(clientPath, greenshotDirectory);
            if (fileName == null || !SourcesByExecutable.TryGetValue(fileName, out var sources))
            {
                error = $"Only Greenshot's own programs may connect, not '{clientPath ?? "unknown"}'.";
                return false;
            }
            if (Array.FindIndex(sources, s => string.Equals(s, source, StringComparison.OrdinalIgnoreCase)) < 0)
            {
                error = $"{fileName} may not connect as '{source}'.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
