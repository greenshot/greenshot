using System.IO;
using Windows.Storage.Streams;

namespace Greenshot.Addon.Win10
{
    /// <summary>
    /// This is an IRandomAccessStream implementation which uses a MemoryStream
    /// </summary>
	public sealed class MemoryRandomAccessStream : MemoryStream, IRandomAccessStream
	{
        /// <summary>
        /// Default constructor
        /// </summary>
		public MemoryRandomAccessStream()
		{
		}

        /// <summary>
        /// Constructor where also bytes are already passed
        /// </summary>
        /// <param name="bytes">byte array</param>
		public MemoryRandomAccessStream(byte[] bytes)
		{
			Write(bytes, 0, bytes.Length);
		}

        /// <inheritdoc />
        public IInputStream GetInputStreamAt(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);

			return this.AsInputStream();
		}

        /// <inheritdoc />
        public IOutputStream GetOutputStreamAt(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);

			return this.AsOutputStream();
		}

        /// <inheritdoc />
        ulong IRandomAccessStream.Position => (ulong)Position;

        /// <inheritdoc />
        public ulong Size
		{
			get { return (ulong)Length; }
			set { SetLength((long)value); }
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
			Seek((long)position, SeekOrigin.Begin);
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
