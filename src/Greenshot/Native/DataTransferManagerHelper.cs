/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Runtime.InteropServices;

namespace Greenshot.Native
{
    /// <summary>
    /// Wraps the interop for calling the ShareUI
    /// </summary>
    public class DataTransferManagerHelper
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DataTransferManagerHelper));

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        private static extern int RoGetActivationFactory(
            [MarshalAs(UnmanagedType.HString)] string activatableClassId,
            [In] ref Guid iid,
            out IntPtr factory);

        public static IDataTransferManagerInterop GetInteropFactory(string className)
        {
            // Interface for the Activation Factory: IActivationFactory
            Guid iidFactory = new Guid("00000035-0000-0000-C000-000000000046"); ;
            int hr = RoGetActivationFactory(className, ref iidFactory, out IntPtr pFactory);
            if (hr < 0) throw Marshal.GetExceptionForHR(hr);

            try
            {
                // Interface for the Factory: IDataTransferManagerInterop
                Guid iidInterop = new Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8");
                IntPtr pInterop;
                int qiHr = Marshal.QueryInterface(pFactory, ref iidInterop, out pInterop);
                if (qiHr < 0) throw new InvalidCastException("Failed to QI factory for Interop interface.");

                return (IDataTransferManagerInterop)Marshal.GetTypedObjectForIUnknown(pInterop, typeof(IDataTransferManagerInterop));
            }
            finally
            {
                if (pFactory != IntPtr.Zero) Marshal.Release(pFactory);
            }
        }
    }
}