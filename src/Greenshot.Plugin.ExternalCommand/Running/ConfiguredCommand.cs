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

using System.Collections.Generic;
using Dapplo.Ini;

namespace Greenshot.Plugin.ExternalCommand.Running
{
    /// <summary>
    /// The settings of a command configured in the External Command settings, per command with the global values as fallback;
    /// the same for the destination and for a recipe which names the command.
    /// </summary>
    public sealed class ConfiguredCommand
    {
        public string Name { get; private set; }

        /// <summary>The program, a template (Command context)</summary>
        public string Program { get; private set; }

        /// <summary>The arguments, a template (Argument context)</summary>
        public string Arguments { get; private set; }

        /// <summary>The file format id, null when not set</summary>
        public string Format { get; private set; }

        /// <summary>Null when not set</summary>
        public bool? RunInBackground { get; private set; }

        /// <summary>RedirectStandardOutput: the output is read</summary>
        public bool ReadOutput { get; private set; }

        /// <summary>RedirectStandardError: the error output is read</summary>
        public bool ReadError { get; private set; }

        /// <summary>ShowStandardOutputInLog</summary>
        public bool ShowOutputInLog { get; private set; }

        /// <summary>ParseOutputForUri</summary>
        public bool FindUrl { get; private set; }

        /// <summary>OutputToClipboard</summary>
        public bool CopyOutput { get; private set; }

        /// <summary>UriToClipboard: a URL found in the output is copied, instead of the output</summary>
        public bool CopyUrl { get; private set; }

        /// <summary>
        /// The configuration section, null when it isn't available (tests without the plugin's settings)
        /// </summary>
        public static IExternalCommandConfiguration Config
        {
            get
            {
                try
                {
                    return IniConfigRegistry.GetSection<IExternalCommandConfiguration>();
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The configured command, null when no command of this name is configured
        /// </summary>
        public static ConfiguredCommand Find(string name, IExternalCommandConfiguration config = null)
        {
            config ??= Config;
            if (string.IsNullOrEmpty(name) || config?.Commands == null || !config.Commands.Contains(name))
            {
                return null;
            }

            return new ConfiguredCommand
            {
                Name = name,
                Program = Get(config.CommandlineTemplate, name),
                Arguments = Get(config.ArgumentTemplate, name),
                Format = Get(config.OutputFormat, name),
                RunInBackground = config.RunInbackground != null && config.RunInbackground.TryGetValue(name, out var background) ? background : null,
                ReadOutput = PerCommand(config.RedirectStandardOutputCommand, name, config.RedirectStandardOutput),
                ReadError = PerCommand(config.RedirectStandardErrorCommand, name, config.RedirectStandardError),
                ShowOutputInLog = PerCommand(config.ShowStandardOutputInLogCommand, name, config.ShowStandardOutputInLog),
                FindUrl = PerCommand(config.ParseOutputForUriCommand, name, config.ParseOutputForUri),
                CopyOutput = PerCommand(config.OutputToClipboardCommand, name, config.OutputToClipboard),
                CopyUrl = PerCommand(config.UriToClipboardCommand, name, config.UriToClipboard)
            };
        }

        private static string Get(Dictionary<string, string> values, string name) =>
            values != null && values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

        private static bool PerCommand(Dictionary<string, bool> values, string name, bool fallback) =>
            values != null && values.TryGetValue(name, out var value) ? value : fallback;
    }
}
