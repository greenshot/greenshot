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
/// Writes the arguments of a portal call; handleToken goes into the options as handle_token
/// </summary>
public delegate void ArgumentWriter(ref MessageWriter writer, string handleToken);

/// <summary>
/// The answer of a portal request, see org.freedesktop.portal.Request
/// </summary>
public sealed record PortalResponse(uint Code, Dictionary<string, VariantValue> Results)
{
    public bool IsSuccess => Code == 0;

    public bool IsCancelled => Code == 1;
}

/// <summary>
/// Calls to the xdg-desktop-portal (https://flatpak.github.io/xdg-desktop-portal/docs/) on the session bus.
/// Most portal methods return a request handle at once and send the result later with the Response signal of that request,
/// <see cref="RequestAsync"/> hides this.
/// </summary>
public sealed class DesktopPortal : IDisposable
{
    public const string BusName = "org.freedesktop.portal.Desktop";
    public const string ObjectPath = "/org/freedesktop/portal/desktop";
    private const string RequestInterface = "org.freedesktop.portal.Request";

    private DesktopPortal(DBusConnection connection)
    {
        Connection = connection;
    }

    public DBusConnection Connection { get; }

    public static async Task<DesktopPortal> ConnectAsync()
    {
        var address = DBusAddress.Session ?? throw new InvalidOperationException("There is no D-Bus session bus (DBUS_SESSION_BUS_ADDRESS)");
        var connection = new DBusConnection(address);
        try
        {
            await connection.ConnectAsync().ConfigureAwait(false);
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return new DesktopPortal(connection);
    }

    /// <summary>
    /// The version of a portal interface, null when the portal doesn't offer it
    /// </summary>
    public async Task<uint?> GetVersionAsync(string portalInterface)
    {
        MessageBuffer message;
        using (var writer = Connection.GetMessageWriter())
        {
            writer.WriteMethodCallHeader(destination: BusName, path: ObjectPath, @interface: "org.freedesktop.DBus.Properties",
                member: "Get", signature: "ss");
            writer.WriteString(portalInterface);
            writer.WriteString("version");
            message = writer.CreateMessage();
        }

        try
        {
            return await Connection.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadVariantValue().GetUInt32(), null)
                .ConfigureAwait(false);
        }
        catch (DBusExceptionBase ex)
        {
            Log.Debug($"{portalInterface} is not available: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// The handle_token for the options, unique for this connection
    /// </summary>
    public static string NewToken() => "greenshot" + Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// Call a portal method which returns a request handle and wait for its Response.
    /// The first argument after the header has to be written by <paramref name="writeArguments"/>,
    /// which gets the handle_token to put in the options.
    /// </summary>
    public async Task<PortalResponse> RequestAsync(string portalInterface, string member, string signature,
        ArgumentWriter writeArguments, CancellationToken cancellationToken = default)
    {
        var token = NewToken();
        var expectedPath = RequestPath(token);
        var response = new TaskCompletionSource<PortalResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Subscribe before calling, the response can come before the call returns
        using var subscription = await SubscribeResponseAsync(expectedPath, response).ConfigureAwait(false);

        var message = CreateCall(portalInterface, member, signature, writeArguments, token);
        var handle = await Connection.CallMethodAsync(message, static (m, _) => m.GetBodyReader().ReadObjectPathAsString(), null)
            .ConfigureAwait(false);

        // Portals before version 0.9 didn't use the handle_token for the path
        using var fallbackSubscription = handle != expectedPath
            ? await SubscribeResponseAsync(handle, response).ConfigureAwait(false)
            : null;

        await using var registration = cancellationToken.Register(() =>
        {
            response.TrySetCanceled(cancellationToken);
            _ = CloseRequestAsync(handle);
        });
        return await response.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// MessageWriter is a ref struct: it has to be passed by reference, a copy writes the bytes
    /// but the original doesn't know about them and creates a message with a broken body
    /// </summary>
    private MessageBuffer CreateCall(string portalInterface, string member, string signature, ArgumentWriter writeArguments, string token)
    {
        var writer = Connection.GetMessageWriter();
        try
        {
            writer.WriteMethodCallHeader(destination: BusName, path: ObjectPath, @interface: portalInterface, member: member, signature: signature);
            writeArguments(ref writer, token);
            return writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
    }

    private string RequestPath(string token)
    {
        var sender = (Connection.UniqueName ?? string.Empty).TrimStart(':').Replace('.', '_');
        return $"{ObjectPath}/request/{sender}/{token}";
    }

    private Task<IDisposable> SubscribeResponseAsync(string path, TaskCompletionSource<PortalResponse> response)
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Interface = RequestInterface,
            Member = "Response",
            Path = path,
        };
        return Connection.WatchAsync(rule,
            static (m, _) =>
            {
                var reader = m.GetBodyReader();
                var code = reader.ReadUInt32();
                var results = reader.ReadDictionaryOfStringToVariantValue();
                return new PortalResponse(code, results);
            },
            value => response.TrySetResult(value),
            ex => response.TrySetException(ex));
    }

    private async Task CloseRequestAsync(string handle)
    {
        try
        {
            MessageBuffer message;
            using (var writer = Connection.GetMessageWriter())
            {
                writer.WriteMethodCallHeader(destination: BusName, path: handle, @interface: RequestInterface, member: "Close");
                message = writer.CreateMessage();
            }

            await Connection.CallMethodAsync(message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Debug($"Closing the portal request {handle} failed: {ex.Message}");
        }
    }

    public void Dispose() => Connection.Dispose();
}
