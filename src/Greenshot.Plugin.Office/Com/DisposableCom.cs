// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Greenshot.Plugin.Office.Com
{
    /// <summary>
    ///     A factory for IDisposableCom
    /// </summary>
    public static class DisposableCom
    {
        /// <summary>
        ///     Create a ComDisposable for the supplied type object
        /// </summary>
        /// <typeparam name="T">Type for the com object</typeparam>
        /// <param name="comObject">the com object itself</param>
        /// <returns>IDisposableCom of type T</returns>
        public static IDisposableCom<T> Create<T>(T comObject) => Equals(comObject, default(T)) ? null : (IDisposableCom<T>)new DisposableComImplementation<T>(comObject);
    }
}