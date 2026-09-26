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
    /// Metadata accompanying a browser capture import.
    /// </summary>
    public class IpcCaptureMetadata
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }

    /// <summary>
    /// Binary data payload representation for browser capture.
    /// </summary>
    public class IpcCaptureData
    {
        [JsonProperty("mime_type")]
        public string MimeType { get; set; }

        [JsonProperty("encoding")]
        public string Encoding { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }
    }

    /// <summary>
    /// Represents the length-prefixed JSON envelope matching Architecture Decision Records 002 and 003.
    /// </summary>
    public class IpcEnvelope
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("extension_version")]
        public string ExtensionVersion { get; set; }

        [JsonProperty("browser")]
        public string Browser { get; set; }

        [JsonProperty("raw_input")]
        public string RawInput { get; set; }

        [JsonProperty("parsed")]
        public IpcParsedCommand Parsed { get; set; } = new IpcParsedCommand();

        [JsonProperty("metadata")]
        public IpcCaptureMetadata Metadata { get; set; }

        [JsonProperty("data")]
        public IpcCaptureData Data { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("recipe")]
        public string Recipe { get; set; }

        [JsonProperty("files")]
        public List<string> Files { get; set; } = new List<string>();

        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        [JsonProperty("async")]
        public bool Async { get; set; }

        [JsonProperty("cwd")]
        public string Cwd { get; set; }

        public static IpcEnvelope CreateListRecipes()
        {
            return new IpcEnvelope
            {
                Version = 1,
                Source = "cli",
                Command = "LIST_RECIPES"
            };
        }

        public static IpcEnvelope CreateRunRecipe(string recipe, Dictionary<string, string> parameters = null, bool isAsync = false)
        {
            var env = new IpcEnvelope
            {
                Version = 1,
                Source = "cli",
                Command = "RUN_RECIPE",
                Recipe = recipe,
                Async = isAsync
            };
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    env.Parameters[kvp.Key] = kvp.Value;
                }
            }
            return env;
        }

        public static IpcEnvelope CreateOpenFiles(IEnumerable<string> filePaths)
        {
            var env = new IpcEnvelope
            {
                Version = 1,
                Source = "open_with",
                Command = "OPEN_FILE"
            };
            if (filePaths != null)
            {
                env.Files.AddRange(filePaths);
            }
            return env;
        }

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
                    {
                        { "path", "" }
                    }
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
                    {
                        { "path", "" }
                    }
                }
            };
        }
    }
}
