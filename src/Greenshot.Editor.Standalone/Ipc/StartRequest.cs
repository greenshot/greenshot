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
using System.IO;
using System.Text.Json.Serialization;

namespace Greenshot.Editor.Standalone.Ipc
{
    /// <summary>
    /// What an editor is started for: files to open, and captures Greenshot hands over as files
    /// (greenshot-editor.exe [--handover file]... [file]...)
    /// </summary>
    public sealed class StartRequest
    {
        public const string HandoverArgument = "--handover";

        /// <summary>
        /// Documents and images to open
        /// </summary>
        [JsonPropertyName("files")]
        public List<string> Files { get; set; } = new();

        /// <summary>
        /// Documents in the handover folder: they are opened as new documents and the files are removed
        /// </summary>
        [JsonPropertyName("handover")]
        public List<string> Handover { get; set; } = new();

        public static StartRequest Parse(string[] arguments)
        {
            var request = new StartRequest();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i] == HandoverArgument)
                {
                    if (++i < arguments.Length)
                    {
                        request.Handover.Add(Path.GetFullPath(arguments[i]));
                    }
                }
                else if (!arguments[i].StartsWith("--", System.StringComparison.Ordinal))
                {
                    request.Files.Add(Path.GetFullPath(arguments[i]));
                }
            }

            return request;
        }
    }

    [JsonSerializable(typeof(StartRequest))]
    internal sealed partial class StartRequestJson : JsonSerializerContext
    {
    }
}
