/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Use the progress stream if you want to have the progress reported.
	/// don't forget to specify the TotalBytesToReceive and/or TotalBytesToSend!
	/// </summary>
	public class ProgressStream : StreamDelegate
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ProgressStream));
		private readonly IProgress<int> _progress;
		private long _bytesReceived;
		private long _bytesSent;

		/// <summary>
		/// Specify the bytes that the other side needs to receive
		/// </summary>
		public long? TotalBytesToReceive
		{
			get;
			set;
		}

		/// <summary>
		/// Specify the bytes that the other side needs to send
		/// </summary>
		public long? TotalBytesToSend
		{
			get;
			set;
		}

		public ProgressStream(Stream innerStream, IProgress<int> progress) : base(innerStream)
		{
			_progress = progress;
			TotalBytesToReceive = innerStream.Length;
			TotalBytesToSend = innerStream.Length;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int bytesRead = InnerStream.Read(buffer, offset, count);
			ReportBytesReceived(bytesRead);
			return bytesRead;
		}

		public override int ReadByte()
		{
			int byteRead = InnerStream.ReadByte();
			ReportBytesReceived(byteRead == -1 ? 0 : 1);
			return byteRead;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			int readCount = await InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
			ReportBytesReceived(readCount);
			return readCount;
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			// Report the bytes read in a new callback, wrapping the passed one.
			var wrappedCallback = new AsyncCallback((result) =>
			{
				ReportBytesReceived(count);
				if (callback != null)
				{
					callback.Invoke(result);
				}
			});
			return InnerStream.BeginRead(buffer, offset, count, wrappedCallback, state);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			InnerStream.Write(buffer, offset, count);
			ReportBytesSent(count);
		}

		public override void WriteByte(byte value)
		{
			InnerStream.WriteByte(value);
			ReportBytesSent(1);
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			await InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
			// Report the bytes send
			ReportBytesSent(count);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			// Report the bytes read in a new callback, wrapping the passed one.
			var wrappedCallback = new AsyncCallback((result) =>
			{
				ReportBytesSent(count);
				if(callback != null)
				{
					callback.Invoke(result);
				}
			});
			return base.BeginWrite(buffer, offset, count, wrappedCallback, state);
		}

		/// <summary>
		/// Report the bytes send to the IProgress
		/// </summary>
		/// <param name="bytesSent"></param>
		internal void ReportBytesSent(int bytesSent)
		{
			if (bytesSent > 0)
			{
				_bytesSent += bytesSent;
				int percentage = 0;
				if (TotalBytesToSend.HasValue && TotalBytesToSend != 0)
				{
					percentage = (int)((100L * _bytesSent) / TotalBytesToSend);
				}
				LOG.DebugFormat("Write progress at {0} bytes = {1}", _bytesSent, percentage);

				_progress.Report(percentage);
			}
		}

		/// <summary>
		/// Report the bytes received to the IProgress
		/// </summary>
		/// <param name="bytesSent"></param>
		private void ReportBytesReceived(int bytesReceived)
		{
			if (bytesReceived > 0)
			{
				_bytesReceived += bytesReceived;
				int percentage = 0;
				if (TotalBytesToReceive.HasValue && TotalBytesToReceive != 0)
				{
					percentage = (int)((100L * _bytesReceived) / TotalBytesToReceive);
				}
				LOG.DebugFormat("Read progress at {0} bytes = {1}", _bytesReceived, percentage);
				_progress.Report(percentage);
			}
		}
	}
}
