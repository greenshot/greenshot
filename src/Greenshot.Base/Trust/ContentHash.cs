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

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Greenshot.Base.Trust;

/// <summary>
/// The SHA-256 of a content (lowercase hex): an approval belongs to exactly this content
/// </summary>
public static class ContentHash
{
    /// <summary>
    /// Of the bytes which were read once, shown and parsed, null for null
    /// </summary>
    public static string Sha256(byte[] content)
    {
        if (content == null) return null;
        using var sha = SHA256.Create();
        return ToHex(sha.ComputeHash(content));
    }

    /// <summary>
    /// Of the file, null when there is none
    /// </summary>
    public static string Sha256OfFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return ToHex(sha.ComputeHash(stream));
    }

    private static string ToHex(byte[] hash)
    {
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
