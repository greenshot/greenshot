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
/// Keeps Greenshot to one instance per user and session, and is the way to talk to it: the first instance listens on a
/// Unix socket in $XDG_RUNTIME_DIR (a folder only the user may open), the command line, the MCP server and later starts
/// connect to it. The protocol is the one of the named pipe of Windows: length-prefixed JSON frames, a HELLO frame binds
/// the source of the connection, then requests; replies are JSON or, for a terminal, text frames.
/// </summary>
public sealed class InstanceChannel : IDisposable
{
    /// <summary>
    /// The command which keeps a connection open for Greenshot's events (tools changed, Greenshot exits)
    /// </summary>
    public const string WatchCommand = "WATCH";

    private readonly Socket _socket;
    private readonly string _path;
    private readonly CancellationTokenSource _stop = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<OpenConnection, bool> _connections = new();

    /// <summary>
    /// A connection while it is open: its stream, the lock its frames are written with, and whether it watches
    /// </summary>
    private sealed class OpenConnection(Stream stream)
    {
        public Stream Stream { get; } = stream;

        public SemaphoreSlim WriteLock { get; } = new(1, 1);

        public bool IsWatching { get; set; }
    }

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
    /// True when a Greenshot listens
    /// </summary>
    public static bool IsRunning(string? path = null) => IsAlive(path ?? DefaultPath);

    /// <summary>
    /// Become the running instance, null when another one already listens
    /// </summary>
    public static InstanceChannel? TryListen(Func<IpcRequestContext, Task> handler, string? path = null)
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

