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
using Newtonsoft.Json;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Encapsulates an incoming IPC request and provides bidirectional response capability on the connected stream.
    /// </summary>
    public class IpcRequestContext
    {
        public IpcEnvelope Envelope { get; }
        public Stream Stream { get; }

        public IpcRequestContext(IpcEnvelope envelope, Stream stream)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        /// <summary>
        /// Writes a framed 4-byte length prefixed JSON response back to the client stream.
        /// </summary>
        public async Task ReplyAsync(object responsePayload, CancellationToken cancellationToken = default)
        {
            if (responsePayload == null)
            {
                throw new ArgumentNullException(nameof(responsePayload));
            }

            string json = JsonConvert.SerializeObject(responsePayload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None
            });

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] lengthBytes = BitConverter.GetBytes((uint)jsonBytes.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            await Stream.WriteAsync(lengthBytes, 0, lengthBytes.Length, cancellationToken).ConfigureAwait(false);
            await Stream.WriteAsync(jsonBytes, 0, jsonBytes.Length, cancellationToken).ConfigureAwait(false);
            await Stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
