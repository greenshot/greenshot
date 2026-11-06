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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// What a document is and where its picture came from, written as the optional metadata.json; it never changes how
    /// anything is drawn. The names are those most formats share (Dublin Core). Saving without metadata: leave it null.
    /// </summary>
    public sealed record DocumentMetadata
    {
        public string Title { get; init; }

        public string Description { get; init; }

        public IReadOnlyList<string> Tags { get; init; }

        public DateTimeOffset? Created { get; init; }

        /// <summary>
        /// Set by GreenshotPackage.Write, like Revision
        /// </summary>
        public DateTimeOffset? Modified { get; init; }

        /// <summary>
        /// The application which wrote the document, e.g. "Greenshot 1.4.12"
        /// </summary>
        public string Generator { get; init; }

        public int? Revision { get; init; }

        /// <summary>
        /// Only when the user sets it
        /// </summary>
        public string Creator { get; init; }

        public MetadataSource Source { get; init; }

        public SessionInfo Session { get; init; }

        /// <summary>
        /// Values of plugins (keyed by plugin id), written back unchanged
        /// </summary>
        public RawJson Extensions { get; init; }

        /// <summary>
        /// Values this version doesn't know, written back unchanged
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Unknown { get; init; }

        /// <summary>
        /// The metadata as it is saved at the time: modified, the next revision, and created when it isn't known
        /// </summary>
        public DocumentMetadata Saved(DateTimeOffset now) => this with { Created = Created ?? now, Modified = now, Revision = (Revision ?? 0) + 1 };
    }

    /// <summary>
    /// How the picture came about: Type says from where (SourceTypes), Mode how. Only the values which apply are set.
    /// </summary>
    public sealed record MetadataSource(string Type)
    {
        /// <summary>
        /// For a capture: "region", "fullscreen", "window" or what a capture tool reports
        /// </summary>
        public string Mode { get; init; }

        /// <summary>
        /// When the picture was taken (a file: the date of the file)
        /// </summary>
        public DateTimeOffset? Date { get; init; }

        /// <summary>
        /// The captured area in desktop coordinates
        /// </summary>
        public RectD? Region { get; init; }

        public WindowInfo Window { get; init; }

        /// <summary>
        /// The screens the capture touches
        /// </summary>
        public IReadOnlyList<ScreenInfo> Screens { get; init; }

        /// <summary>
        /// The id of the recipe which made the capture
        /// </summary>
        public string Recipe { get; init; }

        /// <summary>
        /// The name of the file, never its path
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The clipboard format
        /// </summary>
        public string Format { get; init; }

        /// <summary>
        /// The details of a capture tool, keyed by its id
        /// </summary>
        public RawJson Extensions { get; init; }

        /// <summary>
        /// Values this version doesn't know (e.g. those of a browser), written back unchanged
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Unknown { get; init; }
    }

    public static class SourceTypes
    {
        public const string Capture = "capture";
        public const string Browser = "browser";
        public const string File = "file";
        public const string Clipboard = "clipboard";
    }

    public sealed record WindowInfo
    {
        public string Title { get; init; }

        public RectD? Bounds { get; init; }

        public ApplicationInfo Application { get; init; }
    }

    /// <summary>
    /// An application without its path: name, publisher and version from its version info, id is the AppUserModelID
    /// of a packaged app, otherwise the executable name
    /// </summary>
    public sealed record ApplicationInfo
    {
        public string Name { get; init; }

        public string Publisher { get; init; }

        public string Version { get; init; }

        public string Id { get; init; }
    }

    public sealed record ScreenInfo
    {
        public string Name { get; init; }

        public RectD? Bounds { get; init; }

        public double? Dpi { get; init; }
    }

    /// <summary>
    /// The work session the user picked when the capture was made, the session itself lives in the capture history
    /// </summary>
    public sealed record SessionInfo(Guid Id)
    {
        public string Name { get; init; }

        public WorkItem WorkItem { get; init; }
    }

    public sealed record WorkItem(string Provider, string Key)
    {
        public string Title { get; init; }

        public string Url { get; init; }
    }
}
