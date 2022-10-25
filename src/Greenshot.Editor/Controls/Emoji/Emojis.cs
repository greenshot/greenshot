/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using System.Xml.Serialization;

namespace Greenshot.Editor.Controls.Emoji;

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

        public IEnumerable<Emoji> EmojiList => SubGroups.SelectMany(s => s.Emojis);
    }

    public class Emoji
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Text { get; set; }

        [XmlArray]
        public List<Emoji> Variations { get; set; } = new();

        public bool HasVariations => Variations.Count > 0;

        public IEnumerable<Emoji> AllVariations => HasVariations ? new[] { this }.Union(Variations) : Array.Empty<Emoji>();
    }
}