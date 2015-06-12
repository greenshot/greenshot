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
using System.Runtime.InteropServices;

namespace Greenshot.Interop {
	/// <summary>
	/// A simple com wrapper which helps with "using"
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IDisposableCom<T> : IDisposable {
		T ComObject {
			get;
			set;
		}
	}

	/// <summary>
	/// A factory for IDisposableCom
	/// </summary>
	/// <typeparam name="T">Type to wrap</typeparam>
	public static class DisposableCom {
		/// <summary>
		/// Create a ComDisposable for the supplied type object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static IDisposableCom<T> Create<T>(T obj) {
			if (obj != null) {
				return new DisposableComImplementation<T>(obj);
			}
			return null;
		}
	}


	internal class DisposableComImplementation<T> : IDisposableCom<T> {
		public DisposableComImplementation(T obj) {
			ComObject = obj;
		}

		public T ComObject {
			get;
			set;
		}

		public static IDisposableCom<U> Create<U>(U obj) {
			if (obj != null) {
				return new DisposableComImplementation<U>(obj);
			}
			return null;
		}

		/// <summary>
		/// Cleans up the COM object.
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Release the COM reference
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if this was called from the<see cref="IDisposable"/> interface.</param>
		private void Dispose(bool disposing) {
			if (true) {
				// Do not catch an exception from this.
				// You may want to remove these guards depending on
				// what you think the semantics should be.
				if (ComObject != null && Marshal.IsComObject(ComObject)) {
					Marshal.ReleaseComObject(ComObject);
				}
				ComObject = default(T);
			}
		}
	}
}
