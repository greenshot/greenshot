/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Text.RegularExpressions;
using Greenshot.Editor.Controls.Emoji;
using SixLabors.Fonts.Unicode;
using Task = Microsoft.Build.Utilities.Task;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Build.Framework;

namespace Greenshot.BuildTasks;

/// <summary>
/// A custom task to generate the emoji data we need for the picker.
/// This is based upon code from Sam Hocevar, the license is here:
///  Emoji.Wpf — Emoji support for WPF
///
///  Copyright © 2017—2021 Sam Hocevar <sam@hocevar.net>
///
///  This library is free software. It comes without any warranty, to
///  the extent permitted by applicable law. You can redistribute it
///  and/or modify it under the terms of the Do What the Fuck You Want
///  to Public License, Version 2, as published by the WTFPL Task Force.
///  See http://www.wtfpl.net/ for more details.
/// </summary>
public class EmojiDataTask : Task
{
    private static readonly Regex MatchGroup = new(@"^# group: (.*)", RegexOptions.Compiled);
    private static readonly Regex MatchSubgroup = new(@"^# subgroup: (.*)", RegexOptions.Compiled);
    private static readonly Regex MatchSequence = new(@"^([0-9a-fA-F ]+[0-9a-fA-F]).*; *([-a-z]*) *# [^ ]* (E[0-9.]* )?(.*)", RegexOptions.Compiled);

    private static string ToColonSyntax(string s) => Regex.Replace(s.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-");

    /// <summary>
    /// The name of the output file
    /// </summary>
    [Required]
    public string OutputFilename { get; set; }

    //The name of the namespace where the class is going to be generated
    [Required]
    public string EmojiTestTxtFile { get; set; }

    public override bool Execute()
    {
        var data = ParseEmojiList(EmojiTestTxtFile);
        if (!data.Groups.Any())
        {
            return false;
        }
        Log.LogMessage($"Creating file {OutputFilename}");
        var x = new XmlSerializer(typeof(Emojis));
        using var writer = new XmlTextWriter(OutputFilename, Encoding.UTF8);
        x.Serialize(writer, data);
        
        return true;
    }


    private static Emojis ParseEmojiList(string emojiTestTxtFile)
    {
        var result = new Emojis();
        var lookupByName = new Dictionary<string, Emojis.Emoji>();
        var qualifiedLut = new Dictionary<string, string>();
        Emojis.Group currentGroup = null;
        Emojis.Group currentSubgroup = null;

        foreach (var line in ReadLines(emojiTestTxtFile))
        {
            var m = MatchGroup.Match(line);
            if (m.Success)
            {
                currentGroup = new Emojis.Group { Name = m.Groups[1].ToString() };
                result.Groups.Add(currentGroup);
                continue;
            }

            m = MatchSubgroup.Match(line);
            if (m.Success)
            {
                currentSubgroup = new Emojis.Group { Name = m.Groups[1].ToString() };
                currentGroup?.SubGroups?.Add(currentSubgroup);
                continue;
            }

            m = MatchSequence.Match(line);
            if (!m.Success)
            {
                continue;
            }
            string sequence = m.Groups[1].ToString();
            string name = m.Groups[4].ToString();

            string text = string.Join("", sequence.Split(' ').Select(c => char.ConvertFromUtf32(Convert.ToInt32(c, 16))));

            // If there is already a differently-qualified version of this character, skip it.
            // FIXME: this only works well if fully-qualified appears first in the list.
            var unqualified = text.Replace("\ufe0f", "");
            if (qualifiedLut.ContainsKey(unqualified))
            {
                continue;
            }

            // Fix simple fully-qualified emojis
            if (CodePoint.GetCodePointCount(text.AsSpan()) == 2)
            {
                text = text.TrimEnd('\ufe0f');
            }

            qualifiedLut[unqualified] = text;

            var emoji = new Emojis.Emoji { Text = text };

            lookupByName[ToColonSyntax(name)] = emoji;

            // Get the left part of the name and check whether we’re a variation of an existing
            // emoji. If so, append to that emoji. Otherwise, add to current subgroup.
            // FIXME: does not work properly because variations can appear before the generic emoji
            if (name.Contains(":") && lookupByName.TryGetValue(ToColonSyntax(name.Split(':')[0]), out var parentEmoji))
            {
                parentEmoji.Variations.Add(emoji);
            }
            else
            {
                currentSubgroup?.Emojis?.Add(emoji);
            }
        }

        // Remove the Component group. Not sure we want to have the skin tones in the picker.
        result.Groups.RemoveAll(g => g.Name == "Component");
        return result;
    }

    /// <summary>
    /// This reads the specified file into lines
    /// </summary>
    /// <param name="file">string</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    private static IEnumerable<string> ReadLines(string file)
    {
        if (!File.Exists(file))
        {
            throw new FileNotFoundException($"Can't find {file}");
        }

        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}