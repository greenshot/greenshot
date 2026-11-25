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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Greenshot.Base.Core;
using Microsoft.Win32;

namespace Greenshot.Plugin.ExternalCommand.Running
{
    /// <summary>
    /// An app the user can pick as the program of a command
    /// </summary>
    internal sealed class InstalledApp
    {
        public InstalledApp(string name, string program, string arguments)
        {
            Name = name;
            Program = program;
            Arguments = arguments;
        }

        public string Name { get; }

        /// <summary>The full path, or the name of an App Execution Alias of a Windows App</summary>
        public string Program { get; }

        /// <summary>The arguments of the app's open command, with ${export.file} for the file</summary>
        public string Arguments { get; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// The apps Windows knows: those which open a file type (the "Open with" list), the App Paths and the App Execution Aliases of
    /// Windows Apps. Windows Apps without an alias can't be given a file, they aren't listed.
    /// </summary>
    internal static class InstalledApps
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(InstalledApps));
        private const string FileExtsKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\";
        private const string AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths";

        // Shell hosts, they open the file with something else
        private static readonly HashSet<string> Hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rundll32.exe", "explorer.exe", "dllhost.exe", "cmd.exe", "openwith.exe" };

        // %1, %L, %V are the file; other placeholders (%*, %2, %I, %W) mean nothing for one file
        private static readonly Regex FilePlaceholder = new Regex("\"?%[1LlVv](?![0-9A-Za-z])\"?", RegexOptions.CultureInvariant);
        private static readonly Regex OtherPlaceholder = new Regex("\\s*\"?%[*0-9A-Za-z](?![0-9A-Za-z])\"?", RegexOptions.CultureInvariant);

        /// <summary>
        /// The apps, sorted by name, each program once
        /// </summary>
        /// <param name="extension">the file type the apps should open, e.g. ".png"</param>
        public static IReadOnlyList<InstalledApp> Find(string extension)
        {
            var apps = new Dictionary<string, InstalledApp>(StringComparer.OrdinalIgnoreCase);
            void Add(InstalledApp app)
            {
                if (app == null || Hosts.Contains(Path.GetFileName(app.Program)) || apps.ContainsKey(app.Program)) return;
                apps[app.Program] = app;
            }

            foreach (var app in OpenWith(extension)) Add(app);
            foreach (var app in AppPaths()) Add(app);
            foreach (var app in ExecutionAliases()) Add(app);
            return apps.Values.OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        /// <summary>
        /// The program and arguments of an open command of the registry, e.g. "C:\Program Files\App\app.exe" "%1"
        /// </summary>
        /// <returns>false when it isn't a program with the file, e.g. a command which starts something else</returns>
        internal static bool TryParseOpenCommand(string command, out string program, out string arguments)
        {
            program = null;
            arguments = null;
            if (string.IsNullOrWhiteSpace(command)) return false;
            command = Environment.ExpandEnvironmentVariables(command.Trim());
            string rest;
            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                int end = command.IndexOf('"', 1);
                if (end < 0) return false;
                program = command.Substring(1, end - 1);
                rest = command.Substring(end + 1);
            }
            else
            {
                // Not quoted, the path may have spaces: up to the .exe
                int end = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (end < 0) return false;
                program = command.Substring(0, end + 4);
                rest = command.Substring(end + 4);
            }

            if (!program.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !FilePlaceholder.IsMatch(rest))
            {
                program = null;
                return false;
            }

            rest = FilePlaceholder.Replace(rest, CommandDefinition.DefaultArguments.Replace("$", "$$"), 1);
            rest = OtherPlaceholder.Replace(rest, string.Empty);
            if (rest.IndexOf('%') >= 0)
            {
                program = null;
                return false;
            }

            arguments = rest.Trim();
            return true;
        }

