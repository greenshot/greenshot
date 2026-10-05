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
using log4net;
using Newtonsoft.Json;

namespace Greenshot.Ipc
{
    /// <summary>
    /// Client for sending framed JSON IPC envelopes over the user-SID scoped named pipe.
    /// </summary>
    public static class NamedPipeClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NamedPipeClient));

        private static void WriteFrame(Stream stream, IpcEnvelope envelope)
        {
            string json = JsonConvert.SerializeObject(envelope);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(json);
            byte[] lengthBytes = BitConverter.GetBytes((uint)payloadBytes.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }
            stream.Write(lengthBytes, 0, lengthBytes.Length);
            stream.Write(payloadBytes, 0, payloadBytes.Length);
        }

        public static bool SendMessage(IpcEnvelope envelope, int timeoutMs = 2000)
        {
            return SendMessage(NamedPipeEndpoint.GetPipeName(), envelope, timeoutMs);
        }

        public static bool SendMessage(string pipeName, IpcEnvelope envelope, int timeoutMs = 2000)
        {
            if (envelope == null)
            {
                throw new ArgumentNullException(nameof(envelope));
            }

            try
            {
                using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None))
                {
                    pipeClient.Connect(timeoutMs);

                    // Every connection starts with HELLO, which binds the connection's source on the server
                    WriteFrame(pipeClient, IpcEnvelope.CreateHello(envelope.Source ?? IpcSources.Cli));
                    WriteFrame(pipeClient, envelope);
                    pipeClient.Flush();
                    return true;
                }
            }
            catch (TimeoutException ex)
            {
                Log.Warn($"Timed out connecting to named pipe '{pipeName}'", ex);
                return false;
            }
            catch (Exception ex)
            {
                Log.Warn($"Failed to send message over named pipe '{pipeName}'", ex);
                return false;
            }
        }
    }
}
