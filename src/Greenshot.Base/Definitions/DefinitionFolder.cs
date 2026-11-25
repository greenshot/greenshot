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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Greenshot.Base.Core;
using Greenshot.Base.Trust;
using log4net;

namespace Greenshot.Base.Definitions;

/// <summary>
/// Gives the id of a new definition, e.g. a file name made of its name
/// </summary>
public interface IDefinitionIds
{
    /// <summary>
    /// A new id for a definition with the name, not used yet
    /// </summary>
    string NewId(string name);
}

/// <summary>
/// Definitions the user has to approve before they are used, e.g. files which were put into the folder outside Greenshot
/// </summary>
public interface IDefinitionApprovals
{
    /// <summary>
    /// True when the user approved the definition as it is
    /// </summary>
    bool IsApproved(string id);

    /// <summary>
    /// The user approved the definition as it is (as it was read)
    /// </summary>
    void Approve(string id);
}

/// <summary>
/// The definitions of a kind as files in a folder, for a <see cref="DefinitionStore{T}"/>: the id is the file name, the value its
/// content (UTF-8). The folder is read once, with the first access: a file added, changed or removed outside Greenshot later
/// has no effect until Greenshot is started again. What Greenshot writes (import, editor, the conversion of the 1.3 settings) is
/// approved with the hash of its content. A file Greenshot never wrote (put into the folder by somebody else) is ignored, the
/// user imports it; one Greenshot wrote which was changed outside Greenshot is not approved (<see cref="IDefinitionApprovals"/>)
/// until the user approves it. A file which can't be read has the value null.
/// </summary>
public sealed class DefinitionFolder : IDictionary<string, string>, IDefinitionIds, IDefinitionApprovals
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(DefinitionFolder));
    private static readonly Encoding Utf8 = new UTF8Encoding(false);
    private static readonly Dictionary<string, DefinitionFolder> Opened = new Dictionary<string, DefinitionFolder>(StringComparer.OrdinalIgnoreCase);
    private readonly long _maximumLength;
    private readonly IFileApprovals _approvals;
    private readonly object _lock = new object();
    private Dictionary<string, Entry> _entries;

    private sealed class Entry
    {
        public Entry(string content, string sha256)
        {
            Content = content;
            Sha256 = sha256;
        }

        /// <summary>
        /// Null when the file can't be read
        /// </summary>
        public string Content { get; }

        public string Sha256 { get; }
    }

    /// <param name="directory">the folder, created with the first definition</param>
    /// <param name="extension">the extension of the files, e.g. ".sxcu"</param>
    /// <param name="maximumLength">larger files aren't read (their value is null)</param>
    /// <param name="approvals">the approvals of the files, null: <see cref="FileApprovals.Definitions"/></param>
    public DefinitionFolder(string directory, string extension, long maximumLength, IFileApprovals approvals = null)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("No folder", nameof(directory));
        if (string.IsNullOrWhiteSpace(extension) || !extension.StartsWith(".", StringComparison.Ordinal)) throw new ArgumentException("No extension", nameof(extension));
        Directory = directory;
        Extension = extension;
        _maximumLength = maximumLength;
        _approvals = approvals ?? FileApprovals.Definitions;
    }

    /// <summary>
    /// The folder of this Greenshot: one per folder and extension, read once, shared by every store on it
    /// </summary>
    public static DefinitionFolder Open(string directory, string extension, long maximumLength)
    {
        lock (Opened)
        {
            string key = Path.GetFullPath(directory) + "|" + extension;
            if (!Opened.TryGetValue(key, out var folder))
            {
                folder = new DefinitionFolder(directory, extension, maximumLength);
                Opened[key] = folder;
            }

            return folder;
        }
    }

    public string Directory { get; }

    public string Extension { get; }

    /// <summary>
    /// The folder of a kind next to greenshot.ini (like the Recipes and Stamps folders), so portable installs keep it next to the executable
    /// </summary>
    public static string NextToConfiguration(string folderName)
    {
        string iniDirectory = Path.GetDirectoryName(GreenshotEnvironment.ConfigLocation);
        if (string.IsNullOrEmpty(iniDirectory))
        {
            iniDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Greenshot");
        }

        return Path.Combine(iniDirectory, folderName);
    }

    /// <summary>
    /// The file name of the name (characters a file name can't have replaced), with " (2)", " (3)" when it is taken, also by a file
    /// which was put into the folder after it was read
    /// </summary>
    public string NewId(string name)
    {
        string fileName = DefinitionImport.FileNameFor(name, Extension);
        string stem = fileName.Substring(0, fileName.Length - Extension.Length);
        for (int number = 2; ContainsKey(fileName) || File.Exists(PathOf(fileName)); number++)
        {
            fileName = $"{stem} ({number}){Extension}";
        }

        return fileName;
    }

    public bool IsApproved(string id)
    {
        lock (_lock)
        {
            return id != null && Entries().TryGetValue(id, out var entry) && entry.Content != null && _approvals.IsApproved(PathOf(id), entry.Sha256);
        }
    }

    public void Approve(string id)
    {
        lock (_lock)
        {
            if (id != null && Entries().TryGetValue(id, out var entry) && entry.Content != null)
            {
                _approvals.Approve(PathOf(id), entry.Sha256);
            }
        }
    }

    private string PathOf(string key)
    {
        // Only a file name in the folder, nothing which leaves it
        if (string.IsNullOrEmpty(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !key.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Not a file name of {Extension}: {key}", nameof(key));
        }

        return Path.Combine(Directory, key);
    }

    private static bool IsFileName(string key, string extension) =>
        !string.IsNullOrEmpty(key) && key.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && key.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The files as they were when the folder was read the first time
    /// </summary>
    private Dictionary<string, Entry> Entries()
    {
        if (_entries != null) return _entries;

        _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                return _entries;
            }

            foreach (string fileName in System.IO.Directory.EnumerateFiles(Directory, "*" + Extension).Select(Path.GetFileName)
                         // EnumerateFiles also finds "a.sxcu_old" for "*.sxcu" (8.3 names)
                         .Where(name => name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)))
            {
                // Only what Greenshot wrote or imported: a file somebody put into the folder is ignored, the user imports it
                if (!_approvals.IsKnown(PathOf(fileName)))
                {
                    Log.InfoFormat("{0} wasn't written or imported by Greenshot, it is ignored (import it to use it)", PathOf(fileName));
                    continue;
                }

                _entries[fileName] = ReadFile(fileName);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't read the folder {Directory}", ex);
        }

        return _entries;
    }

    private Entry ReadFile(string key)
    {
        string path = PathOf(key);
        try
        {
            var info = new FileInfo(path);
            if (info.Length > _maximumLength)
            {
                Log.WarnFormat("{0} is larger than {1} bytes, it isn't read", path, _maximumLength);
                return new Entry(null, null);
            }

            // The hash of exactly the bytes which are shown and used
            byte[] bytes = File.ReadAllBytes(path);
            using var reader = new StreamReader(new MemoryStream(bytes), Utf8, true);
            return new Entry(reader.ReadToEnd(), ContentHash.Sha256(bytes));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't read {path}", ex);
            return new Entry(null, null);
        }
    }

    /// <summary>
    /// Written to a new file first, which then takes the place of the old one: a definition is never half written. Greenshot wrote
    /// it, so it is approved.
    /// </summary>
    /// <exception cref="IOException">the file can't be written</exception>
    /// <exception cref="UnauthorizedAccessException">the file can't be written</exception>
    private void WriteFile(string key, string content)
    {
        string path = PathOf(key);
        byte[] bytes = Utf8.GetBytes(content ?? string.Empty);
        lock (_lock)
        {
            System.IO.Directory.CreateDirectory(Directory);
            string temporary = path + ".new";
            File.WriteAllBytes(temporary, bytes);
            if (File.Exists(path))
            {
                File.Replace(temporary, path, null);
            }
            else
            {
                File.Move(temporary, path);
            }

            var entry = new Entry(content ?? string.Empty, ContentHash.Sha256(bytes));
            Entries()[key] = entry;
            _approvals.Approve(path, entry.Sha256);
        }
    }

    public string this[string key]
    {
        get => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(key);
        set => WriteFile(key, value);
    }

    public ICollection<string> Keys
    {
        get
        {
            lock (_lock) return Entries().Keys.ToList();
        }
    }

    public ICollection<string> Values
    {
        get
        {
            lock (_lock) return Entries().Values.Select(entry => entry.Content).ToList();
        }
    }

    public int Count
    {
        get
        {
            lock (_lock) return Entries().Count;
        }
    }

    public bool IsReadOnly => false;

    public void Add(string key, string value)
    {
        if (ContainsKey(key)) throw new ArgumentException($"{key} exists", nameof(key));
        WriteFile(key, value);
    }

    public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Not supported: the files of the folder aren't all removed at once
    /// </summary>
    public void Clear() => throw new NotSupportedException("The definition files are removed one by one");

    public bool Contains(KeyValuePair<string, string> item) => TryGetValue(item.Key, out var value) && value == item.Value;

    public bool ContainsKey(string key)
    {
        if (!IsFileName(key, Extension)) return false;
        lock (_lock) return Entries().ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => this.ToList().CopyTo(array, arrayIndex);

    /// <summary>
    /// Delete the file and its approval
    /// </summary>
    /// <exception cref="IOException">the file can't be deleted</exception>
    /// <exception cref="UnauthorizedAccessException">the file can't be deleted</exception>
    public bool Remove(string key)
    {
        if (!ContainsKey(key)) return false;
        string path = PathOf(key);
        lock (_lock)
        {
            File.Delete(path);
            Entries().Remove(key);
            _approvals.Revoke(path);
        }

        return true;
    }

    public bool Remove(KeyValuePair<string, string> item) => Contains(item) && Remove(item.Key);

    public bool TryGetValue(string key, out string value)
    {
        value = null;
        if (!IsFileName(key, Extension)) return false;
        lock (_lock)
        {
            if (!Entries().TryGetValue(key, out var entry)) return false;
            value = entry.Content;
            return true;
        }
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        lock (_lock)
        {
            return Entries().Select(entry => new KeyValuePair<string, string>(entry.Key, entry.Value.Content)).ToList().GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
