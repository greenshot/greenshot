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
using System.Security.Cryptography;
using System.Text;

namespace Greenshot.Plugin.CustomUploader;

/// <summary>
/// Encrypts the uploaders and the uploads for the Windows user (DPAPI): they hold keys, webhook urls and deletion links.
/// Another user, or the same greenshot.ini on another computer, can't read them, the user imports the uploader again.
/// </summary>
internal static class UserProtection
{
    // Only this plugin decrypts what it encrypted, not every program of the user which calls DPAPI without entropy
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Greenshot.Plugin.CustomUploader.v1");

    public static string Protect(string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        byte[] cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(text), Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(cipher);
    }

    /// <summary>
    /// The text, null when it can't be decrypted (other user, other computer, changed by hand)
    /// </summary>
    public static string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
        {
            return null;
        }

        try
        {
            byte[] plain = ProtectedData.Unprotect(Convert.FromBase64String(protectedText), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return null;
        }
    }
}
