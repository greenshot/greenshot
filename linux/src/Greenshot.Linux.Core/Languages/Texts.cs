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

using System.Globalization;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core.Configuration;

namespace Greenshot.Linux.Core.Languages;

/// <summary>
/// A language Greenshot can show, from the [__language__] section of its file
/// </summary>
public sealed record LanguageInfo(string Ietf, string Description);

/// <summary>
/// The language packs of Greenshot: greenshot.&lt;ietf&gt;.ini from src/Greenshot/Languages and the plugins (shared with Windows,
/// sections [Core], [Editor], [Settings], [Welcome], ... with snake_case keys) and greenshot-linux.&lt;ietf&gt;.ini
/// for the texts only the Linux version has. A text missing in a language comes from en-US.
/// </summary>
public static partial class Texts
{
    public const string DefaultLanguage = "en-US";

    private static readonly object Lock = new();
    private static Dictionary<string, List<string>> _files = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, Dictionary<string, string>> _current = new();
    private static Dictionary<string, Dictionary<string, string>> _fallback = new();

    /// <summary>
    /// greenshot.de-DE.ini, greenshot-linux.de-DE.ini and the plugins' greenshot.box.de-DE.ini
    /// </summary>
    [GeneratedRegex(@"^greenshot(?:-linux|\.[A-Za-z]+)?\.(?<ietf>[A-Za-z]{2,3}(-[A-Za-z0-9]+)*)\.ini$")]
    private static partial Regex FileNameRegex();

    /// <summary>
    /// The language in use, e.g. de-DE
    /// </summary>
    public static string Current { get; private set; } = DefaultLanguage;

    /// <summary>
    /// The culture of the language, the current culture when .NET doesn't know it (e.g. de-x-franconia)
    /// </summary>
    public static CultureInfo Culture
    {
        get
        {
            try
            {
                return CultureInfo.GetCultureInfo(Current);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.CurrentCulture;
            }
        }
    }

    /// <summary>
    /// The folders with language files: next to the program, then the user's own in ~/.config/greenshot/Languages
    /// </summary>
    public static IReadOnlyList<string> SearchPaths =>
    [
        Path.Combine(AppContext.BaseDirectory, "Languages"),
        Path.Combine(XdgDirectories.ConfigHome, "greenshot", "Languages"),
    ];

    /// <summary>
    /// Find the language files and load the language; an empty or unknown language picks the one closest to the system
    /// </summary>
    public static string Initialize(string? language, IEnumerable<string>? searchPaths = null)
    {
        lock (Lock)
        {
            _files = FindFiles(searchPaths ?? SearchPaths);
            _fallback = LoadLanguage(DefaultLanguage);
            return SetLanguageLocked(language);
        }
    }

    /// <summary>
    /// Switch the language, returns the one which is used
    /// </summary>
    public static string SetLanguage(string? language)
    {
        lock (Lock)
        {
            return SetLanguageLocked(language);
        }
    }

    public static IReadOnlyList<LanguageInfo> AvailableLanguages
    {
        get
        {
            lock (Lock)
            {
                return _files.Keys
                    .Select(ietf => new LanguageInfo(ietf, ReadDescription(ietf)))
                    .Where(l => !string.IsNullOrEmpty(l.Description))
                    .OrderBy(l => l.Description, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
        }
    }

    /// <summary>
    /// A text of the section; {0} and so on are filled with the arguments. Unknown texts show the key, so they stand out.
    /// </summary>
    public static string Get(string section, string key, params object?[] arguments)
    {
        string? text;
        lock (Lock)
        {
            text = Lookup(_current, section, key) ?? Lookup(_fallback, section, key);
        }

        if (text == null)
        {
            return key;
        }

        if (arguments.Length == 0)
        {
            return text;
        }

        try
        {
            return string.Format(Culture, text, arguments);
        }
        catch (FormatException)
        {
            return text;
        }
    }

    public static string Core(string key, params object?[] arguments) => Get("Core", key, arguments);

    public static string Editor(string key, params object?[] arguments) => Get("Editor", key, arguments);

    public static string Settings(string key, params object?[] arguments) => Get("Settings", key, arguments);

    public static string Welcome(string key, params object?[] arguments) => Get("Welcome", key, arguments);

    /// <summary>
    /// Texts of the Linux version, in greenshot-linux.*.ini
    /// </summary>
    public static string Linux(string key, params object?[] arguments) => Get("Linux", key, arguments);

    /// <summary>
    /// A WinForms/WPF style access key (&amp; or _) is removed, Avalonia menus use _ and get it from the caller
    /// </summary>
    public static string WithoutAccessKey(string text) => text.Replace("&", string.Empty, StringComparison.Ordinal);

    [GeneratedRegex(@"\s*\{\d+\}")]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"\s*\([^)]*\)\s*$")]
    private static partial Regex TrailingHintRegex();

