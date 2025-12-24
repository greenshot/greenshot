/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Greenshot.Ipc;

/// <summary>
/// Provides functionality to listen for incoming inter-process communication (IPC) commands over a named pipe and
/// invoke a callback when commands are received.
/// </summary>
internal class IpcListener
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(IpcListener));

    private readonly string _pipeName = IpcHelper.GetPipeName(Process.GetCurrentProcess().Id);
    private CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// Starts listening for incoming IPC commands on the configured named pipe and invokes the specified callback when
    /// a command is received.
    /// </summary>
    /// <remarks>
    /// Only reads data up to a maximum size defined by <see cref="IpcHelper.MaxAllowedReceivedBytes"/> to prevent excessive memory usage.
    /// </remarks>
    /// <param name="onCommandReceived">A callback action to invoke with the received AppCommands object.</param>
    /// <returns>The current IpcListener instance</returns>
    public IpcListener Start(Action<AppCommands> onCommandReceived)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        Log.DebugFormat("Start Listening on Pipe: {0}", _pipeName);

        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1, // only allow one connection at a time
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                try
                {
                    await server.WaitForConnectionAsync(cancellationToken);

                    using var ms = new MemoryStream();
                    var buffer = new byte[4096];
                    int bytesRead;
                    long total = 0;
                    
                    while ((bytesRead = await server.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        total += bytesRead;
                        if (total > IpcHelper.MaxAllowedReceivedBytes )
                        {
                            Log.Error("Limit of max message size reached. Operation canceled.");
                            throw new InvalidDataException("Message too large");
                        }
                        ms.Write(buffer, 0, bytesRead);
                    }

                    var raw = ms.ToArray();
                    var appCommands = IpcHelper.Deserialize(raw);

                    Log.DebugFormat("Received AppCommands {0} on Pipe {1} ", appCommands, _pipeName);

                    onCommandReceived(appCommands);
                    
                }
                catch (Exception e)
                {
                    Log.ErrorFormat("NamedPipeConnectionError: {0} on Pipe {1}", e.Message , _pipeName);
                    // do nothing, just wait for next connection
                }
                finally
                {
                    server.Dispose();
                }
            }
        }, cancellationToken);

        return this;
    }

    /// <summary>
    /// Requests cancellation of the current listening operation.
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
    }
}

