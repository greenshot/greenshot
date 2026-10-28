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
using System.Text;
using System.Text.RegularExpressions;

namespace Greenshot.Linux.Editor.Emoji;

/// <summary>
/// An emoji of the picker, with its skin tone and hair style variations
/// </summary>
public sealed record EmojiEntry(string Text, string Name)
{
    public List<EmojiEntry> Variations { get; } = new();
}

/// <summary>
/// A group of the picker (Smileys &amp; Emotion, People &amp; Body, ...)
/// </summary>
public sealed record EmojiGroup(string Name, IReadOnlyList<EmojiEntry> Emojis)
{
    public string Icon => Emojis.FirstOrDefault()?.Text ?? string.Empty;
}

/// <summary>
/// The emoji of the picker, read from emoji-test.txt of the Unicode consortium like the EmojiDataTask of Windows does at build time:
/// only the fully-qualified emoji, the variations (skin tones) belong to their emoji, without the Component group
/// </summary>
public static partial class EmojiData
{
    private static readonly Lazy<IReadOnlyList<EmojiGroup>> LazyGroups = new(() => Load(Path.Combine(AppContext.BaseDirectory, "Emoji", "emoji-test.txt")));

    public static IReadOnlyList<EmojiGroup> Groups => LazyGroups.Value;

    [GeneratedRegex(@"^# group: (.*)")]
    private static partial Regex GroupRegex();

    [GeneratedRegex(@"^([0-9a-fA-F ]+[0-9a-fA-F]).*; *([-a-z]*) *# [^ ]* (E[0-9.]* )?(.*)")]
    private static partial Regex SequenceRegex();

    internal static IReadOnlyList<EmojiGroup> Load(string file)
    {
        if (!File.Exists(file))
        {
            Core.Log.Warn($"The emoji list {file} is missing, the emoji picker is empty");
            return [];
        }

        return Parse(File.ReadLines(file, Encoding.UTF8));
    }

    internal static IReadOnlyList<EmojiGroup> Parse(IEnumerable<string> lines)
    {
        var groups = new List<(string Name, List<EmojiEntry> Emojis)>();
        var byName = new Dictionary<string, EmojiEntry>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            var group = GroupRegex().Match(line);
            if (group.Success)
            {
                groups.Add((group.Groups[1].Value.Trim(), new List<EmojiEntry>()));
                continue;
            }

            var sequence = SequenceRegex().Match(line);
            if (!sequence.Success || groups.Count == 0)
            {
                continue;
            }

            string text = string.Concat(sequence.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(code => char.ConvertFromUtf32(int.Parse(code, NumberStyles.HexNumber, CultureInfo.InvariantCulture))));
            // Another qualification of an emoji which is there already
            if (!seen.Add(text.Replace("️", string.Empty, StringComparison.Ordinal)))
            {
                continue;
            }

            // A simple emoji with the emoji presentation selector is shown without it
            if (text.EnumerateRunes().Count() == 2)
            {
                text = text.TrimEnd('️');
            }

            string name = sequence.Groups[4].Value.Trim();
            var entry = new EmojiEntry(text, name);
            byName[name] = entry;
            int colon = name.IndexOf(':');
            if (colon > 0 && byName.TryGetValue(name[..colon], out var parent))
            {
                parent.Variations.Add(entry);
            }
            else
            {
                groups[^1].Emojis.Add(entry);
            }
        }

        return groups.Where(g => g.Name != "Component" && g.Emojis.Count > 0).Select(g => new EmojiGroup(g.Name, g.Emojis)).ToList();
    }
}
