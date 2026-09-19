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
using System.IO;
using System.Xml.Linq;
using Greenshot.Base.Core;

namespace Greenshot.Editor.Controls.Emoji
{
    /// <summary>
    /// This class processes the emoji-test.txt extract, as was generated in a build task, so it can show a list of possible emoji depending on skin tone and hairstyle.
    /// </summary>
    public static class EmojiData
    {
        private static readonly string EmojisXmlFilePath = Path.Combine(EnvironmentInfo.GetApplicationFolder(), "emojis.xml");

        public static Emojis Data { get; private set; } = new();

        public static void Load()
        {
            if (!File.Exists(EmojisXmlFilePath))
            {
                throw new NotSupportedException($"Missing {EmojisXmlFilePath}, can't load ");
            }

            var doc = XDocument.Load(EmojisXmlFilePath);
            var emojis = new Emojis();
            var gsElem = doc.Root?.Element("Gs");
            if (gsElem != null)
            {
                foreach (var gElem in gsElem.Elements("G"))
                {
                    emojis.Groups.Add(ParseGroup(gElem));
                }
            }
            Data = emojis;
        }

        private static Emojis.Group ParseGroup(XElement gElem)
        {
            var group = new Emojis.Group
            {
                Name = (string)gElem.Attribute("N")
            };

            var sgElem = gElem.Element("Sg");
            if (sgElem != null)
            {
                foreach (var subG in sgElem.Elements("G"))
                {
                    group.SubGroups.Add(ParseGroup(subG));
                }
            }

            var esElem = gElem.Element("Es");
            if (esElem != null)
            {
                foreach (var eElem in esElem.Elements("E"))
                {
                    group.Emojis.Add(ParseEmoji(eElem));
                }
            }

            return group;
        }

        private static Emojis.Emoji ParseEmoji(XElement eElem)
        {
            var emoji = new Emojis.Emoji
            {
                Text = (string)eElem.Attribute("T")
            };

            var vElem = eElem.Element("V");
            if (vElem != null)
            {
                foreach (var subE in vElem.Elements("E"))
                {
                    emoji.Variations.Add(ParseEmoji(subE));
                }
            }

            return emoji;
        }
    }
}