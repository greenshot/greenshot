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
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Mcp
{
    /// <summary>
    /// A failure talking to Greenshot (not running, connection closed, invalid reply).
    /// </summary>
    public sealed class GreenshotConnectionException : Exception
    {
        public GreenshotConnectionException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// One request to the running Greenshot over its named pipe (Greenshot_&lt;user SID&gt;), using the protocol of
    /// greenshot-cli.exe and the browser extension: frames with a 4-byte little-endian length and UTF-8 JSON. The first frame is
    /// HELLO with source "mcp", which selects the command whitelist and consent rules for AI tools in Greenshot.
    /// </summary>
    public static class GreenshotConnection
    {
        private const string PipePrefix = "Greenshot_";
        private const int ConnectTimeoutMilliseconds = 2000;
        private const int StartupTimeoutMilliseconds = 20000;
        // A capture of several 4K displays as base64 PNG can be large
        private const int MaxReplySize = 256 * 1024 * 1024;

        public static string McpServerVersion => typeof(GreenshotConnection).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        /// <summary>
        /// Sends the request and returns Greenshot's final reply, with streamed stdout / stderr text collected in
        /// "streamed_stdout" / "streamed_stderr".
        /// </summary>
        /// <param name="request">Envelope with at least "command"</param>
        /// <param name="clientName">Name of the AI tool, shown to the user when Greenshot asks for consent</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="startGreenshot">Start Greenshot when it isn't running (false for background requests)</param>
        public static async Task<JsonObject> SendAsync(JsonObject request, string? clientName, CancellationToken cancellationToken, bool startGreenshot = true)
        {
            // The user closed Greenshot: it isn't started again behind the user's back, it has to be started by the user
            await using var pipe = await ConnectAsync(startGreenshot && !GreenshotState.IsClosedByUser, cancellationToken).ConfigureAwait(false);
            await SendHelloAsync(pipe, clientName, cancellationToken).ConfigureAwait(false);

            request["version"] = 1;
            request["source"] = "mcp";
            await WriteFrameAsync(pipe, request, cancellationToken).ConfigureAwait(false);

            var streamedStdout = new StringBuilder();
            var streamedStderr = new StringBuilder();
            while (true)
            {
                var frame = await ReadFrameAsync(pipe, cancellationToken).ConfigureAwait(false)
                            ?? throw new GreenshotConnectionException("Greenshot closed the connection without a reply.");

                string? greenshotEvent = (string?)frame["event"];
                if (greenshotEvent != null)
                {
                    // Greenshot's own messages can arrive on any connection
                    if (string.Equals(greenshotEvent, "shutdown", StringComparison.OrdinalIgnoreCase))
                    {
                        GreenshotState.OnShutdown((string?)frame["reason"]);
                        throw new GreenshotConnectionException("Greenshot is exiting, the request was not finished.");
                    }
                    continue;
                }

                string? stream = (string?)frame["stream"];
                if (stream != null)
                {
                    var target = string.Equals(stream, "stderr", StringComparison.OrdinalIgnoreCase) ? streamedStderr : streamedStdout;
                    target.AppendLine((string?)frame["text"]);
                    continue;
                }

                if (streamedStdout.Length > 0)
                {
                    frame["streamed_stdout"] = streamedStdout.ToString().TrimEnd();
                }
                if (streamedStderr.Length > 0)
                {
                    frame["streamed_stderr"] = streamedStderr.ToString().TrimEnd();
                }
                return frame;
            }
        }

        /// <summary>
        /// Opens a connection which stays open for Greenshot's events (WATCH), without starting Greenshot.
        /// The pipe is null when Greenshot isn't running, or doesn't know WATCH (an older version, IsRunning is true).
        /// </summary>
        internal static async Task<(NamedPipeClientStream? Pipe, bool IsRunning)> OpenWatchAsync(CancellationToken cancellationToken)
        {
            NamedPipeClientStream pipe;
            try
            {
                pipe = await ConnectAsync(false, cancellationToken).ConfigureAwait(false);
            }
            catch (GreenshotConnectionException)
            {
                return (null, false);
            }

            try
            {
                await SendHelloAsync(pipe, "greenshot-mcp", cancellationToken).ConfigureAwait(false);
                await WriteFrameAsync(pipe, new JsonObject { ["version"] = 1, ["command"] = "WATCH", ["source"] = "mcp" }, cancellationToken).ConfigureAwait(false);
                var reply = await ReadFrameAsync(pipe, cancellationToken).ConfigureAwait(false);
                if (reply != null && reply["watching"] is JsonValue watching && watching.TryGetValue(out bool isWatching) && isWatching)
                {
                    return (pipe, true);
                }
            }
            catch (Exception ex) when (ex is GreenshotConnectionException || ex is IOException)
            {
                // Treated like an older Greenshot
            }
            await pipe.DisposeAsync().ConfigureAwait(false);
            return (null, true);
        }

        private static Task SendHelloAsync(Stream pipe, string? clientName, CancellationToken cancellationToken)
        {
            var hello = new JsonObject
            {
                ["version"] = 1,
                ["command"] = "HELLO",
                ["source"] = "mcp",
                ["origin"] = string.IsNullOrWhiteSpace(clientName) ? "AI tool" : clientName,
                ["reply_format"] = "json"
            };
            return WriteFrameAsync(pipe, hello, cancellationToken);
        }

        private static string GetPipeName()
        {
            var user = WindowsIdentity.GetCurrent().User
                       ?? throw new GreenshotConnectionException("Could not get the Windows user SID.");
            return PipePrefix + user.Value;
        }

        /// <summary>
        /// Connects to Greenshot; starts Greenshot.exe from the same directory when it isn't running (and startGreenshot is true).
        /// </summary>
        private static async Task<NamedPipeClientStream> ConnectAsync(bool startGreenshot, CancellationToken cancellationToken)
        {
            string pipeName = GetPipeName();
            var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(ConnectTimeoutMilliseconds, cancellationToken).ConfigureAwait(false);
                return pipe;
            }
            catch (TimeoutException)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
            }

            string greenshotExe = Path.Combine(AppContext.BaseDirectory, "Greenshot.exe");
            if (!startGreenshot || !File.Exists(greenshotExe))
            {
                throw new GreenshotConnectionException(GreenshotState.IsClosedByUser
                    ? "Greenshot was closed by the user. Ask the user to start Greenshot to use these tools."
                    : "Greenshot is not running. Please start Greenshot and try again.");
            }

            try
            {
                using var process = Process.Start(new ProcessStartInfo(greenshotExe) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                throw new GreenshotConnectionException($"Greenshot is not running and could not be started: {ex.Message}", ex);
            }

            pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(StartupTimeoutMilliseconds, cancellationToken).ConfigureAwait(false);
                return pipe;
            }
            catch (TimeoutException ex)
            {
                await pipe.DisposeAsync().ConfigureAwait(false);
                throw new GreenshotConnectionException("Greenshot was started, but did not accept the connection in time. Please try again.", ex);
            }
        }

        internal static async Task WriteFrameAsync(Stream stream, JsonObject message, CancellationToken cancellationToken)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message.ToJsonString());
            byte[] frame = new byte[4 + payload.Length];
            BinaryPrimitives.WriteInt32LittleEndian(frame, payload.Length);
            payload.CopyTo(frame, 4);
            await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads one frame, null when the connection was closed before a frame started.
        /// </summary>
        internal static async Task<JsonObject?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] lengthBytes = new byte[4];
            int read = await ReadExactlyOrEndAsync(stream, lengthBytes, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return null;
            }
            if (read < 4)
            {
                throw new GreenshotConnectionException("Greenshot closed the connection in the middle of a reply.");
            }

            int length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
            if (length <= 0 || length > MaxReplySize)
            {
                throw new GreenshotConnectionException($"Greenshot sent an invalid reply length ({length}).");
            }

            byte[] payload = new byte[length];
            if (await ReadExactlyOrEndAsync(stream, payload, cancellationToken).ConfigureAwait(false) < length)
            {
                throw new GreenshotConnectionException("Greenshot closed the connection in the middle of a reply.");
            }

            return JsonNode.Parse(payload) as JsonObject
                   ?? throw new GreenshotConnectionException("Greenshot sent a reply which is not a JSON object.");
        }

        private static async Task<int> ReadExactlyOrEndAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }
                total += read;
            }
            return total;
        }
    }
}
