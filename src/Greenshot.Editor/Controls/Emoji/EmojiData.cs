//
//  Emoji.Wpf — Emoji support for WPF
//
//  Copyright © 2017—2021 Sam Hocevar <sam@hocevar.net>
//
//  This library is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using SixLabors.Fonts.Unicode;

namespace Greenshot.Editor.Controls.Emoji
{
    /// <summary>
    /// This class processes the emoji-test.txt to generate a list of possible emoji depending ony skin tone and hairstyle.
    /// </summary>
    public static class EmojiData
    {
        private const string FilePath = "emojis.xml";

        public static Emojis Data { get; private set; } = new Emojis();

        public static void Load()
        {
            var x = new XmlSerializer(typeof(Emojis));

            if (File.Exists(FilePath))
            {
                Data = (Emojis)x.Deserialize(new XmlTextReader(FilePath));
            }
            else
            {
                // To be removed
                ParseEmojiList();
                x.Serialize(new XmlTextWriter(FilePath, Encoding.UTF8), Data);
            }
        }

        private static readonly List<string> SkinToneComponents = new List<string>
        {
            "🏻", // light skin tone
            "🏼", // medium-light skin tone
            "🏽", // medium skin tone
            "🏾", // medium-dark skin tone
            "🏿", // dark skin tone
        };

        private static readonly List<string> HairStyleComponents = new List<string>
        {
            "🦰", // red hair
            "🦱", // curly hair
            "🦳", // white hair
            "🦲", // bald
        };

        private static string ToColonSyntax(string s)
            => Regex.Replace(s.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-");

        private static void ParseEmojiList()
        {
            var lookupByName = new Dictionary<string, Emojis.Emoji>();
            var matchGroup = new Regex(@"^# group: (.*)");
            var matchSubgroup = new Regex(@"^# subgroup: (.*)");
            var matchSequence = new Regex(@"^([0-9a-fA-F ]+[0-9a-fA-F]).*; *([-a-z]*) *# [^ ]* (E[0-9.]* )?(.*)");
            var matchSkinTone = new Regex($"({string.Join("|", SkinToneComponents)})");
            var matchHairStyle = new Regex($"({string.Join("|", HairStyleComponents)})");

            var adult = "(👨|👩)(🏻|🏼|🏽|🏾|🏿)?";
            var child = "(👦|👧|👶)(🏻|🏼|🏽|🏾|🏿)?";
            var matchFamily = new Regex($"{adult}(\u200d{adult})*(\u200d{child})+");

            var qualifiedLut = new Dictionary<string, string>();
            var allText = new List<string>();

            Emojis.Group currentGroup = null;
            Emojis.Group currentSubgroup = null;

            foreach (var line in EmojiDescriptionLines())
            {
                var m = matchGroup.Match(line);
                if (m.Success)
                {
                    currentGroup = new Emojis.Group { Name = m.Groups[1].ToString() };
                    Data.Groups.Add(currentGroup);
                    continue;
                }

                m = matchSubgroup.Match(line);
                if (m.Success)
                {
                    currentSubgroup = new Emojis.Group { Name = m.Groups[1].ToString() };
                    currentGroup.SubGroups.Add(currentSubgroup);
                    continue;
                }

                m = matchSequence.Match(line);
                if (m.Success)
                {
                    string sequence = m.Groups[1].ToString();
                    string name = m.Groups[4].ToString();

                    string text = string.Join("", from n in sequence.Split(' ')
                                                  select char.ConvertFromUtf32(Convert.ToInt32(n, 16)));
                    bool has_modifier = false;

                    if (matchFamily.Match(text).Success)
                    {
                        // If this is a family emoji, no need to add it to our big matching
                        // regex, since the match_family regex is already included.
                    }
                    else
                    {
                        // Construct a regex to replace e.g. "🏻" with "(🏻|🏼|🏽|🏾|🏿)" in a big
                        // regex so that we can match all variations of this Emoji even if they are
                        // not in the standard.
                        bool hasNonfirstModifier = false;
                        var regexText = matchSkinTone.Replace(
                            matchHairStyle.Replace(text, (x) =>
                            {
                                has_modifier = true;
                                hasNonfirstModifier |= x.Value != HairStyleComponents[0];
                                return matchHairStyle.ToString();
                            }), (x) =>
                            {
                                has_modifier = true;
                                hasNonfirstModifier |= x.Value != SkinToneComponents[0];
                                return matchSkinTone.ToString();
                            });

                        if (!hasNonfirstModifier)
                        {
                            allText.Add(has_modifier ? regexText : text);
                        }
                    }

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

                    var emoji = new Emojis.Emoji { Name = name, Text = text, };

                    lookupByName[ToColonSyntax(name)] = emoji;

                    // Get the left part of the name and check whether we’re a variation of an existing
                    // emoji. If so, append to that emoji. Otherwise, add to current subgroup.
                    // FIXME: does not work properly because variations can appear before the generic emoji
                    if (name.Contains(":") && lookupByName.TryGetValue(ToColonSyntax(name.Split(':')[0]), out var parent_emoji))
                    {
                        parent_emoji.Variations.Add(emoji);
                    }
                    else
                    {
                        currentSubgroup.Emojis.Add(emoji);
                    }
                }
            }

            // Remove the Component group. Not sure we want to have the skin tones in the picker.
            Data.Groups.RemoveAll(g => g.Name == "Component");
        }

        private static IEnumerable<string> EmojiDescriptionLines()
        {
            var exeDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            var emojiTestFile = Path.Combine(exeDirectory, @"emoji-test.txt");
            if (!File.Exists(emojiTestFile))
            {
                throw new FileNotFoundException($"Can't find {emojiTestFile}, bad installation?");
            }
            using var fileStream = new FileStream(emojiTestFile, FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(fileStream);
            return streamReader.ReadToEnd().Split('\r', '\n');
        }
    }
}