using System.IO;
using Windows.Storage.Streams;

namespace GreenshotWin10Plugin
{
	public sealed class MemoryRandomAccessStream : MemoryStream, IRandomAccessStream
	{
		public MemoryRandomAccessStream()
		{
		}

		public MemoryRandomAccessStream(byte[] bytes)
		{
			Write(bytes, 0, bytes.Length);
		}

		public IInputStream GetInputStreamAt(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);

			return this.AsInputStream();
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);

			return this.AsOutputStream();
		}

		ulong IRandomAccessStream.Position => (ulong)Position;

		public ulong Size
		{
			get { return (ulong)Length; }
			set { SetLength((long)value); }
		}

		public IRandomAccessStream CloneStream()
		{
			var cloned = new MemoryRandomAccessStream();
			CopyTo(cloned);
			return cloned;
		}

		public void Seek(ulong position)
		{
			Seek((long)position, SeekOrigin.Begin);
		}

		public Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			var inputStream = GetInputStreamAt(0);
			return inputStream.ReadAsync(buffer, count, options);
		}

		Windows.Foundation.IAsyncOperation<bool> IOutputStream.FlushAsync()
		{
			var outputStream = GetOutputStreamAt(0);
			return outputStream.FlushAsync();
		}

		public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			var outputStream = GetOutputStreamAt(0);
			return outputStream.WriteAsync(buffer);
		}
	}
}
