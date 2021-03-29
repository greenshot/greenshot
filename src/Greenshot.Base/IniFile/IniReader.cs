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

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Greenshot.Base.IniFile
{
    /// <summary>
    /// The IniReader does exactly what it says, it reads the .ini file
    /// </summary>
    public static class IniReader
    {
        private const char SectionStartToken = '[';
        private const char SectionEndToken = ']';
        private const char CommentToken = ';';

        private static readonly char[] Assignment =
        {
            '='
        };

        /// <summary>
        /// Read an ini file to a Dictionary, each key is a section and the value is a Dictionary with name and values.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static IDictionary<string, IDictionary<string, string>> Read(string path, Encoding encoding)
        {
            var ini = new Dictionary<string, IDictionary<string, string>>();
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024))
            {
                using var streamReader = new StreamReader(fileStream, encoding);
                IDictionary<string, string> nameValues = new Dictionary<string, string>();
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
                    if (line == null)
                    {
                        continue;
                    }

                    string cleanLine = line.Trim();
                    if (cleanLine.Length == 0 || cleanLine[0] == CommentToken)
                    {
                        continue;
                    }

                    if (cleanLine[0] == SectionStartToken)
                    {
                        var sectionEndIndex = line.IndexOf(SectionEndToken, 1);
                        if (sectionEndIndex < 0)
                        {
                            continue;
                        }

                        string section = line.Substring(1, sectionEndIndex - 1).Trim();
                        if (!ini.TryGetValue(section, out nameValues))
                        {
                            nameValues = new Dictionary<string, string>();
                            ini.Add(section, nameValues);
                        }
                    }
                    else
                    {
                        string[] keyvalueSplitter = line.Split(Assignment, 2);
                        string name = keyvalueSplitter[0];
                        string inivalue = keyvalueSplitter.Length > 1 ? keyvalueSplitter[1] : null;
                        if (nameValues.ContainsKey(name))
                        {
                            nameValues[name] = inivalue;
                        }
                        else
                        {
                            nameValues.Add(name, inivalue);
                        }
                    }
                }
            }

            return ini;
        }
    }
}