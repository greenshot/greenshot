// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;

namespace Greenshot.Plugin.Office.Com
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
            return CLSIDFromProgID(programId, out Guid clsId).Succeeded() ? clsId : clsId;
        }

        /// <summary>
        /// See more <a href="https://docs.microsoft.com/en-us/windows/desktop/api/combaseapi/nf-combaseapi-clsidfromprogid">here</a>
        /// </summary>
        /// <param name="progId">string with the progId</param>
        /// <param name="clsId">Guid</param>
        /// <returns>HResult</returns>
        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern HResult CLSIDFromProgID([In][MarshalAs(UnmanagedType.LPWStr)] string progId, [Out] out Guid clsId);
    }
}