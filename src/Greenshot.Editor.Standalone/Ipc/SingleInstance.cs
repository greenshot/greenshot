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
using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Editor.Standalone.Diagnostics;

namespace Greenshot.Editor.Standalone.Ipc
{
    /// <summary>
    /// One editor per user: an editor which is started while another runs hands its files to that one over a named pipe
    /// (only for the same user) and exits. This is also how Greenshot gives a capture to a running editor: it just starts
    /// the editor with the file.
    /// </summary>
    public static class SingleInstance
    {
        private const int ConnectTimeout = 300;
        private const int MaxRequestSize = 1024 * 1024;
        private static CancellationTokenSource _stop;

        /// <summary>
        /// Another editor was started with these files. Raised on a thread of the pool.
        /// </summary>
        public static event Action<StartRequest> Received;

        public static string PipeName { get; set; } = "GreenshotEditor_" + Environment.UserName;

        /// <summary>
        /// Gives the request to the editor which already runs. False when there is none: this editor is the one.
        /// </summary>
        public static bool TrySend(StartRequest request)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
                pipe.Connect(ConnectTimeout);
                JsonSerializer.Serialize(pipe, request, StartRequestJson.Default.StartRequest);
                pipe.Flush();
                return true;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Listens for editors which are started later
        /// </summary>
        public static void Listen()
        {
            _stop = new CancellationTokenSource();
            ListenAsync(_stop.Token).Forget("Listening for other editors");
        }

        public static void Stop() => _stop?.Cancel();

        private static async Task ListenAsync(CancellationToken cancellationToken)
        {
            // Off the thread which started it: this runs for as long as the editor does
            await Task.Yield();
            while (!cancellationToken.IsCancellationRequested)
            {
                await using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    using var buffer = new MemoryStream();
                    var chunk = new byte[8192];
                    int read;
                    while ((read = await pipe.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0 && buffer.Length <= MaxRequestSize)
                    {
                        await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }

                    if (buffer.Length is > 0 and <= MaxRequestSize && JsonSerializer.Deserialize(buffer.ToArray(), StartRequestJson.Default.StartRequest) is StartRequest request)
                    {
                        Received?.Invoke(request);
                    }
                }
                catch (Exception ex) when (ex is IOException or JsonException)
                {
                    EditorLog.Warn("The request of another editor could not be read", ex);
                }
            }
        }
    }
}
