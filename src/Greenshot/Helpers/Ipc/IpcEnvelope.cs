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
using Newtonsoft.Json;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Represents the length-prefixed JSON envelope matching Architecture Decision Record 002.
    /// </summary>
    public class IpcEnvelope
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("raw_input")]
        public string RawInput { get; set; }

        [JsonProperty("parsed")]
        public IpcParsedCommand Parsed { get; set; } = new IpcParsedCommand();

        public static IpcEnvelope CreateOpenFile(string filePath)
        {
            return new IpcEnvelope
            {
                Version = 1,
                Source = "open_with",
                RawInput = filePath,
                Parsed = new IpcParsedCommand
                {
                    Action = "open_file",
                    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "path", filePath }
                    }
                }
            };
        }

        public static IpcEnvelope CreateExit()
        {
            return new IpcEnvelope
            {
                Version = 1,
                Source = "cli",
                RawInput = "--exit",
                Parsed = new IpcParsedCommand
                {
                    Action = "exit",
                    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                }
            };
        }

        public static IpcEnvelope CreateReloadConfig()
        {
            return new IpcEnvelope
            {
                Version = 1,
                Source = "cli",
                RawInput = "--reload",
                Parsed = new IpcParsedCommand
                {
                    Action = "reload_config",
                    Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                }
            };
        }
    }
}
