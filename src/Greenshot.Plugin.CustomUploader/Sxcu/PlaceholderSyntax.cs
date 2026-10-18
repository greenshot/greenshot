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
using System.Text;

namespace Greenshot.Plugin.CustomUploader.Sxcu;

/// <summary>
/// The placeholders of ShareX 13.7 and later: {name} or {name:parameter|parameter}, parameters can contain placeholders,
/// a backslash escapes { } | and \. Each placeholder is replaced by what the function returns for it.
/// </summary>
internal static class PlaceholderSyntax
{
    /// <summary>
    /// Replace the placeholders of the text.
    /// </summary>
    /// <param name="text">The text, null gives null</param>
    /// <param name="call">Returns the value of a placeholder: its name (lower case) and its parameters, already replaced</param>
    /// <param name="urlEncode">Escape the values of the outer placeholders for a url (the text around them is taken as it is)</param>
    /// <exception cref="FormatException">a placeholder isn't closed</exception>
    public static string Replace(string text, Func<string, IReadOnlyList<string>, string> call, bool urlEncode = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        int position = 0;
        return ParseText(text, ref position, call, urlEncode, false);
    }

    /// <summary>
    /// The names of the placeholders in the text, also nested ones.
    /// </summary>
    /// <exception cref="FormatException">a placeholder isn't closed</exception>
    public static IReadOnlyList<string> FindNames(string text)
    {
        var names = new List<string>();
        Replace(text, (name, parameters) =>
        {
            names.Add(name);
            return string.Empty;
        });
        return names;
    }

    private static string ParseText(string text, ref int position, Func<string, IReadOnlyList<string>, string> call, bool urlEncode, bool inParameter)
    {
        var result = new StringBuilder();
        while (position < text.Length)
        {
            char current = text[position];
            if (current == '\\' && position + 1 < text.Length && IsSyntaxCharacter(text[position + 1]))
            {
                result.Append(text[position + 1]);
                position += 2;
                continue;
            }

            if (current == '{')
            {
                position++;
                string value = ParsePlaceholder(text, ref position, call) ?? string.Empty;
                // Only the outer values: a nested value is a parameter, its function decides
                result.Append(urlEncode && !inParameter ? Uri.EscapeDataString(value) : value);
                continue;
            }

            if (inParameter && (current == '|' || current == '}'))
            {
                break;
            }

            result.Append(current);
            position++;
        }

        return result.ToString();
    }

    private static string ParsePlaceholder(string text, ref int position, Func<string, IReadOnlyList<string>, string> call)
    {
        int start = position;
        while (position < text.Length && text[position] != ':' && text[position] != '}')
        {
            position++;
        }

        if (position >= text.Length)
        {
            throw new FormatException("A placeholder isn't closed");
        }

        string name = text.Substring(start, position - start).Trim().ToLowerInvariant();
        var parameters = new List<string>();
        if (text[position] == ':')
        {
            position++;
            while (true)
            {
                parameters.Add(ParseText(text, ref position, call, false, true));
                if (position >= text.Length)
                {
                    throw new FormatException("A placeholder isn't closed");
                }

                if (text[position] == '|')
                {
                    position++;
                    continue;
                }

                break;
            }
        }

        // The closing }
        position++;
        return call(name, parameters);
    }

    private static bool IsSyntaxCharacter(char character) => character is '{' or '}' or '|' or '\\';
}
