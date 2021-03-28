// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Greenshot.Plugin.Office.Com
{
    /// <summary>
    ///     Implementation of the IDisposableCom, this is internal to prevent other code to use it directly
    /// </summary>
    /// <typeparam name="T">Type of the com object</typeparam>
    internal class DisposableComImplementation<T> : IDisposableCom<T>
    {
        public DisposableComImplementation(T obj)
        {
            ComObject = obj;
        }

        public T ComObject { get; private set; }

        /// <summary>
        ///     Cleans up the COM object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Release the COM reference
        /// </summary>
        /// <param name="disposing"><see langword="true" /> if this was called from the<see cref="IDisposable" /> interface.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            // Do not catch an exception from this.
            // You may want to remove these guards depending on
            // what you think the semantics should be.
            if (!Equals(ComObject, default(T)) && Marshal.IsComObject(ComObject))
            {
                Marshal.ReleaseComObject(ComObject);
            }
            ComObject = default;
        }
    }
}