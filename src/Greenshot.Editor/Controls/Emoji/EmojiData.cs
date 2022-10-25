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
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
#if DEBUG
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using SixLabors.Fonts.Unicode;
#endif

namespace Greenshot.Editor.Controls.Emoji
{
    /// <summary>
    /// This class processes the emoji-test.txt to generate a list of possible emoji depending ony skin tone and hairstyle.
    /// </summary>
    public static class EmojiData
    {
        private const string EmojisXmlFilePath = "emojis.xml";
        private const string EmojisTestFile = @"emoji-test.txt.gz";
#if DEBUG
        private const string Adult = "(👨|👩)(🏻|🏼|🏽|🏾|🏿)?";
        private const string Child = "(👦|👧|👶)(🏻|🏼|🏽|🏾|🏿)?";
        private static readonly Regex MatchFamily = new($"{Adult}(\u200d{Adult})*(\u200d{Child})+");

        private static readonly Regex MatchGroup = new(@"^# group: (.*)", RegexOptions.Compiled);
        private static readonly Regex MatchSubgroup = new(@"^# subgroup: (.*)", RegexOptions.Compiled);
        private static readonly Regex MatchSequence = new(@"^([0-9a-fA-F ]+[0-9a-fA-F]).*; *([-a-z]*) *# [^ ]* (E[0-9.]* )?(.*)", RegexOptions.Compiled);
        private static readonly List<string> SkinToneComponents = new()
        {
            "🏻", // light skin tone
            "🏼", // medium-light skin tone
            "🏽", // medium skin tone
            "🏾", // medium-dark skin tone
            "🏿", // dark skin tone
        };

        private static readonly List<string> HairStyleComponents = new()
        {
            "🦰", // red hair
            "🦱", // curly hair
            "🦳", // white hair
            "🦲", // bald
        };
        
        private static readonly Regex MatchSkinTone = new($"({string.Join("|", SkinToneComponents)})", RegexOptions.Compiled);
        private static readonly Regex MatchHairStyle = new($"({string.Join("|", HairStyleComponents)})", RegexOptions.Compiled);

#endif 

        public static Emojis Data { get; private set; } = new();

        public static void Load()
        {
            var x = new XmlSerializer(typeof(Emojis));

            if (File.Exists(EmojisXmlFilePath))
            {
                Data = (Emojis)x.Deserialize(new XmlTextReader(EmojisXmlFilePath));
            }
#if RELEASE
            else
            {
                throw new NotSupportedException($"Missing {EmojisXmlFilePath}, can't load ");
            }
#elif DEBUG
            else
            {
                // To be removed
                ParseEmojiList();
                x.Serialize(new XmlTextWriter(EmojisXmlFilePath, Encoding.UTF8), Data);
            }
#endif
        }


#if DEBUG
       private static string ToColonSyntax(string s) => Regex.Replace(s.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-");

        private static void ParseEmojiList()
        {
            var lookupByName = new Dictionary<string, Emojis.Emoji>();

            var qualifiedLut = new Dictionary<string, string>();
            var allText = new List<string>();

            Emojis.Group currentGroup = null;
            Emojis.Group currentSubgroup = null;

            foreach (var line in EmojiDescriptionLines())
            {
                var m = MatchGroup.Match(line);
                if (m.Success)
                {
                    currentGroup = new Emojis.Group { Name = m.Groups[1].ToString() };
                    Data.Groups.Add(currentGroup);
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
                bool hasModifier = false;

                // If this is a family emoji, no need to add it to our big matching
                // regex, since the match_family regex is already included.
                if (!MatchFamily.Match(text).Success)
                {
                    // Construct a regex to replace e.g. "🏻" with "(🏻|🏼|🏽|🏾|🏿)" in a big
                    // regex so that we can match all variations of this Emoji even if they are
                    // not in the standard.
                    bool hasNonfirstModifier = false;
                    var regexText = MatchSkinTone.Replace(
                        MatchHairStyle.Replace(text, (x) =>
                        {
                            hasModifier = true;
                            hasNonfirstModifier |= x.Value != HairStyleComponents[0];
                            return MatchHairStyle.ToString();
                        }), (x) =>
                        {
                            hasModifier = true;
                            hasNonfirstModifier |= x.Value != SkinToneComponents[0];
                            return MatchSkinTone.ToString();
                        });

                    if (!hasNonfirstModifier)
                    {
                        allText.Add(hasModifier ? regexText : text);
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

                var emoji = new Emojis.Emoji { Name = name, Text = text};

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
            Data.Groups.RemoveAll(g => g.Name == "Component");
        }

        private static IEnumerable<string> EmojiDescriptionLines()
        {
            var exeDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            var emojiTestFile = Path.Combine(exeDirectory, EmojisTestFile);
            if (!File.Exists(emojiTestFile))
            {
                throw new FileNotFoundException($"Can't find {emojiTestFile}, bad installation?");
            }
            using var fileStream = new FileStream(emojiTestFile, FileMode.Open, FileAccess.Read);
            using var gzStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var streamReader = new StreamReader(fileStream);
            return streamReader.ReadToEnd().Split('\r', '\n');
        }
#endif
    }
}