/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using System.IO;
using Windows.Storage.Streams;

namespace Greenshot.Plugin.Win10.Internal
{
    /// <summary>
    /// This is an IRandomAccessStream implementation which uses a MemoryStream
    /// </summary>
    internal sealed class MemoryRandomAccessStream : MemoryStream, IRandomAccessStream
    {
        /// <inheritdoc />
        public IInputStream GetInputStreamAt(ulong position)
        {
            Seek((long) position, SeekOrigin.Begin);

            return this.AsInputStream();
        }

        /// <inheritdoc />
        public IOutputStream GetOutputStreamAt(ulong position)
        {
            Seek((long) position, SeekOrigin.Begin);

            return this.AsOutputStream();
        }

        /// <inheritdoc />
        ulong IRandomAccessStream.Position => (ulong) Position;

        /// <inheritdoc />
        public ulong Size
        {
            get { return (ulong) Length; }
            set { SetLength((long) value); }
        }

        /// <inheritdoc />
        public IRandomAccessStream CloneStream()
        {
            var cloned = new MemoryRandomAccessStream();
            CopyTo(cloned);
            return cloned;
        }

        /// <inheritdoc />
        public void Seek(ulong position)
        {
            Seek((long) position, SeekOrigin.Begin);
        }

        /// <inheritdoc />
        public Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            var inputStream = GetInputStreamAt(0);
            return inputStream.ReadAsync(buffer, count, options);
        }

        /// <inheritdoc />
        Windows.Foundation.IAsyncOperation<bool> IOutputStream.FlushAsync()
        {
            var outputStream = GetOutputStreamAt(0);
            return outputStream.FlushAsync();
        }

        /// <inheritdoc />
        public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            var outputStream = GetOutputStreamAt(0);
            return outputStream.WriteAsync(buffer);
        }
    }
}