        private static IEnumerable<InstalledApp> OpenWith(string extension)
        {
            if (string.IsNullOrEmpty(extension)) yield break;
            var progIds = new List<string>();
            var exeNames = new List<string>();
            using (var key = Registry.ClassesRoot.OpenSubKey(extension + @"\OpenWithProgids"))
            {
                progIds.AddRange(key?.GetValueNames().Where(name => name.Length > 0) ?? Enumerable.Empty<string>());
            }

            using (var key = Registry.CurrentUser.OpenSubKey(FileExtsKey + extension + @"\OpenWithProgids"))
            {
                progIds.AddRange(key?.GetValueNames().Where(name => name.Length > 0) ?? Enumerable.Empty<string>());
            }

            using (var key = Registry.ClassesRoot.OpenSubKey(extension + @"\OpenWithList"))
            {
                exeNames.AddRange(key?.GetSubKeyNames() ?? Array.Empty<string>());
            }

            using (var key = Registry.CurrentUser.OpenSubKey(FileExtsKey + extension + @"\OpenWithList"))
            {
                exeNames.AddRange(key?.GetValueNames().Where(name => name != "MRUList").Select(name => key.GetValue(name) as string) ?? Enumerable.Empty<string>());
            }

            foreach (string progId in progIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var app = FromClass(progId, progId);
                if (app != null) yield return app;
            }

            foreach (string exeName in exeNames.Where(name => !string.IsNullOrEmpty(name)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var app = FromClass(@"Applications\" + exeName, null);
                if (app != null) yield return app;
            }
        }

        private static InstalledApp FromClass(string className, string progId)
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(className);
                if (key == null) return null;
                if (FromWindowsApp(key) is { } windowsApp) return windowsApp;
                string command;
                using (var commandKey = key.OpenSubKey(@"shell\open\command") ?? key.OpenSubKey(@"shell\edit\command"))
                {
                    command = commandKey?.GetValue(string.Empty) as string;
                }

                if (!TryParseOpenCommand(command, out string program, out string arguments) || !File.Exists(program)) return null;
                string name = null;
                if (progId != null)
                {
                    using var application = key.OpenSubKey("Application");
                    name = Readable(application?.GetValue("ApplicationName") as string) ?? Readable(key.GetValue(string.Empty) as string);
                }

                return new InstalledApp(Readable(key.GetValue("FriendlyAppName") as string) ?? DescriptionOf(program) ?? name ?? Path.GetFileNameWithoutExtension(program), program, arguments);
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException or ArgumentException)
            {
                Log.Debug($"Can't read the app {className}", ex);
                return null;
            }
        }

        /// <summary>
        /// A Windows App (MSIX/Store) registers a class with its app id and no command string, it starts by its app id
        /// </summary>
        private static InstalledApp FromWindowsApp(RegistryKey key)
        {
            using var application = key.OpenSubKey("Application");
            string program = WindowsAppLauncher.ProgramOf(application?.GetValue("AppUserModelID") as string);
            if (program == null) return null;
            string appId = WindowsAppLauncher.AppIdOf(program);
            string name = Readable(application.GetValue("ApplicationName") as string) ?? Readable(key.GetValue(string.Empty) as string) ?? appId.Substring(appId.IndexOf('!') + 1);
            return new InstalledApp(name, program, CommandDefinition.DefaultArguments);
        }

        private static IEnumerable<InstalledApp> AppPaths()
        {
            foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                using var key = root.OpenSubKey(AppPathsKey);
                foreach (string exeName in key?.GetSubKeyNames() ?? Array.Empty<string>())
                {
                    string program;
                    using (var appKey = key.OpenSubKey(exeName))
                    {
                        program = (appKey?.GetValue(string.Empty) as string)?.Trim().Trim('"');
                    }

                    if (string.IsNullOrEmpty(program)) continue;
                    try
                    {
                        program = Environment.ExpandEnvironmentVariables(program);
                        if (!program.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(program)) continue;
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    yield return new InstalledApp(DescriptionOf(program) ?? Path.GetFileNameWithoutExtension(program), program, CommandDefinition.DefaultArguments);
                }
            }
        }

        private static IEnumerable<InstalledApp> ExecutionAliases()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps");
            IEnumerable<string> files;
            try
            {
                files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.exe") : Array.Empty<string>();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                yield break;
            }

            foreach (string file in files)
            {
                // The alias is found on the PATH, like a program of Windows
                string alias = Path.GetFileName(file);
                yield return new InstalledApp(Path.GetFileNameWithoutExtension(alias), alias, CommandDefinition.DefaultArguments);
            }
        }

        private static string DescriptionOf(string program)
        {
            try
            {
                return Readable(FileVersionInfo.GetVersionInfo(program).FileDescription);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        // A resource reference ("@shell32.dll,-123") isn't a name
        private static string Readable(string name) => string.IsNullOrWhiteSpace(name) || name.StartsWith("@", StringComparison.Ordinal) ? null : name.Trim();
    }
}
