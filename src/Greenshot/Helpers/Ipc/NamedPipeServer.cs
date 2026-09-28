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
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Server listener for incoming framed JSON IPC envelopes on the user-SID scoped named pipe.
    /// Supports bidirectional communication, multiple concurrent client connections, and persistent sessions.
    /// </summary>
    public class NamedPipeServer : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NamedPipeServer));
        private const int MaxPayloadSize = 64 * 1024 * 1024; // 64 MB cap per ADR 003

        private readonly string _pipeName;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenerTask;
        private bool _disposed;

        public event EventHandler<IpcEnvelope> MessageReceived;
        public event Func<IpcRequestContext, Task> RequestReceived;

        public NamedPipeServer() : this(NamedPipeEndpoint.GetPipeName())
        {
        }

        public NamedPipeServer(string pipeName)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        }

        public void Start()
        {
            if (_listenerTask != null)
            {
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _listenerTask = Task.Run(() => ListenLoopAsync(_cancellationTokenSource.Token));
        }

        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                NamedPipeServerStream serverStream = null;
                try
                {
                    PipeSecurity pipeSecurity = NamedPipeEndpoint.CreateServerSecurity();
                    serverStream = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        0,
                        0,
                        pipeSecurity);

                    await serverStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                    var connectedStream = serverStream;
                    serverStream = null; // Ownership transferred to ProcessClientAsync

                    _ = Task.Run(() => ProcessClientAsync(connectedStream, cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    Log.Error("Error while listening on named pipe", ex);
                    try
                    {
                        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                finally
                {
                    serverStream?.Dispose();
                }
            }
        }

        private async Task ProcessClientAsync(NamedPipeServerStream stream, CancellationToken cancellationToken)
        {
            using (stream)
            {
                try
                {
                    byte[] lengthBytes = new byte[4];

                    // Connection identity: bound once from the mandatory HELLO frame, never from later envelopes.
                    string connectionSource = null;
                    string connectionOrigin = null;
                    bool connectionUsesTextFrames = false;
                    var connectionWriteLock = new SemaphoreSlim(1, 1);

                    while (!cancellationToken.IsCancellationRequested && stream.IsConnected)
                    {
                        int read = await ReadExactAsync(stream, lengthBytes, 0, 4, cancellationToken).ConfigureAwait(false);
                        if (read == 0)
                        {
                            // Client disconnected cleanly
                            break;
                        }

                        if (read < 4)
                        {
                            Log.Warn("Named pipe client disconnected before sending full 4-byte length prefix.");
                            break;
                        }

                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(lengthBytes);
                        }
                        uint payloadLength = BitConverter.ToUInt32(lengthBytes, 0);

                        if (payloadLength < 2 || payloadLength > MaxPayloadSize)
                        {
                            Log.Warn($"[SECURITY] Invalid or oversized payload received on named pipe: {payloadLength} bytes. Closing connection.");
                            break;
                        }

                        byte[] payloadBytes = new byte[payloadLength];
                        read = await ReadExactAsync(stream, payloadBytes, 0, (int)payloadLength, cancellationToken).ConfigureAwait(false);
                        if (read < payloadLength)
                        {
                            Log.Warn("Named pipe client disconnected before sending full payload.");
                            break;
                        }

                        string json = Encoding.UTF8.GetString(payloadBytes);
                        var envelope = JsonConvert.DeserializeObject<IpcEnvelope>(json, new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.None
                        });

                        if (envelope == null)
                        {
                            continue;
                        }

                        if (connectionSource == null)
                        {
                            // The first frame must be HELLO, announcing a known source.
                            if (!envelope.IsHello || !IpcSources.IsKnown(envelope.Source))
                            {
                                Log.Warn($"[SECURITY] Named pipe connection rejected: first frame must be HELLO with a known source (got command '{envelope.Command}', source '{envelope.Source}').");
                                await RejectAsync(stream, connectionWriteLock, "[SECURITY] Connection rejected: the first message must be HELLO with a known source.", cancellationToken).ConfigureAwait(false);
                                break;
                            }

                            string replyFormat = string.IsNullOrEmpty(envelope.ReplyFormat) ? IpcSources.ReplyFormatJson : envelope.ReplyFormat;
                            if (!string.Equals(replyFormat, IpcSources.ReplyFormatJson, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(replyFormat, IpcSources.ReplyFormatText, StringComparison.OrdinalIgnoreCase))
                            {
                                Log.Warn($"[SECURITY] Named pipe connection rejected: unknown reply format '{replyFormat}'.");
                                await RejectAsync(stream, connectionWriteLock, "[SECURITY] Connection rejected: unknown reply format.", cancellationToken).ConfigureAwait(false);
                                break;
                            }

                            connectionSource = envelope.Source.ToLowerInvariant();
                            connectionOrigin = envelope.Origin;
                            connectionUsesTextFrames = string.Equals(replyFormat, IpcSources.ReplyFormatText, StringComparison.OrdinalIgnoreCase);
                            Log.Debug($"Named pipe connection identified: source '{connectionSource}'{(string.IsNullOrEmpty(connectionOrigin) ? string.Empty : $", origin '{connectionOrigin}'")}.");
                            continue;
                        }

                        if (envelope.IsHello)
                        {
                            Log.Warn($"[SECURITY] Named pipe connection closed: repeated HELLO on a connection already identified as '{connectionSource}'.");
                            await RejectAsync(stream, connectionWriteLock, "[SECURITY] Connection rejected: HELLO is only allowed as the first message.", cancellationToken).ConfigureAwait(false);
                            break;
                        }

                        // Whatever the client put into "source" is ignored; the connection's HELLO decides.
                        envelope.Source = connectionSource;

                        var context = new IpcRequestContext(envelope, stream, connectionWriteLock)
                        {
                            ConnectionOrigin = connectionOrigin,
                            UsesTextFrames = connectionUsesTextFrames
                        };

                        try
                        {
                            if (RequestReceived != null)
                            {
                                await RequestReceived.Invoke(context).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            Log.Error($"Error handling IPC command '{envelope.Command}'", ex);
                            if (!context.IsReplyCompleted)
                            {
                                await context.ReplyAsync(new
                                {
                                    status = "error",
                                    exit_code = 1,
                                    stderr = $"Error: Greenshot failed to handle the command: {ex.Message}"
                                }, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        // Text clients wait for the exit frame; commands without a reply (or failing ones) must still end the reply
                        await context.CompleteAsync(0, cancellationToken).ConfigureAwait(false);

                        MessageReceived?.Invoke(this, envelope);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Shutting down
                }
                catch (Exception ex)
                {
                    Log.Error("Error processing incoming message from named pipe client", ex);
                }
            }
        }

        /// <summary>
        /// Sends a final error frame before the server closes a connection that violates the protocol.
        /// </summary>
        private static async Task RejectAsync(Stream stream, SemaphoreSlim writeLock, string message, CancellationToken cancellationToken)
        {
            try
            {
                var context = new IpcRequestContext(new IpcEnvelope(), stream, writeLock);
                await context.ReplyAsync(new
                {
                    status = "error",
                    exit_code = 1,
                    stderr = message
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Debug("Could not send rejection to named pipe client", ex);
            }
        }

        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }
                totalRead += read;
            }
            return totalRead;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Debug("Exception while disposing NamedPipeServer", ex);
            }
        }
    }
}
