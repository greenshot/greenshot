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

                        if (envelope != null)
                        {
                            var context = new IpcRequestContext(envelope, stream);

                            if (RequestReceived != null)
                            {
                                await RequestReceived.Invoke(context).ConfigureAwait(false);
                            }

                            MessageReceived?.Invoke(this, envelope);
                        }
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
