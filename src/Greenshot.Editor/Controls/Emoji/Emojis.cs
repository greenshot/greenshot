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

[XmlRoot("Es")]
public class Emojis
{
    [XmlArray(ElementName = "Gs")]
    public List<Group> Groups { get; set; } = new();

    [XmlType("G")]
    public class Group
    {
        [XmlAttribute(AttributeName= "N")]
        public string Name { get; set; }

        [XmlArray(ElementName = "Sg")]
        public List<Group> SubGroups { get; set; } = new();

        [XmlArray(ElementName = "Es")]
        public List<Emoji> Emojis { get; set; } = new();

        public IEnumerable<IEnumerable<Emoji>> EmojiChunkList => new ChunkHelper<Emoji>(EmojiList, 8);

        public string Icon => SubGroups.FirstOrDefault()?.Emojis.FirstOrDefault()?.Text;

        public IEnumerable<Emoji> EmojiList => SubGroups.SelectMany(s => s.Emojis);
    }

    [XmlType("E")]
    public class Emoji
    {
        [XmlAttribute(AttributeName = "T")]
        public string Text { get; set; }

        [XmlArray(ElementName = "V")]
        public List<Emoji> Variations { get; set; } = new();
        
        /// <summary>
        /// Xml trick so that the Xml serializer does not output the 'Variations' element when the emoji has no variation
        /// (see https://learn.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlserializer#controlling-generated-xml)
        /// </summary>
        [XmlIgnore]
        public bool VariationsSpecified => HasVariations;

        public bool HasVariations => Variations.Count > 0;

        public IEnumerable<Emoji> AllVariations => HasVariations ? new[] { this }.Union(Variations) : Array.Empty<Emoji>();
    }
}