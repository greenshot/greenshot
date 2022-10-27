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
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Greenshot.Editor.Controls.Emoji
{
    /// <summary>
    /// This class processes the emoji-test.txt extract, as was generated in a build task, so it can show a list of possible emoji depending on skin tone and hairstyle.
    /// </summary>
    public static class EmojiData
    {
        private const string EmojisXmlFilePath = "emojis.xml";

        public static Emojis Data { get; private set; } = new();

        public static void Load()
        {
            var x = new XmlSerializer(typeof(Emojis));

            if (File.Exists(EmojisXmlFilePath))
            {
                Data = (Emojis)x.Deserialize(new XmlTextReader(EmojisXmlFilePath));
            }
            else
            {
                throw new NotSupportedException($"Missing {EmojisXmlFilePath}, can't load ");
            }
        }
    }
}