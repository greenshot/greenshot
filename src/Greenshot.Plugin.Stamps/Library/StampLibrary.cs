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
using System.Text;
using log4net;
using Newtonsoft.Json;

namespace Greenshot.Plugin.Stamps.Library;

/// <summary>
/// The stamps folder: the image files in it (also in sub folders) and stamps.json with their names and tags.
/// Files which are copied into the folder by hand show up with their file name and without tags.
/// Every change reads stamps.json again before it writes it, so a folder shared by several users keeps the changes of the others.
/// </summary>
public sealed class StampLibrary
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(StampLibrary));

    /// <summary>
    /// The file in the stamps folder with the names and tags
    /// </summary>
    public const string IndexFileName = "stamps.json";

    private static readonly char[] TagSeparators = { ',', ';' };
    private readonly HashSet<string> _extensions;
    private List<Stamp> _stamps = new();

    /// <param name="directory">the stamps folder, created when the first stamp is added</param>
    /// <param name="extensions">the extensions of the files which are stamps, with the dot</param>
    public StampLibrary(string directory, IEnumerable<string> extensions)
    {
        Directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
        _extensions = new HashSet<string>(extensions ?? throw new ArgumentNullException(nameof(extensions)), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The stamps folder
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// The extensions of the files which are stamps
    /// </summary>
    public IReadOnlyCollection<string> Extensions => _extensions;

    /// <summary>
    /// The stamps, sorted by name
    /// </summary>
    public IReadOnlyList<Stamp> Stamps => _stamps;

    private string IndexPath => Path.Combine(Directory, IndexFileName);

    /// <summary>
    /// Is the file a stamp, judged by its extension
    /// </summary>
    public bool IsStampFile(string path) => !string.IsNullOrEmpty(path) && _extensions.Contains(Path.GetExtension(path));

    /// <summary>
    /// Read the folder and stamps.json (again)
    /// </summary>
    public void Load()
    {
        var index = ReadIndex();
        var stamps = new List<Stamp>();
        if (System.IO.Directory.Exists(Directory))
        {
            foreach (string file in System.IO.Directory.EnumerateFiles(Directory, "*", SearchOption.AllDirectories).Where(IsStampFile))
            {
                string key = KeyOf(file);
                index.Stamps.TryGetValue(key, out var entry);
                string name = string.IsNullOrWhiteSpace(entry?.Name) ? Path.GetFileNameWithoutExtension(file) : entry.Name;
                stamps.Add(new Stamp(key, file, name, NormalizeTags(entry?.Tags)));
            }
        }

        _stamps = Sort(stamps);
    }

    /// <summary>
    /// Copy an image file into the stamps folder, a file which is already in it is only returned
    /// </summary>
    /// <returns>the new stamp</returns>
    /// <exception cref="ArgumentException">when the file isn't an image Greenshot can use as stamp</exception>
    public Stamp Import(string sourceFile)
    {
        if (!IsStampFile(sourceFile))
        {
            throw new ArgumentException($"{Path.GetExtension(sourceFile)} files can't be stamps", nameof(sourceFile));
        }

        string fullSource = Path.GetFullPath(sourceFile);
        var existing = _stamps.FirstOrDefault(stamp => string.Equals(stamp.FullPath, fullSource, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return existing;
        }

        System.IO.Directory.CreateDirectory(Directory);
        string target = UniquePath(Path.GetFileNameWithoutExtension(sourceFile), Path.GetExtension(sourceFile).ToLowerInvariant());
        File.Copy(fullSource, target);
        return AddStamp(target);
    }

    /// <summary>
    /// Add a stamp with the content of a file, e.g. an image from the clipboard
    /// </summary>
    /// <param name="name">the name, also used for the file name</param>
    /// <param name="extension">the extension of the content, with the dot</param>
    /// <param name="content">the content of the file</param>
    /// <returns>the new stamp</returns>
    public Stamp Add(string name, string extension, byte[] content)
    {
        if (!_extensions.Contains(extension))
        {
            throw new ArgumentException($"{extension} files can't be stamps", nameof(extension));
        }

        System.IO.Directory.CreateDirectory(Directory);
        string target = UniquePath(name, extension.ToLowerInvariant());
        File.WriteAllBytes(target, content ?? throw new ArgumentNullException(nameof(content)));
        return AddStamp(target);
    }

    /// <summary>
    /// Change the name and the tags of the stamp
    /// </summary>
    /// <param name="stamp">Stamp</param>
    /// <param name="name">the new name, empty for the file name</param>
    /// <param name="tags">the tags as the user enters them, see <see cref="ParseTags"/></param>
    public void Update(Stamp stamp, string name, string tags)
    {
        if (stamp == null) throw new ArgumentNullException(nameof(stamp));

        stamp.Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(stamp.FullPath) : name.Trim();
        stamp.Tags = ParseTags(tags);
        var index = ReadIndex();
        index.Stamps[stamp.Key] = new StampEntry { Name = stamp.Name, Tags = stamp.Tags.ToList() };
        WriteIndex(index);
        _stamps = Sort(_stamps);
    }

    /// <summary>
    /// Delete the file of the stamp and its entry in stamps.json
    /// </summary>
    public void Delete(Stamp stamp)
    {
        if (stamp == null) throw new ArgumentNullException(nameof(stamp));

        if (File.Exists(stamp.FullPath))
        {
            File.Delete(stamp.FullPath);
        }

        var index = ReadIndex();
        if (index.Stamps.Remove(stamp.Key))
        {
            WriteIndex(index);
        }

        _stamps = _stamps.Where(other => other != stamp).ToList();
    }

    /// <summary>
    /// The tags of the text the user entered: separated by commas or semicolons, trimmed, each once ignoring the case
    /// </summary>
    public static IReadOnlyList<string> ParseTags(string text) =>
        NormalizeTags(text?.Split(TagSeparators, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The tags as the user enters them
    /// </summary>
    public static string FormatTags(IEnumerable<string> tags) => string.Join(", ", tags ?? Enumerable.Empty<string>());

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags) =>
        (tags ?? Enumerable.Empty<string>())
        .Select(tag => tag?.Trim())
        .Where(tag => !string.IsNullOrEmpty(tag))
        .Distinct(StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    private Stamp AddStamp(string path)
    {
        var stamp = new Stamp(KeyOf(path), path, Path.GetFileNameWithoutExtension(path), Array.Empty<string>());
        _stamps = Sort(_stamps.Append(stamp));
        return stamp;
    }

    private static List<Stamp> Sort(IEnumerable<Stamp> stamps) =>
        stamps.OrderBy(stamp => stamp.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(stamp => stamp.Key, StringComparer.OrdinalIgnoreCase).ToList();

    private string KeyOf(string path) =>
        path.Substring(Directory.TrimEnd(Path.DirectorySeparatorChar).Length + 1).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>
    /// A path in the stamps folder for a new file: the name without the characters a file name can't have, with " (2)" etc. when it is taken
    /// </summary>
    private string UniquePath(string name, string extension)
    {
        var invalid = Path.GetInvalidFileNameChars();
        string baseName = new string((name ?? string.Empty).Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        if (baseName.Length == 0)
        {
            baseName = "Stamp";
        }

        string path = Path.Combine(Directory, baseName + extension);
        for (int number = 2; File.Exists(path); number++)
        {
            path = Path.Combine(Directory, $"{baseName} ({number}){extension}");
        }

        return path;
    }

    private StampIndex ReadIndex()
    {
        try
        {
            if (File.Exists(IndexPath))
            {
                var index = JsonConvert.DeserializeObject<StampIndex>(File.ReadAllText(IndexPath, Encoding.UTF8));
                if (index?.Stamps != null)
                {
                    return new StampIndex { Stamps = new Dictionary<string, StampEntry>(index.Stamps, StringComparer.OrdinalIgnoreCase) };
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // The stamps still show, with their file names
            Log.Warn($"Can't read the names and tags of the stamps from {IndexPath}", ex);
        }

        return new StampIndex();
    }

    private void WriteIndex(StampIndex index)
    {
        // Only the files which are still there
        foreach (string key in index.Stamps.Keys.Where(key => !File.Exists(Path.Combine(Directory, key))).ToList())
        {
            index.Stamps.Remove(key);
        }

        System.IO.Directory.CreateDirectory(Directory);
        // Written next to it and then swapped, so a crash or a full disk doesn't leave half a file
        string temporary = IndexPath + ".tmp";
        File.WriteAllText(temporary, JsonConvert.SerializeObject(index, Formatting.Indented), new UTF8Encoding(false));
        if (File.Exists(IndexPath))
        {
            File.Replace(temporary, IndexPath, null);
        }
        else
        {
            File.Move(temporary, IndexPath);
        }
    }

    /// <summary>
    /// The content of stamps.json
    /// </summary>
    private sealed class StampIndex
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("stamps")]
        public Dictionary<string, StampEntry> Stamps { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class StampEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }
    }
}
