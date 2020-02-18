// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using GreenshotPlugin.Core;
using GreenshotPlugin.Core.Enums;

namespace GreenshotOfficePlugin.Com
{
    /// <summary>
    /// This provides an API for OLE32
    /// </summary>
    public static class Ole32Api
    {
        /// <summary>
        /// This converts a ProgID (program ID) into a Guid with the clsId
        /// </summary>
        /// <param name="programId">string with the program ID</param>
        /// <returns>Guid with the clsId</returns>
        public static Guid ClassIdFromProgId(string programId)
        {
            if (CLSIDFromProgID(programId, out Guid clsId).Succeeded())
            {
                return clsId;
            }
            return clsId;
        }

        /// <summary>
        /// This converts a clsid (Class ID) into a ProgID (program ID)
        /// </summary>
        /// <param name="clsId">Guid with the clsid (Class ID)</param>
        /// <returns>string with the progid</returns>
        public static string ProgIdFromClassId(Guid clsId)
        {
            if (ProgIDFromCLSID(ref clsId, out string progId).Succeeded())
            {
                return progId;
            }

            return null;
        }

        /// <summary>
        /// See more <a href="https://docs.microsoft.com/en-us/windows/desktop/api/combaseapi/nf-combaseapi-clsidfromprogid">here</a>
        /// </summary>
        /// <param name="progId">string with the progId</param>
        /// <param name="clsId">Guid</param>
        /// <returns>HResult</returns>
        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern HResult CLSIDFromProgID([In] [MarshalAs(UnmanagedType.LPWStr)] string progId, [Out] out Guid clsId);

        /// <summary>
        /// See more <a href="https://docs.microsoft.com/en-us/windows/desktop/api/combaseapi/nf-combaseapi-progidfromclsid">here</a>
        /// </summary>
        /// <param name="clsId">Guid The CLSID for which the ProgID is to be requested.</param>
        /// <param name="lplpszProgId">string the ProgID string. The string that represents clsid includes enclosing braces.</param>
        /// <returns>HResult</returns>
        [DllImport("ole32.dll")]
        private static extern HResult ProgIDFromCLSID([In] ref Guid clsId, [MarshalAs(UnmanagedType.LPWStr)] out string lplpszProgId);
    }
}
