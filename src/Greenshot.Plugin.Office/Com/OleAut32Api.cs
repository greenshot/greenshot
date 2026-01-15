// Copyright (C) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;

namespace Greenshot.Plugin.Office.Com
{
    /// <summary>
    /// API for OLEAUT32
    /// </summary>
    public static class OleAut32Api
    {
        /// <summary>
        /// Get the active instance of the com object with the specified GUID
        /// </summary>
        /// <typeparam name="T">Type for the instance</typeparam>
        /// <param name="clsId">Guid</param>
        /// <returns>IDisposableCom of T</returns>
        public static IDisposableCom<T> GetActiveObject<T>(ref Guid clsId)
        {
            if (GetActiveObject(ref clsId, IntPtr.Zero, out object comObject).Succeeded())
            {
                return DisposableCom.Create((T) comObject);
            }

            return null;
        }

        /// <summary>
        /// Get the active instance of the com object with the specified progId
        /// </summary>
        /// <typeparam name="T">Type for the instance</typeparam>
        /// <param name="progId">string</param>
        /// <returns>IDisposableCom of T</returns>
        public static IDisposableCom<T> GetActiveObject<T>(string progId)
        {
            var clsId = Ole32Api.ClassIdFromProgId(progId);
            return GetActiveObject<T>(ref clsId);
        }

        /// <summary>
        /// For more details read <a href="https://docs.microsoft.com/en-gb/windows/desktop/api/oleauto/nf-oleauto-getactiveobject">this</a>
        /// </summary>
        /// <param name="rclsId">The class identifier (CLSID) of the active object from the OLE registration database.</param>
        /// <param name="pvReserved">Reserved for future use. Must be null.</param>
        /// <param name="ppunk">The requested active object.</param>
        /// <returns></returns>
        [DllImport("oleaut32.dll")]
        private static extern HResult GetActiveObject(ref Guid rclsId, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
    }
}