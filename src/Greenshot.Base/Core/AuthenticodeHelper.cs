/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Security.Cryptography.X509Certificates;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The Authenticode signature of a file (WinVerifyTrust), without the revocation check: that can go online and take 200 ms or more
    /// </summary>
    public static class AuthenticodeHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuthenticodeHelper));

        /// <summary>
        /// The signer's name when the file has a valid Authenticode signature, otherwise null.
        /// </summary>
        public static string GetVerifiedSigner(string filePath)
        {
            if (!VerifyAuthenticode(filePath))
            {
                return null;
            }

            return GetSignerName(filePath);
        }

        /// <summary>
        /// The name of the signer of the file, without checking the signature; null when it has none
        /// </summary>
        public static string GetSignerName(string filePath)
        {
            try
            {
                using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
                return certificate.GetNameInfo(X509NameType.SimpleName, false);
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not read the signer of {filePath}", ex);
                return null;
            }
        }

        /// <summary>
        /// The thumbprint of the signer's certificate, without checking the signature; null when the file has none
        /// </summary>
        public static string GetSignerThumbprint(string filePath)
        {
            try
            {
                using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
                return certificate.Thumbprint;
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not read the signer of {filePath}", ex);
                return null;
            }
        }

        /// <summary>
        /// True when the file has a valid Authenticode signature
        /// </summary>
        public static bool VerifyAuthenticode(string filePath)
        {
            IntPtr fileInfoPointer = IntPtr.Zero;
            IntPtr dataPointer = IntPtr.Zero;
            var data = new WinTrustData();
            try
            {
                var fileInfo = new WinTrustFileInfo
                {
                    cbStruct = (uint)Marshal.SizeOf<WinTrustFileInfo>(),
                    pcwszFilePath = filePath
                };
                fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustFileInfo>());
                Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);

                data.cbStruct = (uint)Marshal.SizeOf<WinTrustData>();
                data.dwUIChoice = WtdUiNone;
                data.fdwRevocationChecks = WtdRevokeNone;
                data.dwUnionChoice = WtdChoiceFile;
                data.pFile = fileInfoPointer;
                data.dwStateAction = WtdStateActionVerify;
                data.dwProvFlags = WtdCacheOnlyUrlRetrieval;
                dataPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WinTrustData>());
                Marshal.StructureToPtr(data, dataPointer, false);

                var action = WinTrustActionGenericVerifyV2;
                int result = WinVerifyTrust(IntPtr.Zero, ref action, dataPointer);

                // Release the state data
                var verified = Marshal.PtrToStructure<WinTrustData>(dataPointer);
                verified.dwStateAction = WtdStateActionClose;
                Marshal.StructureToPtr(verified, dataPointer, true);
                WinVerifyTrust(IntPtr.Zero, ref action, dataPointer);
                return result == 0;
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not verify the signature of {filePath}", ex);
                return false;
            }
            finally
            {
                if (dataPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(dataPointer);
                }
                if (fileInfoPointer != IntPtr.Zero)
                {
                    Marshal.DestroyStructure<WinTrustFileInfo>(fileInfoPointer);
                    Marshal.FreeHGlobal(fileInfoPointer);
                }
            }
        }

        private const uint WtdUiNone = 2;
        private const uint WtdRevokeNone = 0;
        private const uint WtdChoiceFile = 1;
        private const uint WtdStateActionVerify = 1;
        private const uint WtdStateActionClose = 2;
        private const uint WtdCacheOnlyUrlRetrieval = 0x1000;
        private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WinTrustFileInfo
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WinTrustData
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid actionId, IntPtr data);
    }
}
