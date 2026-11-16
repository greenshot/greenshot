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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenshot.Editor.Standalone.Diagnostics;

namespace Greenshot.Editor.Standalone.Stamps
{
    /// <summary>
    /// A stamp: an image file in the stamps folder, with the name and the tags it has in stamps.json
    /// </summary>
    public sealed record Stamp(string Key, string FullPath, string Name, IReadOnlyList<string> Tags)
    {
        /// <summary>
        /// The category: the folder of the file in the stamps folder, with / between sub folders. Empty for the stamps folder itself.
        /// </summary>
        public string Category => Key.LastIndexOf('/') is int separator and >= 0 ? Key.Substring(0, separator) : string.Empty;

        /// <summary>
        /// Does the stamp match the search: every word of it is part of the name, of a tag, of the category or of the file name
        /// (ignoring the case). An empty search matches every stamp.
        /// </summary>
        public bool Matches(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            string fileName = Path.GetFileNameWithoutExtension(FullPath);
            return search.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                .All(word => Contains(Name, word) || Contains(fileName, word) || Contains(Category, word) || Tags.Any(tag => Contains(tag, word)));
        }

        private static bool Contains(string text, string word) => text != null && text.Contains(word, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// The stamps folder: the image files in it (also in sub folders, which are the categories) and stamps.json with their
    /// names and tags. It is the folder and the file format of the stamps of Greenshot, so both show the same stamps.
    /// The editor adds image files to it; names and tags are only read.
    /// </summary>
    public sealed class StampLibrary
    {
        public const string IndexFileName = "stamps.json";

        /// <summary>
        /// The files which are stamps: what the editor can show and draw
        /// </summary>
        public static readonly IReadOnlyCollection<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

        public StampLibrary(string directory)
        {
            Directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
        }

        public string Directory { get; }

        /// <summary>
        /// The stamps, sorted by category and name
        /// </summary>
        public IReadOnlyList<Stamp> Stamps { get; private set; } = Array.Empty<Stamp>();

        public static bool IsStampFile(string path) => !string.IsNullOrEmpty(path) && Extensions.Contains(Path.GetExtension(path));

        /// <summary>
        /// Reads the folder and stamps.json (again)
        /// </summary>
        public void Load()
        {
            var index = ReadIndex();
            var stamps = new List<Stamp>();
            if (System.IO.Directory.Exists(Directory))
            {
                foreach (string file in System.IO.Directory.EnumerateFiles(Directory, "*", SearchOption.AllDirectories).Where(IsStampFile))
                {
                    string key = Path.GetRelativePath(Directory, file).Replace(Path.DirectorySeparatorChar, '/');
                    index.Stamps.TryGetValue(key, out var entry);
                    string name = string.IsNullOrWhiteSpace(entry?.Name) ? Path.GetFileNameWithoutExtension(file) : entry.Name;
                    stamps.Add(new Stamp(key, file, name, entry?.Tags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList() ?? new List<string>()));
                }
            }

            Stamps = stamps.OrderBy(stamp => stamp.Category, StringComparer.CurrentCultureIgnoreCase).ThenBy(stamp => stamp.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        /// <summary>
        /// Copies an image file into the stamps folder, a file which is already in it is only returned
        /// </summary>
        public Stamp Import(string sourceFile)
        {
            if (!IsStampFile(sourceFile))
            {
                throw new ArgumentException($"{Path.GetExtension(sourceFile)} files can't be stamps", nameof(sourceFile));
            }

            string source = Path.GetFullPath(sourceFile);
            if (!source.StartsWith(Directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                System.IO.Directory.CreateDirectory(Directory);
                string target = FreePath(Path.GetFileNameWithoutExtension(source), Path.GetExtension(source).ToLowerInvariant());
                File.Copy(source, target);
                source = target;
            }

            Load();
            return Stamps.FirstOrDefault(stamp => string.Equals(stamp.FullPath, source, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds a PNG image as a new stamp of the name
        /// </summary>
        public Stamp Add(byte[] png, string name)
        {
            System.IO.Directory.CreateDirectory(Directory);
            var invalid = Path.GetInvalidFileNameChars();
            string safe = new string((name ?? string.Empty).Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray()).Trim();
            string target = FreePath(safe.Length == 0 ? "stamp" : safe, ".png");
            File.WriteAllBytes(target, png);
            Load();
            return Stamps.FirstOrDefault(stamp => string.Equals(stamp.FullPath, target, StringComparison.OrdinalIgnoreCase));
        }

        private string FreePath(string name, string extension)
        {
            string path = Path.Combine(Directory, name + extension);
            for (int i = 2; File.Exists(path); i++)
            {
                path = Path.Combine(Directory, $"{name} {i}{extension}");
            }

            return path;
        }

        private StampIndex ReadIndex()
        {
            string path = Path.Combine(Directory, IndexFileName);
            try
            {
                if (File.Exists(path))
                {
                    var index = JsonSerializer.Deserialize(File.ReadAllText(path), StampJson.Default.StampIndex);
                    if (index != null)
                    {
                        index.Stamps = new Dictionary<string, StampEntry>(index.Stamps ?? new Dictionary<string, StampEntry>(), StringComparer.OrdinalIgnoreCase);
                        return index;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // The stamps show with their file names
                EditorLog.Warn($"{path} could not be read", ex);
            }

            return new StampIndex();
        }
    }

    internal sealed class StampIndex
    {
        [JsonPropertyName("stamps")]
        public Dictionary<string, StampEntry> Stamps { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class StampEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }
    }

    [JsonSerializable(typeof(StampIndex))]
    internal sealed partial class StampJson : JsonSerializerContext
    {
    }
}
