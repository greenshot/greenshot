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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Encapsulates an incoming IPC request and provides bidirectional response capability on the connected stream.
    /// </summary>
    /// <remarks>
    /// Handlers always reply with JSON-shaped objects: streaming chunks <c>{stream, text}</c> and one final reply
    /// <c>{status, exit_code, stdout, stderr, ...}</c>. For connections that announced <c>reply_format: "text"</c> in their HELLO
    /// (greenshot.com / greenshot-proxy.exe) these are translated to text frames, so the executables never parse JSON:
    /// <c>'O' + UTF-8</c> (stdout), <c>'E' + UTF-8</c> (stderr) and <c>'X' + int32 exit code</c> (end of the reply).
    /// With <c>--json</c> the final reply object itself is sent as stdout text.
    /// </remarks>
    public class IpcRequestContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IpcRequestContext));

        public const byte TextFrameStdout = (byte)'O';
        public const byte TextFrameStderr = (byte)'E';
        public const byte TextFrameExit = (byte)'X';

        private static readonly JsonSerializer ReplySerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None
        });

        /// <summary>
        /// Shared by a request and the contexts derived from it (see <see cref="WithEnvelope"/>), so the final reply is sent only once.
        /// </summary>
        private sealed class ReplyState
        {
            public bool Completed;
        }

        private readonly SemaphoreSlim _writeLock;
        private readonly ReplyState _replyState;

        public IpcEnvelope Envelope { get; }
        public Stream Stream { get; }

        /// <summary>
        /// Origin of the calling browser extension, as announced in the connection's HELLO frame (native messaging only).
        /// </summary>
        public string ConnectionOrigin { get; set; }

        /// <summary>
        /// True when the connection announced text replies (terminal / shell) instead of JSON.
        /// </summary>
        public bool UsesTextFrames { get; set; }

        /// <summary>
        /// True once the final reply (text mode: the exit frame) has been sent.
        /// </summary>
        public bool IsReplyCompleted => _replyState.Completed;

        /// <summary>
        /// Default for <see cref="WriteTimeout"/>: generous, as a console can pause output (e.g. while selecting text).
        /// </summary>
        public static readonly TimeSpan DefaultWriteTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// How long a frame may take to be read by the client, before the connection is closed.
        /// </summary>
        public TimeSpan WriteTimeout { get; set; } = DefaultWriteTimeout;

        public IpcRequestContext(IpcEnvelope envelope, Stream stream, SemaphoreSlim connectionWriteLock = null)
            : this(envelope, stream, connectionWriteLock ?? new SemaphoreSlim(1, 1), new ReplyState())
        {
        }

        private IpcRequestContext(IpcEnvelope envelope, Stream stream, SemaphoreSlim writeLock, ReplyState replyState)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            // Serializes writes to the connection: streamed stdout/stderr frames can be emitted concurrently from parallel DAG
            // branches, and a length prefix + body must never interleave with another frame.
            _writeLock = writeLock;
            _replyState = replyState;
        }

        /// <summary>
        /// Creates a context for the same connection and reply, but another envelope (e.g. the command parsed from a CLI request).
        /// </summary>
        public IpcRequestContext WithEnvelope(IpcEnvelope envelope)
        {
            return new IpcRequestContext(envelope, Stream, _writeLock, _replyState)
            {
                ConnectionOrigin = ConnectionOrigin,
                UsesTextFrames = UsesTextFrames,
                WriteTimeout = WriteTimeout
            };
        }

        /// <summary>
        /// Sends a streaming chunk or the final reply.
        /// </summary>
        public async Task ReplyAsync(object responsePayload, CancellationToken cancellationToken = default)
        {
            if (responsePayload == null)
            {
                throw new ArgumentNullException(nameof(responsePayload));
            }

            if (!UsesTextFrames)
            {
                string json = JsonConvert.SerializeObject(responsePayload, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None
                });
                await WriteFrameAsync(Encoding.UTF8.GetBytes(json), cancellationToken).ConfigureAwait(false);
                return;
            }

            var reply = JObject.FromObject(responsePayload, ReplySerializer);
            string stream = reply.Value<string>("stream");
            if (stream != null)
            {
                string text = reply.Value<string>("text");
                byte type = string.Equals(stream, "stderr", StringComparison.OrdinalIgnoreCase) ? TextFrameStderr : TextFrameStdout;
                await WriteTextFrameAsync(type, text, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (_replyState.Completed)
            {
                Log.Warn("Ignoring an additional final reply for a request that already completed.");
                return;
            }

            if (Envelope.Json)
            {
                // --json: the reply object is the result document
                await WriteTextFrameAsync(TextFrameStdout, reply.ToString(Formatting.None), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteTextFrameAsync(TextFrameStdout, reply.Value<string>("stdout"), cancellationToken).ConfigureAwait(false);
                await WriteTextFrameAsync(TextFrameStderr, reply.Value<string>("stderr"), cancellationToken).ConfigureAwait(false);
            }

            int exitCode = reply.Value<int?>("exit_code") ?? 0;
            if (exitCode == 0 && string.Equals(reply.Value<string>("status"), "error", StringComparison.OrdinalIgnoreCase))
            {
                exitCode = 1;
            }
            await CompleteAsync(exitCode, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Text mode: sends the exit frame unless the reply was already completed (e.g. for commands without a reply).
        /// Has no effect for JSON connections.
        /// </summary>
        public async Task CompleteAsync(int exitCode = 0, CancellationToken cancellationToken = default)
        {
            if (!UsesTextFrames || _replyState.Completed)
            {
                return;
            }
            _replyState.Completed = true;

            byte[] frame = new byte[5];
            frame[0] = TextFrameExit;
            byte[] code = BitConverter.GetBytes(exitCode);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(code);
            }
            Buffer.BlockCopy(code, 0, frame, 1, 4);
            await WriteFrameAsync(frame, cancellationToken).ConfigureAwait(false);
        }

        private Task WriteTextFrameAsync(byte type, string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Task.CompletedTask;
            }
            if (!text.EndsWith("\n", StringComparison.Ordinal))
            {
                text += "\n";
            }
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] frame = new byte[textBytes.Length + 1];
            frame[0] = type;
            Buffer.BlockCopy(textBytes, 0, frame, 1, textBytes.Length);
            return WriteFrameAsync(frame, cancellationToken);
        }

        /// <summary>
        /// Writes a 4-byte little-endian length prefix followed by the payload.
        /// </summary>
        private static async Task<bool> CompletesWithinAsync(Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var completed = await Task.WhenAny(task, Task.Delay(timeout, delayCancellation.Token)).ConfigureAwait(false);
                // Stop the timer when the write finished first
                delayCancellation.Cancel();
                return completed == task;
            }
        }

        private async Task WriteFrameAsync(byte[] payload, CancellationToken cancellationToken)
        {
            byte[] lengthBytes = BitConverter.GetBytes((uint)payload.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }
            // One write per frame. No Flush: a pipe has no stream buffer, and FlushFileBuffers would wait until the client read everything.
            byte[] frame = new byte[lengthBytes.Length + payload.Length];
            Buffer.BlockCopy(lengthBytes, 0, frame, 0, lengthBytes.Length);
            Buffer.BlockCopy(payload, 0, frame, lengthBytes.Length, payload.Length);

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var writeTask = Stream.WriteAsync(frame, 0, frame.Length, cancellationToken);
                if (!writeTask.IsCompleted && !await CompletesWithinAsync(writeTask, WriteTimeout, cancellationToken).ConfigureAwait(false))
                {
                    // The client does not read its replies. Closing the connection ends the pending write (and the connection),
                    // instead of keeping the handler waiting forever.
                    Log.Warn($"IPC client did not read its reply within {WriteTimeout.TotalSeconds:0} seconds, closing the connection.");
                    Stream.Dispose();
                    _ = writeTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new IOException("The IPC client did not read the reply in time.");
                }
                await writeTask.ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }
    }
}
