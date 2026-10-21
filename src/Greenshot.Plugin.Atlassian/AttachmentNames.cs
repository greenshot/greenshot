/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Greenshot.Plugin.Atlassian;

/// <summary>
/// File names of attachments
/// </summary>
public static class AttachmentNames
{
    private static readonly Regex Numbered = new Regex(@"^(.*) \((\d{1,6})\)$", RegexOptions.CultureInvariant);

    /// <summary>
    /// The next numbered name: "name (2).png" for "name.png", "name (3).png" for "name (2).png"
    /// </summary>
    public static string Next(string filename)
    {
        string extension = Path.GetExtension(filename);
        string name = filename.Substring(0, filename.Length - extension.Length);
        var numbered = Numbered.Match(name);
        return numbered.Success
            ? $"{numbered.Groups[1].Value} ({int.Parse(numbered.Groups[2].Value, CultureInfo.InvariantCulture) + 1}){extension}"
            : $"{name} (2){extension}";
    }
}
