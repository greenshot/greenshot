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

internal static class DBusConnectionExtensions
{
    /// <summary>
    /// Write an a{sv} of options. Values can be string, bool, byte, uint or int.
    /// The writer is a ref struct and must be passed by reference, see DesktopPortal.CreateCall.
    /// </summary>
    public static void WriteOptions(this ref MessageWriter writer, IEnumerable<KeyValuePair<string, object>> options)
    {
        var dictionary = writer.WriteDictionaryStart();
        foreach (var (key, value) in options)
        {
            writer.WriteDictionaryEntryStart();
            writer.WriteString(key);
            switch (value)
            {
                case string text:
                    writer.WriteVariantString(text);
                    break;
                case bool flag:
                    writer.WriteVariantBool(flag);
                    break;
                case byte number:
                    writer.WriteVariantByte(number);
                    break;
                case uint number:
                    writer.WriteVariantUInt32(number);
                    break;
                case int number:
                    writer.WriteVariantInt32(number);
                    break;
                default:
                    throw new ArgumentException($"The option {key} has the unsupported type {value.GetType()}", nameof(options));
            }
        }

        writer.WriteDictionaryEnd(dictionary);
    }

    /// <summary>
    /// Watch for signals matching the rule; onValue gets every signal, onError a failure of the connection or the reader.
    /// Dispose the result to stop watching.
    /// </summary>
    public static async Task<IDisposable> WatchAsync<T>(this DBusConnection connection, MatchRule rule, MessageValueReader<T> reader,
        Action<T> onValue, Action<Exception>? onError = null)
    {
        return await connection.AddMatchAsync(rule, reader, (Notification<T> notification) =>
        {
            if (notification.HasValue)
            {
                onValue(notification.Value);
            }
            else if (notification.Exception != null)
            {
                onError?.Invoke(notification.Exception);
            }
        }, emitOnCapturedContext: false, ObserverFlags.None, null).ConfigureAwait(false);
    }
}
