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

using System.Text;

namespace Greenshot.Linux.Core.Configuration;

/// <summary>
/// A small reader and writer for the greenshot.ini format: [Section], Key=Value and ; comments.
/// Unknown sections and keys are kept, so a file shared with the Windows version survives a save.
/// </summary>
public sealed class IniFile
{
    private readonly List<Section> _sections = new();

    private sealed class Section(string name)
    {
        public string Name { get; } = name;
        public List<string> Comments { get; } = new();
        public List<KeyValuePair<string, string>> Values { get; } = new();
    }

    public static IniFile Parse(string text)
    {
        var ini = new IniFile();
        Section? current = null;
        var pendingComments = new List<string>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith(';') || trimmed.StartsWith('#'))
            {
                pendingComments.Add(trimmed);
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                current = ini.GetOrAddSection(trimmed[1..^1].Trim());
                current.Comments.AddRange(pendingComments);
                pendingComments.Clear();
                continue;
            }

            int equals = trimmed.IndexOf('=');
            if (equals <= 0 || current == null)
            {
                continue;
            }

            pendingComments.Clear();
            var key = trimmed[..equals].Trim();
            var value = trimmed[(equals + 1)..].Trim();
            ini.Set(current.Name, key, value);
        }

        return ini;
    }

    public static IniFile Load(string path) => File.Exists(path) ? Parse(File.ReadAllText(path, Encoding.UTF8)) : new IniFile();

    public string? Get(string section, string key)
    {
        var s = FindSection(section);
        if (s == null)
        {
            return null;
        }

        foreach (var pair in s.Values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public void Set(string section, string key, string value)
    {
        var s = GetOrAddSection(section);
        for (int i = 0; i < s.Values.Count; i++)
        {
            if (string.Equals(s.Values[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                s.Values[i] = new KeyValuePair<string, string>(s.Values[i].Key, value);
                return;
            }
        }

        s.Values.Add(new KeyValuePair<string, string>(key, value));
    }

    /// <summary>
    /// The keys and values of a section, empty when there is none
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> GetSection(string section) => FindSection(section)?.Values.ToList() ?? [];

    public void Remove(string section, string key) =>
        FindSection(section)?.Values.RemoveAll(pair => string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase));

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var section in _sections)
        {
            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            foreach (var comment in section.Comments)
            {
                sb.Append(comment).Append('\n');
            }

            sb.Append('[').Append(section.Name).Append("]\n");
            foreach (var pair in section.Values)
            {
                sb.Append(pair.Key).Append('=').Append(pair.Value).Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Write the file through a temporary file, so a crash while saving never leaves a half written configuration.
    /// </summary>
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, ToString(), new UTF8Encoding(false));
        File.Move(temp, path, overwrite: true);
    }

    private Section? FindSection(string name) =>
        _sections.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    private Section GetOrAddSection(string name)
    {
        var s = FindSection(name);
        if (s == null)
        {
            s = new Section(name);
            _sections.Add(s);
        }

        return s;
    }
}
