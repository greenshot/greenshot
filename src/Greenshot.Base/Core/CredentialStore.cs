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
using System.Text;
using Greenshot.Base.Http;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Generic credentials in the Windows Credential Manager, by target name. The target is the url of the server,
    /// like the credentials dialog of Windows (CredUIPromptForCredentials) saved them, so credentials saved by
    /// older Greenshot versions are found too.
    /// </summary>
    public static class CredentialStore
    {
        private const int CredTypeGeneric = 1;
        private const int CredPersistLocalMachine = 2;
        private const int ErrorNotFound = 1168;

        /// <summary>
        /// The credentials stored for the target, null when there are none
        /// </summary>
        public static ServerCredentials Read(string target)
        {
            if (string.IsNullOrEmpty(target) || !CredRead(target, CredTypeGeneric, 0, out var credentialPointer))
            {
                return null;
            }

            try
            {
                var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
                string userName = credential.UserName == IntPtr.Zero ? null : Marshal.PtrToStringUni(credential.UserName);
                string secret = credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0
                    ? string.Empty
                    : Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
                return new ServerCredentials(userName, secret);
            }
            finally
            {
                CredFree(credentialPointer);
            }
        }

        /// <summary>
        /// Store the credentials for the target, replaces what was stored
        /// </summary>
        /// <returns>false when Windows didn't store them</returns>
        public static bool Write(string target, ServerCredentials credentials)
        {
            if (string.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));
            if (credentials == null) throw new ArgumentNullException(nameof(credentials));

            byte[] secret = Encoding.Unicode.GetBytes(credentials.Secret);
            var targetName = Marshal.StringToCoTaskMemUni(target);
            var userName = credentials.UserName == null ? IntPtr.Zero : Marshal.StringToCoTaskMemUni(credentials.UserName);
            var blob = Marshal.AllocCoTaskMem(Math.Max(1, secret.Length));
            try
            {
                Marshal.Copy(secret, 0, blob, secret.Length);
                var credential = new NativeCredential
                {
                    Type = CredTypeGeneric,
                    TargetName = targetName,
                    CredentialBlobSize = secret.Length,
                    CredentialBlob = blob,
                    Persist = CredPersistLocalMachine,
                    UserName = userName
                };
                return CredWrite(ref credential, 0);
            }
            finally
            {
                // Don't leave the secret in freed memory
                Marshal.Copy(new byte[secret.Length], 0, blob, secret.Length);
                Marshal.FreeCoTaskMem(blob);
                Marshal.FreeCoTaskMem(targetName);
                if (userName != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(userName);
                }
            }
        }

        /// <summary>
        /// Remove the credentials of the target
        /// </summary>
        /// <returns>true when they were removed or there were none</returns>
        public static bool Delete(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return true;
            }

            return CredDelete(target, CredTypeGeneric, 0) || Marshal.GetLastWin32Error() == ErrorNotFound;
        }

        /// <summary>
        /// CREDENTIALW, see https://learn.microsoft.com/windows/win32/api/wincred/ns-wincred-credentialw
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeCredential
        {
            public int Flags;
            public int Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public long LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credential);

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite(ref NativeCredential credential, int flags);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredDelete(string target, int type, int flags);

        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr buffer);
    }
}
