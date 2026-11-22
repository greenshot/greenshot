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

#if !GREENSHOT_LIGHT
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Greenshot.Destinations.ImageSearch
{
    /// <summary>
    /// Opens an address or a local page in the browser which handles https: a local .html file could belong to an editor.
    /// TODO before merge: AssocQueryString belongs in Dapplo.Windows (Shell32), see the prompt in the PR.
    /// </summary>
    internal static class DefaultBrowser
    {
        private const int AssocfIsProtocol = 0x1000;
        private const int AssocstrCommand = 1;
        private const int SOk = 0;

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int AssocQueryString(int flags, int str, string assoc, string extra, StringBuilder output, ref int outputLength);

        /// <summary>
        /// Open the https address or the full path of a local page in the default browser
        /// </summary>
        /// <exception cref="Win32Exception">the browser couldn't be started</exception>
        public static void Open(string target)
        {
            // A local page as file:// address: a browser takes it like any address, and it has no spaces or quotes
            if (System.IO.Path.IsPathRooted(target))
            {
                target = new Uri(target).AbsoluteUri;
            }

            var startInfo = CreateStartInfo(GetHttpsCommand(), target) ?? new ProcessStartInfo(target) { UseShellExecute = true };
            Process.Start(startInfo)?.Dispose();
        }

        /// <summary>
        /// The command of the https protocol, e.g. "C:\Program Files\...\msedge.exe" --single-argument %1, null when there is none
        /// </summary>
        private static string GetHttpsCommand()
        {
            int length = 1024;
            var command = new StringBuilder(length);
            return AssocQueryString(AssocfIsProtocol, AssocstrCommand, "https", "open", command, ref length) == SOk ? command.ToString() : null;
        }

        /// <summary>
        /// Splits the command in the program and its arguments, %1 becomes the target. Null when the command can't be used.
        /// </summary>
        internal static ProcessStartInfo CreateStartInfo(string command, string target)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            command = command.Trim();
            string program;
            string arguments;
            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                int end = command.IndexOf('"', 1);
                if (end < 0)
                {
                    return null;
                }
                program = command.Substring(1, end - 1);
                arguments = command.Substring(end + 1).Trim();
            }
            else
            {
                int space = command.IndexOf(' ');
                program = space < 0 ? command : command.Substring(0, space);
                arguments = space < 0 ? string.Empty : command.Substring(space + 1).Trim();
            }

            // A command without %1 gets the target at the end; the quotes around %1 are added when missing.
            // Not after --single-argument (Edge, Chrome): everything after it is the address, quotes included.
            string quotedTarget = "\"" + target + "\"";
            if (arguments.Contains("\"%1\""))
            {
                arguments = arguments.Replace("\"%1\"", quotedTarget);
            }
            else if (arguments.Contains("%1"))
            {
                arguments = arguments.Replace("%1", arguments.Contains("--single-argument") ? target : quotedTarget);
            }
            else
            {
                arguments = (arguments + " " + quotedTarget).Trim();
            }

            return new ProcessStartInfo(program, arguments) { UseShellExecute = false };
        }
    }
}
#endif
