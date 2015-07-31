/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Class that delegates all calls to the supplied Stream
	/// This is used to implement the ProgressStream, which gives progress information on posting / read content
	/// </summary>
	public abstract class StreamDelegate : Stream {
		private Stream _innerStream;

		protected StreamDelegate(Stream innerStream) {
			if (innerStream == null) {
				throw new ArgumentNullException("innerStream");
			}
			_innerStream = innerStream;
		}

		protected Stream InnerStream {
			get { return _innerStream; }
		}

		public override bool CanRead {
			get { return _innerStream.CanRead; }
		}

		public override bool CanSeek {
			get { return _innerStream.CanSeek; }
		}

		public override bool CanWrite {
			get { return _innerStream.CanWrite; }
		}

		public override long Length {
			get { return _innerStream.Length; }
		}

		public override long Position {
			get { return _innerStream.Position; }
			set { _innerStream.Position = value; }
		}

		public override int ReadTimeout {
			get { return _innerStream.ReadTimeout; }
			set { _innerStream.ReadTimeout = value; }
		}

		public override bool CanTimeout {
			get { return _innerStream.CanTimeout; }
		}

		public override int WriteTimeout {
			get { return _innerStream.WriteTimeout; }
			set { _innerStream.WriteTimeout = value; }
		}

		protected override void Dispose(bool disposing) {
			if (disposing) {
				_innerStream.Dispose();
			}
			base.Dispose(disposing);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return _innerStream.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return _innerStream.Read(buffer, offset, count);
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			return _innerStream.BeginRead(buffer, offset, count, callback, state);
		}

		public override int EndRead(IAsyncResult asyncResult) {
			return _innerStream.EndRead(asyncResult);
		}

		public override int ReadByte() {
			return _innerStream.ReadByte();
		}

		public override void Flush() {
			_innerStream.Flush();
		}

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken);
        }

		public override void SetLength(long value) {
			_innerStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			_innerStream.Write(buffer, offset, count);
		}

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
			return _innerStream.BeginWrite(buffer, offset, count, callback, state);
		}

		public override void EndWrite(IAsyncResult asyncResult) {
			_innerStream.EndWrite(asyncResult);
		}

		public override void WriteByte(byte value) {
			_innerStream.WriteByte(value);
		}
	}
}