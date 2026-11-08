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
/// The definitions of a kind as files in a folder, for a <see cref="DefinitionStore{T}"/>: the id is the file name, the value its
/// content (UTF-8). Each read goes to the folder, so a file changed, added or removed by hand is seen at once; the store checks
/// each one like an imported file. A file which can't be read has the value null.
/// </summary>
public sealed class DefinitionFolder : IDictionary<string, string>, IDefinitionIds
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(DefinitionFolder));
    private static readonly Encoding Utf8 = new UTF8Encoding(false);
    private readonly long _maximumLength;

    /// <param name="directory">the folder, created with the first definition</param>
    /// <param name="extension">the extension of the files, e.g. ".sxcu"</param>
    /// <param name="maximumLength">larger files aren't read (their value is null)</param>
    public DefinitionFolder(string directory, string extension, long maximumLength)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("No folder", nameof(directory));
        if (string.IsNullOrWhiteSpace(extension) || !extension.StartsWith(".", StringComparison.Ordinal)) throw new ArgumentException("No extension", nameof(extension));
        Directory = directory;
        Extension = extension;
        _maximumLength = maximumLength;
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
    /// The file name of the name (characters a file name can't have replaced), with " (2)", " (3)" when it is taken
    /// </summary>
    public string NewId(string name)
    {
        string fileName = DefinitionImport.FileNameFor(name, Extension);
        string stem = fileName.Substring(0, fileName.Length - Extension.Length);
        for (int number = 2; ContainsKey(fileName); number++)
        {
            fileName = $"{stem} ({number}){Extension}";
        }

        return fileName;
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

    private IEnumerable<string> FileNames()
    {
        try
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                return Array.Empty<string>();
            }

            return System.IO.Directory.EnumerateFiles(Directory, "*" + Extension)
                .Select(Path.GetFileName)
                // EnumerateFiles also finds "a.sxcu_old" for "*.sxcu" (8.3 names)
                .Where(name => name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't read the folder {Directory}", ex);
            return Array.Empty<string>();
        }
    }

    private string ReadFile(string key)
    {
        string path = PathOf(key);
        try
        {
            var info = new FileInfo(path);
            if (info.Length > _maximumLength)
            {
                Log.WarnFormat("{0} is larger than {1} bytes, it isn't read", path, _maximumLength);
                return null;
            }

            return File.ReadAllText(path, Utf8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't read {path}", ex);
            return null;
        }
    }

    /// <summary>
    /// Written to a new file first, which then takes the place of the old one: a definition is never half written
    /// </summary>
    /// <exception cref="IOException">the file can't be written</exception>
    /// <exception cref="UnauthorizedAccessException">the file can't be written</exception>
    private void WriteFile(string key, string content)
    {
        string path = PathOf(key);
        System.IO.Directory.CreateDirectory(Directory);
        string temporary = path + ".new";
        File.WriteAllText(temporary, content ?? string.Empty, Utf8);
        if (File.Exists(path))
        {
            File.Replace(temporary, path, null);
        }
        else
        {
            File.Move(temporary, path);
        }
    }

    public string this[string key]
    {
        get => ContainsKey(key) ? ReadFile(key) : throw new KeyNotFoundException(key);
        set => WriteFile(key, value);
    }

    public ICollection<string> Keys => FileNames().ToList();

    public ICollection<string> Values => FileNames().Select(ReadFile).ToList();

    public int Count => FileNames().Count();

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
        try
        {
            return File.Exists(PathOf(key));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => this.ToList().CopyTo(array, arrayIndex);

    /// <summary>
    /// Delete the file
    /// </summary>
    /// <exception cref="IOException">the file can't be deleted</exception>
    /// <exception cref="UnauthorizedAccessException">the file can't be deleted</exception>
    public bool Remove(string key)
    {
        if (!ContainsKey(key)) return false;
        File.Delete(PathOf(key));
        return true;
    }

    public bool Remove(KeyValuePair<string, string> item) => Contains(item) && Remove(item.Key);

    public bool TryGetValue(string key, out string value)
    {
        if (!ContainsKey(key))
        {
            value = null;
            return false;
        }

        value = ReadFile(key);
        return true;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
        FileNames().Select(key => new KeyValuePair<string, string>(key, ReadFile(key))).ToList().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
