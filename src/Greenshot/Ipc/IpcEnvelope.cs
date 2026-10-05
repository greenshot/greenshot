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
using System.Linq;
using Newtonsoft.Json;

namespace Greenshot.Ipc
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
    /// <summary>
    /// Connection sources. A connection announces its source once, in the HELLO frame sent by the proxy (or by Greenshot itself);
    /// the server then ignores the "source" field of all later envelopes on that connection, so data relayed from a browser
    /// can never claim to come from the command line.
    /// </summary>
    public static class IpcSources
    {
        public const string HelloCommand = "HELLO";

        /// <summary>
        /// Raw command line from greenshot-cli.exe / greenshot-proxy.exe; parsed by <see cref="CliCommandParser"/> according to the connection source.
        /// </summary>
        public const string CliCommand = "CLI";

        /// <summary>
        /// Reply formats announced in HELLO: JSON objects (browser extension, default) or text frames (terminal / shell).
        /// </summary>
        public const string ReplyFormatJson = "json";
        public const string ReplyFormatText = "text";

        public const string Cli = "cli";
        public const string OpenWith = "open_with";
        public const string UrlScheme = "url_scheme";
        public const string NativeMessaging = "native_messaging";

        /// <summary>
        /// greenshot-mcp.exe, the MCP server for AI tools. HELLO origin carries the AI tool's name.
        /// </summary>
        public const string Mcp = "mcp";

        public static bool IsKnown(string source)
        {
#if GREENSHOT_LIGHT
            // Greenshot Light has no browser extension and no AI tools: their connections are rejected at HELLO
            return string.Equals(source, Cli, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(source, OpenWith, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(source, UrlScheme, StringComparison.OrdinalIgnoreCase);
#else
            return string.Equals(source, Cli, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(source, OpenWith, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(source, UrlScheme, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(source, NativeMessaging, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(source, Mcp, StringComparison.OrdinalIgnoreCase);
#endif
        }
    }

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

        /// <summary>
        /// HELLO only: the calling browser extension's origin, as passed to the proxy by the browser.
        /// </summary>
        [JsonProperty("origin")]
        public string Origin { get; set; }

        /// <summary>
        /// HELLO only: "json" (default) or "text", see <see cref="IpcSources.ReplyFormatText"/>.
        /// </summary>
        [JsonProperty("reply_format")]
        public string ReplyFormat { get; set; }

        /// <summary>
        /// CLI only: the unparsed command line arguments (without the executable name).
        /// </summary>
        [JsonProperty("argv")]
        public List<string> Argv { get; set; }

        /// <summary>
        /// RUN_RECIPE only: expression evaluated after the recipe finished; its value replaces the recipe's own output (--query).
        /// </summary>
        [JsonProperty("query")]
        public string Query { get; set; }

        /// <summary>
        /// RUN_RECIPE only: return a structured JSON result (status, exit code, output, variables) instead of streaming (--json).
        /// </summary>
        [JsonProperty("json")]
        public bool Json { get; set; }

        /// <summary>
        /// True when this envelope is the HELLO frame that opens every connection.
        /// </summary>
        [JsonIgnore]
        public bool IsHello => string.Equals(Command, IpcSources.HelloCommand, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Creates the HELLO frame that must be the first frame on every connection. It binds the connection's source.
        /// </summary>
        public static IpcEnvelope CreateHello(string source)
        {
            return new IpcEnvelope
            {
                Version = 1,
                Command = IpcSources.HelloCommand,
                Source = source
            };
        }

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

        /// <summary>
        /// Creates an OPEN_FILE command, as <see cref="CliCommandParser"/> creates it for the file arguments of a command line.
        /// </summary>
        public static IpcEnvelope CreateOpenFiles(IEnumerable<string> filePaths, string source = IpcSources.OpenWith)
        {
            var env = new IpcEnvelope
            {
                Version = 1,
                Source = source,
                Command = "OPEN_FILE"
            };
            if (filePaths != null)
            {
                env.Files.AddRange(filePaths);
            }
            return env;
        }

        /// <summary>
        /// Creates a CLI request: an unparsed command line, which Greenshot parses with <see cref="CliCommandParser"/>.
        /// This is what greenshot-cli.exe and greenshot-proxy.exe send, and what Greenshot.exe sends for its command arguments.
        /// </summary>
        public static IpcEnvelope CreateCli(IEnumerable<string> argv, string source, string cwd)
        {
            return new IpcEnvelope
            {
                Version = 1,
                Source = source,
                Command = IpcSources.CliCommand,
                Cwd = cwd,
                Argv = argv?.ToList() ?? new List<string>()
            };
        }
    }
}
