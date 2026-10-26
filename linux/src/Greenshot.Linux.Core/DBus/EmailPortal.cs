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

using Tmds.DBus.Protocol;

namespace Greenshot.Linux.Core.DBus;

/// <summary>
/// A new e-mail in the mail program of the user with the capture attached (org.freedesktop.portal.Email)
/// </summary>
public sealed class EmailPortal(DesktopPortal portal)
{
    private const string EmailInterface = "org.freedesktop.portal.Email";

    public static async Task<bool> IsAvailableAsync(DesktopPortal portal) =>
        await portal.GetVersionAsync(EmailInterface).ConfigureAwait(false) != null;

    public async Task<bool> ComposeAsync(string subject, string attachmentPath, CancellationToken cancellationToken = default)
    {
        using var handle = File.OpenHandle(attachmentPath, FileMode.Open, FileAccess.Read);
        var response = await portal.RequestAsync(EmailInterface, "ComposeEmail", "sa{sv}", (ref MessageWriter writer, string token) =>
        {
            writer.WriteString(string.Empty);
            var options = writer.WriteDictionaryStart();
            writer.WriteDictionaryEntryStart();
            writer.WriteString("handle_token");
            writer.WriteVariantString(token);
            writer.WriteDictionaryEntryStart();
            writer.WriteString("subject");
            writer.WriteVariantString(subject);
            writer.WriteDictionaryEntryStart();
            writer.WriteString("attachment_fds");
            writer.WriteSignature("ah");
            writer.WriteArray(new System.Runtime.InteropServices.SafeHandle[] { handle });
            writer.WriteDictionaryEnd(options);
        }, cancellationToken).ConfigureAwait(false);
        return response.IsSuccess;
    }
}
