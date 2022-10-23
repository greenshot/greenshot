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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using SixLabors.Fonts.Unicode;

namespace Greenshot.Editor.Controls
{
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

        private static List<string> SkinToneComponents = new List<string>
        {
            "🏻", // light skin tone
            "🏼", // medium-light skin tone
            "🏽", // medium skin tone
            "🏾", // medium-dark skin tone
            "🏿", // dark skin tone
        };

        private static List<string> HairStyleComponents = new List<string>
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
            var lookup_by_name = new Dictionary<string, Emojis.Emoji>();
            var match_group = new Regex(@"^# group: (.*)");
            var match_subgroup = new Regex(@"^# subgroup: (.*)");
            var match_sequence = new Regex(@"^([0-9a-fA-F ]+[0-9a-fA-F]).*; *([-a-z]*) *# [^ ]* (E[0-9.]* )?(.*)");
            var match_skin_tone = new Regex($"({string.Join("|", SkinToneComponents)})");
            var match_hair_style = new Regex($"({string.Join("|", HairStyleComponents)})");

            var adult = "(👨|👩)(🏻|🏼|🏽|🏾|🏿)?";
            var child = "(👦|👧|👶)(🏻|🏼|🏽|🏾|🏿)?";
            var match_family = new Regex($"{adult}(\u200d{adult})*(\u200d{child})+");

            var qualified_lut = new Dictionary<string, string>();
            var alltext = new List<string>();

            Emojis.Group current_group = null;
            Emojis.Group current_subgroup = null;

            foreach (var line in EmojiDescriptionLines())
            {
                var m = match_group.Match(line);
                if (m.Success)
                {
                    current_group = new Emojis.Group { Name = m.Groups[1].ToString() };
                    Data.Groups.Add(current_group);
                    continue;
                }

                m = match_subgroup.Match(line);
                if (m.Success)
                {
                    current_subgroup = new Emojis.Group { Name = m.Groups[1].ToString() };
                    current_group.SubGroups.Add(current_subgroup);
                    continue;
                }

                m = match_sequence.Match(line);
                if (m.Success)
                {
                    string sequence = m.Groups[1].ToString();
                    string name = m.Groups[4].ToString();

                    string text = string.Join("", from n in sequence.Split(' ')
                                                  select char.ConvertFromUtf32(Convert.ToInt32(n, 16)));
                    bool has_modifier = false;

                    if (match_family.Match(text).Success)
                    {
                        // If this is a family emoji, no need to add it to our big matching
                        // regex, since the match_family regex is already included.
                    }
                    else
                    {
                        // Construct a regex to replace e.g. "🏻" with "(🏻|🏼|🏽|🏾|🏿)" in a big
                        // regex so that we can match all variations of this Emoji even if they are
                        // not in the standard.
                        bool has_nonfirst_modifier = false;
                        var regex_text = match_skin_tone.Replace(
                            match_hair_style.Replace(text, (x) =>
                            {
                                has_modifier = true;
                                has_nonfirst_modifier |= x.Value != HairStyleComponents[0];
                                return match_hair_style.ToString();
                            }), (x) =>
                            {
                                has_modifier = true;
                                has_nonfirst_modifier |= x.Value != SkinToneComponents[0];
                                return match_skin_tone.ToString();
                            });

                        if (!has_nonfirst_modifier)
                            alltext.Add(has_modifier ? regex_text : text);
                    }

                    // If there is already a differently-qualified version of this character, skip it.
                    // FIXME: this only works well if fully-qualified appears first in the list.
                    var unqualified = text.Replace("\ufe0f", "");
                    if (qualified_lut.ContainsKey(unqualified))
                        continue;

                    // Fix simple fully-qualified emojis
                    if (CodePoint.GetCodePointCount(text.AsSpan()) == 2)
                    {
                        text = text.TrimEnd('\ufe0f');
                    }

                    qualified_lut[unqualified] = text;

                    var emoji = new Emojis.Emoji { Name = name, Text = text, };

                    lookup_by_name[ToColonSyntax(name)] = emoji;

                    // Get the left part of the name and check whether we’re a variation of an existing
                    // emoji. If so, append to that emoji. Otherwise, add to current subgroup.
                    // FIXME: does not work properly because variations can appear before the generic emoji
                    if (name.Contains(":") && lookup_by_name.TryGetValue(ToColonSyntax(name.Split(':')[0]), out var parent_emoji))
                    {
                        parent_emoji.Variations.Add(emoji);
                    }
                    else
                        current_subgroup.Emojis.Add(emoji);
                }
            }

            // Remove the Component group. Not sure we want to have the skin tones in the picker.
            Data.Groups.RemoveAll(g => g.Name == "Component");
        }

        private static IEnumerable<string> EmojiDescriptionLines()
        {
            using var fileStream = new FileStream(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), @"emoji-test.txt"), FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(fileStream);
            return streamReader.ReadToEnd().Split('\r', '\n');
        }
    }

    public class Emojis
    {
        [XmlElement(ElementName = "Group")] 
        public List<Group> Groups { get; set; } = new();
            
        public class Group
        {
            [XmlAttribute]
            public string Name { get; set; }
            
            [XmlElement(ElementName = "Group")]
            public List<Group> SubGroups { get; set; } = new();
            
            [XmlElement(ElementName = "Emoji")]
            public List<Emoji> Emojis { get; set; } = new();

            public IEnumerable<IEnumerable<Emoji>> EmojiChunkList => new ChunkHelper<Emoji>(EmojiList, 8);

            public string Icon => SubGroups.FirstOrDefault()?.Emojis.FirstOrDefault()?.Text;

            public IEnumerable<Emoji> EmojiList
                => from s in SubGroups
                   from e in s.Emojis
                   select e;
        }
            
        public class Emoji
        {
            [XmlAttribute]
            public string Name { get; set; }
            
            [XmlAttribute]
            public string Text { get; set; }
            
            [XmlArray]
            public List<Emoji> Variations { get; set; }

            public bool HasVariations => Variations.Count > 0;

            public IEnumerable<Emoji> AllVariations => HasVariations ? new[] { this }.Union(Variations) : Array.Empty<Emoji>();
        }
    }

    sealed class ChunkHelper<T> : IEnumerable<IEnumerable<T>>
    {
        public ChunkHelper(IEnumerable<T> elements, int size)
        {
            m_elements = elements;
            m_size = size;
        }

        public IEnumerator<IEnumerable<T>> GetEnumerator()
        {
            using var enumerator = m_elements.GetEnumerator();
            m_has_more = enumerator.MoveNext();
            while (m_has_more)
                yield return GetNextBatch(enumerator).ToList();
        }

        private IEnumerable<T> GetNextBatch(IEnumerator<T> enumerator)
        {
            for (int i = 0; i < m_size; ++i)
            {
                yield return enumerator.Current;
                m_has_more = enumerator.MoveNext();
                if (!m_has_more)
                    yield break;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private readonly IEnumerable<T> m_elements;
        private readonly int m_size;
        private bool m_has_more;
    }
}