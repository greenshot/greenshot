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

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Greenshot.Linux.Core.Ipc;

/// <summary>
/// The peer of a connection, from the socket (SO_PEERCRED): the kernel tells it, the client can't claim another one
/// </summary>
public sealed record IpcPeer(int ProcessId, int UserId)
{
    public static IpcPeer Unknown { get; } = new(0, -1);
}

/// <summary>
/// A request and the way back to its client, the IpcRequestContext of Windows. Handlers reply with objects: streamed
/// chunks <c>{stream, text}</c> and one final reply <c>{status, exit_code, stdout, stderr, ...}</c>. For a terminal
/// (reply_format "text") they become text frames: 'O' + UTF-8 (stdout), 'E' + UTF-8 (stderr), 'X' + int32 (exit code).
/// With --json the final reply itself is the stdout text.
/// </summary>
public sealed class IpcRequestContext
{
    public const byte TextFrameStdout = (byte)'O';
    public const byte TextFrameStderr = (byte)'E';
    public const byte TextFrameExit = (byte)'X';

    /// <summary>
    /// Shared by a request and the contexts derived from it, so the final reply is sent once
    /// </summary>
    private sealed class ReplyState
    {
        public bool Completed;
        public bool ClientGone;
    }

    private static readonly JsonSerializerOptions ReplyOptions = new() { WriteIndented = false };
    private static readonly JsonSerializerOptions ResultOptions = new() { WriteIndented = true };

    private readonly SemaphoreSlim _writeLock;
    private readonly ReplyState _state;

    public IpcRequestContext(IpcEnvelope envelope, Stream stream, IpcPeer peer, bool usesTextFrames)
        : this(envelope, stream, peer, usesTextFrames, new SemaphoreSlim(1, 1), new ReplyState())
    {
    }

    private IpcRequestContext(IpcEnvelope envelope, Stream stream, IpcPeer peer, bool usesTextFrames, SemaphoreSlim writeLock, ReplyState state)
    {
        Envelope = envelope;
        Stream = stream;
        Peer = peer;
        UsesTextFrames = usesTextFrames;
        _writeLock = writeLock;
        _state = state;
    }

    public IpcEnvelope Envelope { get; }

    public Stream Stream { get; }

    public IpcPeer Peer { get; }

    /// <summary>
    /// Source "mcp": the AI tool which uses Greenshot, as Greenshot identified it
    /// </summary>
    public object? AiClient { get; set; }

    /// <summary>
    /// A terminal, text instead of JSON
    /// </summary>
    public bool UsesTextFrames { get; }

    public bool IsReplyCompleted => _state.Completed;

    /// <summary>
    /// The client went away without reading its reply
    /// </summary>
    public bool IsClientGone => _state.ClientGone;

    /// <summary>
    /// How long a frame may take to be read, a terminal can pause its output
    /// </summary>
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// The same connection and reply with another envelope, e.g. the command parsed from a command line
    /// </summary>
    public IpcRequestContext WithEnvelope(IpcEnvelope envelope) =>
        new(envelope, Stream, Peer, UsesTextFrames, _writeLock, _state) { AiClient = AiClient, WriteTimeout = WriteTimeout };

    /// <summary>
    /// The reply as text when the terminal wants text (not --json)
    /// </summary>
    public bool WantsTextOutput => UsesTextFrames && !Envelope.Json;

    /// <summary>
    /// Sends a streamed chunk or the final reply
    /// </summary>
    public async Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reply);
        if (!UsesTextFrames)
        {
            await WriteFrameAsync(JsonSerializer.SerializeToUtf8Bytes(reply, reply.GetType(), ReplyOptions), cancellationToken).ConfigureAwait(false);
            return;
        }

        var node = JsonSerializer.SerializeToNode(reply, reply.GetType(), ReplyOptions) as JsonObject ?? new JsonObject();
        if (GetText(node, "stream") is { } stream)
        {
            await WriteTextFrameAsync(stream.Equals("stderr", StringComparison.OrdinalIgnoreCase) ? TextFrameStderr : TextFrameStdout,
                GetText(node, "text"), cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_state.Completed)
        {
            Log.Warn("An additional final reply to a request which already completed is ignored");
            return;
        }

        if (Envelope.Json)
        {
            await WriteTextFrameAsync(TextFrameStdout, node.ToJsonString(ResultOptions), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WriteTextFrameAsync(TextFrameStdout, GetText(node, "stdout"), cancellationToken).ConfigureAwait(false);
            await WriteTextFrameAsync(TextFrameStderr, GetText(node, "stderr"), cancellationToken).ConfigureAwait(false);
        }

        int exitCode = node["exit_code"] is JsonValue value && value.TryGetValue(out int code) ? code : 0;
        if (exitCode == 0 && string.Equals(GetText(node, "status"), "error", StringComparison.OrdinalIgnoreCase))
        {
            exitCode = 1;
        }

        await CompleteAsync(exitCode, cancellationToken).ConfigureAwait(false);
    }

    private static string? GetText(JsonObject node, string name)
    {
        var value = node[name];
        return value is JsonValue jsonValue && jsonValue.TryGetValue(out string? text) ? text : value?.ToJsonString();
    }

    /// <summary>
    /// Text mode: the exit frame, unless the reply was already completed
    /// </summary>
    public async Task CompleteAsync(int exitCode = 0, CancellationToken cancellationToken = default)
    {
        if (!UsesTextFrames || _state.Completed)
        {
            return;
        }

        _state.Completed = true;
        var frame = new byte[5];
        frame[0] = TextFrameExit;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(1), exitCode);
        await WriteFrameAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    private Task WriteTextFrameAsync(byte type, string? text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Task.CompletedTask;
        }

        if (!text.EndsWith('\n'))
        {
            text += "\n";
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        var frame = new byte[bytes.Length + 1];
        frame[0] = type;
        bytes.CopyTo(frame, 1);
        return WriteFrameAsync(frame, cancellationToken);
    }

    private async Task WriteFrameAsync(byte[] payload, CancellationToken cancellationToken)
    {
        if (_state.ClientGone)
        {
            return;
        }

        // Streamed output can come from parallel branches of a recipe: frames must not interleave
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(WriteTimeout);
            await IpcFraming.WriteFrameAsync(Stream, payload, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            // Not an error: the client stopped (Ctrl+C) or doesn't read its reply
            _state.ClientGone = true;
            Log.Debug($"The client of '{Envelope.Command}' is gone, its reply is dropped: {ex.Message}");
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