    /// <summary>
    /// A text for a menu or a label: without access key, without unfilled {0} and without a key hint at the end
    /// ("Crop (C)" becomes "Crop", "Undo {0}" becomes "Undo"); menus show their keys themselves
    /// </summary>
    public static string Label(string text) => TrailingHintRegex().Replace(PlaceholderRegex().Replace(WithoutAccessKey(text), string.Empty), string.Empty);

    /// <summary>
    /// The IETF tag of the system language from LANGUAGE, LC_ALL, LC_MESSAGES or LANG ("de_DE.UTF-8" becomes "de-DE")
    /// </summary>
    public static string SystemLanguage
    {
        get
        {
            foreach (var variable in new[] { "LANGUAGE", "LC_ALL", "LC_MESSAGES", "LANG" })
            {
                var value = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                // LANGUAGE can be a list like "de:en"
                var first = value.Split(':')[0];
                var tag = first.Split('.', '@')[0].Replace('_', '-');
                if (tag.Length >= 2 && tag != "C" && tag != "POSIX")
                {
                    return tag;
                }
            }

            return CultureInfo.CurrentUICulture.Name is { Length: > 0 } name ? name : DefaultLanguage;
        }
    }

    /// <summary>
    /// The closest language with files: the same tag, then the same language (de-AT finds de-DE), then en-US
    /// </summary>
    internal static string FindClosest(string? wanted, IEnumerable<string> available)
    {
        var languages = available.ToList();
        if (string.IsNullOrWhiteSpace(wanted))
        {
            return DefaultLanguage;
        }

        var exact = languages.FirstOrDefault(l => l.Equals(wanted, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }

        var prefix = wanted.Split('-')[0];
        return languages
            .Where(l => l.Split('-')[0].Equals(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l.Length)
            .FirstOrDefault() ?? DefaultLanguage;
    }

    private static string SetLanguageLocked(string? language)
    {
        var wanted = string.IsNullOrWhiteSpace(language) ? SystemLanguage : language;
        Current = FindClosest(wanted, _files.Keys);
        _current = Current.Equals(DefaultLanguage, StringComparison.OrdinalIgnoreCase) ? _fallback : LoadLanguage(Current);
        Log.Info($"Language {Current} (asked for {wanted})");
        return Current;
    }

    private static string? Lookup(Dictionary<string, Dictionary<string, string>> texts, string section, string key) =>
        texts.TryGetValue(section.ToLowerInvariant(), out var values) && values.TryGetValue(key.ToLowerInvariant(), out var text) && text.Length > 0
            ? text
            : null;

    private static Dictionary<string, List<string>> FindFiles(IEnumerable<string> searchPaths)
    {
        var files = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in searchPaths.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(folder, "greenshot*.ini"))
            {
                var match = FileNameRegex().Match(Path.GetFileName(file));
                if (!match.Success)
                {
                    continue;
                }

                var ietf = match.Groups["ietf"].Value;
                if (!files.TryGetValue(ietf, out var list))
                {
                    list = new List<string>();
                    files[ietf] = list;
                }

                list.Add(file);
            }
        }

        return files;
    }

    /// <summary>
    /// All files of a language merged, later files (the user's folder) win
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> LoadLanguage(string ietf)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        if (!_files.TryGetValue(ietf, out var files))
        {
            return result;
        }

        foreach (var file in files)
        {
            try
            {
                ReadInto(result, File.ReadAllLines(file));
            }
            catch (IOException ex)
            {
                Log.Warn($"Could not read the language file {file}", ex);
            }
        }

        return result;
    }

    /// <summary>
    /// Language files are INI files; \n in a value is a line break
    /// </summary>
    internal static void ReadInto(Dictionary<string, Dictionary<string, string>> texts, IEnumerable<string> lines)
    {
        Dictionary<string, string>? section = null;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var name = line[1..^1].Trim().ToLowerInvariant();
                if (!texts.TryGetValue(name, out section))
                {
                    section = new Dictionary<string, string>();
                    texts[name] = section;
                }

                continue;
            }

            int equals = line.IndexOf('=');
            if (section == null || equals <= 0)
            {
                continue;
            }

            var key = line[..equals].Trim().ToLowerInvariant();
            var value = line[(equals + 1)..].Trim().Replace("\\n", "\n", StringComparison.Ordinal).Replace("\\t", "\t", StringComparison.Ordinal);
            section[key] = value;
        }
    }

    private static string ReadDescription(string ietf)
    {
        foreach (var file in _files[ietf])
        {
            try
            {
                var texts = new Dictionary<string, Dictionary<string, string>>();
                ReadInto(texts, File.ReadLines(file).Take(10));
                var description = Lookup(texts, "__language__", "description");
                if (description != null)
                {
                    return description;
                }
            }
            catch (IOException)
            {
                // The next file may have it
            }
        }

        return string.Empty;
    }
}
