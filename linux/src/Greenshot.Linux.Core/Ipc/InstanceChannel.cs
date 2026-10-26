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

using System.Net.Sockets;
using System.Text;
using Greenshot.Linux.Core.Configuration;

namespace Greenshot.Linux.Core.Ipc;

/// <summary>
/// Keeps Greenshot to one instance per user and session: the first instance listens on a Unix socket
/// in $XDG_RUNTIME_DIR, later starts send their command line to it and end.
/// One line per connection: the command, answered with "ok" or "error: ...".
/// </summary>
public sealed class InstanceChannel : IDisposable
{
    private const int MaxLineLength = 4096;

    private readonly Socket _socket;
    private readonly string _path;
    private readonly CancellationTokenSource _stop = new();

    private InstanceChannel(Socket socket, string path)
    {
        _socket = socket;
        _path = path;
    }

    public static string DefaultPath
    {
        get
        {
            var display = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") ?? Environment.GetEnvironmentVariable("DISPLAY") ?? "default";
            return Path.Combine(XdgDirectories.RuntimeDir, "greenshot", $"greenshot-{display.Replace('/', '_').Replace(':', '_')}.sock");
        }
    }

    /// <summary>
    /// Send a command to the running instance, null when none runs
    /// </summary>
    public static async Task<string?> TrySendAsync(string command, string? path = null, CancellationToken cancellationToken = default)
    {
        path ??= DefaultPath;
        if (!File.Exists(path))
        {
            return null;
        }

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            // A socket file without a listener, left by a crashed instance
            return null;
        }

        await using var stream = new NetworkStream(socket, ownsSocket: false);
        var request = Encoding.UTF8.GetBytes(command.ReplaceLineEndings(" ") + "\n");
        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;
    }

    /// <summary>
    /// Become the running instance, null when another one already listens
    /// </summary>
    public static InstanceChannel? TryListen(Func<string, Task<string>> handler, string? path = null)
    {
        path ??= DefaultPath;
        var folder = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(folder);
        if (!OperatingSystem.IsWindows())
        {
            // Only the user may talk to Greenshot
            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            socket.Bind(new UnixDomainSocketEndPoint(path));
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            if (IsAlive(path))
            {
                socket.Dispose();
                return null;
            }

            // Left by an instance which crashed, take it over
            File.Delete(path);
            socket.Bind(new UnixDomainSocketEndPoint(path));
        }

        socket.Listen(8);
        var channel = new InstanceChannel(socket, path);
        _ = channel.AcceptLoopAsync(handler);
        return channel;
    }

    private static bool IsAlive(string path)
    {
        using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            probe.Connect(new UnixDomainSocketEndPoint(path));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private async Task AcceptLoopAsync(Func<string, Task<string>> handler)
    {
        while (!_stop.IsCancellationRequested)
        {
            Socket client;
            try
            {
                client = await _socket.AcceptAsync(_stop.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                Log.Warn("Accepting a command failed", ex);
                continue;
            }

            _ = HandleClientAsync(client, handler);
        }
    }

    private async Task HandleClientAsync(Socket client, Func<string, Task<string>> handler)
    {
        using (client)
        {
            try
            {
                await using var stream = new NetworkStream(client, ownsSocket: false);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                var line = await ReadLineAsync(stream, timeout.Token).ConfigureAwait(false);
                if (line.Length == 0)
                {
                    // A probe of IsAlive, or a client which gave up
                    return;
                }

                var answer = await handler(line).ConfigureAwait(false);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(answer.ReplaceLineEndings(" ") + "\n"), _stop.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException)
            {
                Log.Warn($"A command connection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Read one line, limited in length so a misbehaving client can't make Greenshot use memory without end
    /// </summary>
    private static async Task<string> ReadLineAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[MaxLineLength];
        int length = 0;
        while (length < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(length, 1), cancellationToken).ConfigureAwait(false);
            if (read == 0 || buffer[length] == (byte)'\n')
            {
                break;
            }

            length++;
        }

        return Encoding.UTF8.GetString(buffer, 0, length).Trim();
    }

    public void Dispose()
    {
        _stop.Cancel();
        _socket.Dispose();
        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // Nothing to clean up then
        }

        _stop.Dispose();
    }
}