        socket.Listen(16);
        var channel = new InstanceChannel(socket, path);
        _ = channel.AcceptLoopAsync(handler);
        return channel;
    }

    private static bool IsAlive(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

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

    private async Task AcceptLoopAsync(Func<IpcRequestContext, Task> handler)
    {
        while (!_stop.IsCancellationRequested)
        {
            Socket client;
            try
            {
                client = await _socket.AcceptAsync(_stop.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                Log.Warn("Accepting a connection failed", ex);
                continue;
            }

            _ = HandleConnectionAsync(client, handler);
        }
    }

    private async Task HandleConnectionAsync(Socket client, Func<IpcRequestContext, Task> handler)
    {
        using (client)
        {
            try
            {
                var peer = GetPeer(client);
                if (peer.UserId != GetUserId())
                {
                    // The folder is the user's, this can't happen unless its rights were changed
                    Log.Warn($"A connection of the user {peer.UserId} (process {peer.ProcessId}) is refused");
                    return;
                }

                await using var stream = new NetworkStream(client, ownsSocket: false);
                var connection = new OpenConnection(stream);
                byte[]? helloFrame;
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(5));
                    helloFrame = await IpcFraming.ReadFrameAsync(stream, 64 * 1024, timeout.Token).ConfigureAwait(false);
                }

                if (helloFrame == null)
                {
                    // A probe of IsRunning
                    return;
                }

                var hello = IpcEnvelope.FromBytes(helloFrame);
                if (hello is not { IsHello: true } || !IpcSources.IsKnown(hello.Source))
                {
                    Log.Warn($"A connection without a valid HELLO is closed (process {peer.ProcessId})");
                    return;
                }

                string source = hello.Source!.ToLowerInvariant();
                bool text = string.Equals(hello.ReplyFormat, IpcSources.ReplyFormatText, StringComparison.OrdinalIgnoreCase);
                object? aiClient = null;
                _connections[connection] = true;
                try
                {
                while (!_stop.IsCancellationRequested)
                {
                    var frame = await IpcFraming.ReadFrameAsync(stream, cancellationToken: _stop.Token).ConfigureAwait(false);
                    if (frame == null)
                    {
                        return;
                    }

                    var envelope = IpcEnvelope.FromBytes(frame);
                    if (envelope == null || envelope.IsHello)
                    {
                        Log.Warn($"An invalid request of '{source}' closes its connection");
                        return;
                    }

                    // The source of the connection, whatever the envelope says
                    envelope.Source = source;
                    if (string.Equals(envelope.Command, WatchCommand, StringComparison.OrdinalIgnoreCase) && !text)
                    {
                        // Only events are sent on it, nothing about the user's windows or recipes
                        connection.IsWatching = true;
                        await SendAsync(connection, new { status = "ok", exit_code = 0, watching = true }).ConfigureAwait(false);
                        continue;
                    }

                    var context = new IpcRequestContext(envelope, stream, peer, text, connection.WriteLock) { AiClient = aiClient };
                    if (source == IpcSources.Mcp)
                    {
                        context.AiClient ??= new McpHello(hello.Origin);
                    }

                    await handler(context).ConfigureAwait(false);
                    aiClient = context.AiClient;
                    if (context.IsClientGone)
                    {
                        return;
                    }
                }
                }
                finally
                {
                    _connections.TryRemove(connection, out _);
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException)
            {
                Log.Debug($"A connection ended: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error("A request failed", ex);
            }
        }
    }

    /// <summary>
    /// Sends an event (e.g. {"event":"tools_changed"}) to every watching connection
    /// </summary>
    public Task NotifyWatchersAsync(object message) =>
        Task.WhenAll(_connections.Keys.Where(c => c.IsWatching).Select(c => SendAsync(c, message)));

    /// <summary>
    /// Tells every open connection that Greenshot exits: the MCP server doesn't start it again then
    /// </summary>
    public Task NotifyShutdownAsync(string reason) =>
        Task.WhenAll(_connections.Keys.Select(c => SendAsync(c, new { @event = "shutdown", reason, greenshot_running = false })));

    private static async Task SendAsync(OpenConnection connection, object message)
    {
        await connection.WriteLock.WaitAsync().ConfigureAwait(false);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await IpcFraming.WriteFrameAsync(connection.Stream, System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message, message.GetType()), timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
        {
            Log.Debug($"An event could not be sent: {ex.Message}");
        }
        finally
        {
            connection.WriteLock.Release();
        }
    }

    /// <summary>
    /// What greenshot-mcp said in its HELLO, until Greenshot identified the AI tool itself
    /// </summary>
    public sealed record McpHello(string? Origin);

    /// <summary>
    /// The process and user on the other end, from the kernel
    /// </summary>
    public static IpcPeer GetPeer(Socket socket)
    {
        try
        {
            // struct ucred { pid_t pid; uid_t uid; gid_t gid; }, SOL_SOCKET = 1, SO_PEERCRED = 17
            Span<byte> credentials = stackalloc byte[12];
            int length = socket.GetRawSocketOption(1, 17, credentials);
            if (length >= 8)
            {
                return new IpcPeer(BitConverter.ToInt32(credentials[..4]), BitConverter.ToInt32(credentials.Slice(4, 4)));
            }
        }
        catch (SocketException ex)
        {
            Log.Warn($"The peer of a connection is unknown: {ex.Message}");
        }

        return IpcPeer.Unknown;
    }

    private static readonly Lazy<int> UserId = new(() =>
    {
        // "Uid:  real  effective  saved  filesystem"
        var line = File.ReadLines("/proc/self/status").FirstOrDefault(l => l.StartsWith("Uid:", StringComparison.Ordinal));
        return line != null && int.TryParse(line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[1], out int uid) ? uid : -1;
    });

    /// <summary>
    /// The user Greenshot runs as
    /// </summary>
    public static int GetUserId() => UserId.Value;

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

/// <summary>
/// A connection to the running Greenshot: the command line and the MCP server
/// </summary>
public sealed class InstanceClient : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;

    private InstanceClient(Socket socket)
    {
        _socket = socket;
        _stream = new NetworkStream(socket, ownsSocket: false);
    }

    /// <summary>
    /// Connect and say who this is, null when no Greenshot runs
    /// </summary>
    public static async Task<InstanceClient?> ConnectAsync(string source, string replyFormat, string? origin = null, string? path = null,
        CancellationToken cancellationToken = default)
    {
        path ??= InstanceChannel.DefaultPath;
        if (!File.Exists(path))
        {
            return null;
        }

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken).ConfigureAwait(false);
        }
        catch (SocketException)
        {
            // A socket file without a listener, left by a crashed instance
            socket.Dispose();
            return null;
        }

        var client = new InstanceClient(socket);
        await client.SendAsync(IpcEnvelope.CreateHello(source, replyFormat, origin), cancellationToken).ConfigureAwait(false);
        return client;
    }

    public Task SendAsync(IpcEnvelope envelope, CancellationToken cancellationToken = default) =>
        IpcFraming.WriteFrameAsync(_stream, envelope.ToBytes(), cancellationToken);

    /// <summary>
    /// The next frame of the reply, null when Greenshot closed the connection
    /// </summary>
    public Task<byte[]?> ReadFrameAsync(CancellationToken cancellationToken = default) =>
        IpcFraming.ReadFrameAsync(_stream, cancellationToken: cancellationToken);

    /// <summary>
    /// Runs a command line in the running Greenshot and prints what it answers; the exit code, null when none runs
    /// </summary>
    public static async Task<int?> RunCommandLineAsync(IReadOnlyList<string> argv, string? cwd, TextWriter stdout, TextWriter stderr,
        string? path = null, CancellationToken cancellationToken = default, string source = IpcSources.Cli)
    {
        await using var client = await ConnectAsync(source, IpcSources.ReplyFormatText, path: path, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            return null;
        }

        await client.SendAsync(IpcEnvelope.CreateCli(argv, source, cwd), cancellationToken).ConfigureAwait(false);
        while (true)
        {
            var frame = await client.ReadFrameAsync(cancellationToken).ConfigureAwait(false);
            if (frame == null)
            {
                // Greenshot ended (e.g. --exit) without an exit frame
                return 0;
            }

            if (frame.Length == 0)
            {
                continue;
            }

            switch (frame[0])
            {
                case IpcRequestContext.TextFrameStdout:
                    await stdout.WriteAsync(Encoding.UTF8.GetString(frame, 1, frame.Length - 1)).ConfigureAwait(false);
                    await stdout.FlushAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case IpcRequestContext.TextFrameStderr:
                    await stderr.WriteAsync(Encoding.UTF8.GetString(frame, 1, frame.Length - 1)).ConfigureAwait(false);
                    await stderr.FlushAsync(cancellationToken).ConfigureAwait(false);
                    break;
                case IpcRequestContext.TextFrameExit when frame.Length >= 5:
                    return BitConverter.ToInt32(frame, 1);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync().ConfigureAwait(false);
        _socket.Dispose();
    }
}
