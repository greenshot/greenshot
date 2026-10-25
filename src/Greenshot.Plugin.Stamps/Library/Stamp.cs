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

namespace Greenshot.Plugin.Stamps.Library;

/// <summary>
/// A stamp: an image file in the stamps folder, with the name and the tags the user gave it
/// </summary>
public sealed class Stamp
{
    internal Stamp(string key, string fullPath, string name, IReadOnlyList<string> tags)
    {
        Key = key;
        FullPath = fullPath;
        Name = name;
        Tags = tags;
    }

    /// <summary>
    /// The path of the file relative to the stamps folder, with / between the folders: the key in stamps.json
    /// </summary>
    public string Key { get; internal set; }

    /// <summary>
    /// The full path of the file
    /// </summary>
    public string FullPath { get; internal set; }

    /// <summary>
    /// The category: the folder of the file in the stamps folder, with / between sub folders. Empty for the stamps folder itself.
    /// </summary>
    public string Category
    {
        get
        {
            int separator = Key.LastIndexOf('/');
            return separator < 0 ? string.Empty : Key.Substring(0, separator);
        }
    }

    /// <summary>
    /// The extension of the file, lower case with the dot
    /// </summary>
    public string Extension => Path.GetExtension(FullPath).ToLowerInvariant();

    /// <summary>
    /// The name shown, the file name without extension until the user changes it
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// The tags, each once (ignoring the case), in the order they were entered
    /// </summary>
    public IReadOnlyList<string> Tags { get; internal set; }

    /// <summary>
    /// Does the stamp match the search: every word of it is part of the name, of a tag, of the category or of the file name (ignoring the case).
    /// An empty search matches every stamp.
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

    private static bool Contains(string text, string word) => text != null && text.IndexOf(word, StringComparison.CurrentCultureIgnoreCase) >= 0;

    public override string ToString() => Name;
}
