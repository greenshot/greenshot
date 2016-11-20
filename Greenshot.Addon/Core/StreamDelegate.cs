//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace Greenshot.Addon.Core
{
	/// <summary>
	///     Class that delegates all calls to the supplied Stream
	///     This is used to implement the ProgressStream, which gives progress information on posting / read content
	/// </summary>
	public abstract class StreamDelegate : Stream
	{
		protected StreamDelegate(Stream innerStream)
		{
			if (innerStream == null)
			{
				throw new ArgumentNullException("innerStream");
			}
			InnerStream = innerStream;
		}

		public override bool CanRead
		{
			get { return InnerStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return InnerStream.CanSeek; }
		}

		public override bool CanTimeout
		{
			get { return InnerStream.CanTimeout; }
		}

		public override bool CanWrite
		{
			get { return InnerStream.CanWrite; }
		}

		protected Stream InnerStream { get; }

		public override long Length
		{
			get { return InnerStream.Length; }
		}

		public override long Position
		{
			get { return InnerStream.Position; }
			set { InnerStream.Position = value; }
		}

		public override int ReadTimeout
		{
			get { return InnerStream.ReadTimeout; }
			set { InnerStream.ReadTimeout = value; }
		}

		public override int WriteTimeout
		{
			get { return InnerStream.WriteTimeout; }
			set { InnerStream.WriteTimeout = value; }
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return InnerStream.BeginRead(buffer, offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return InnerStream.BeginWrite(buffer, offset, count, callback, state);
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			return InnerStream.CopyToAsync(destination, bufferSize, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				InnerStream.Dispose();
			}
			base.Dispose(disposing);
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			return InnerStream.EndRead(asyncResult);
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			InnerStream.EndWrite(asyncResult);
		}

		public override void Flush()
		{
			InnerStream.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return InnerStream.FlushAsync(cancellationToken);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return InnerStream.Read(buffer, offset, count);
		}

		public override int ReadByte()
		{
			return InnerStream.ReadByte();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return InnerStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			InnerStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			InnerStream.Write(buffer, offset, count);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override void WriteByte(byte value)
		{
			InnerStream.WriteByte(value);
		}
	}
}