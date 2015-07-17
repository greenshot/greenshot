/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// A simple class to make it possible to lock a resource while waiting
	/// </summary>
	public class AsyncLock {
		private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

		public async Task<IDisposable> LockAsync() {
			await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
			return new Releaser(_semaphoreSlim);
		}

		internal struct Releaser : IDisposable {
			private readonly SemaphoreSlim _semaphoreSlim;

			public Releaser(SemaphoreSlim semaphoreSlim) {
				_semaphoreSlim = semaphoreSlim;
			}
			public void Dispose() {
				_semaphoreSlim.Release();
			}
		}
	}
}
