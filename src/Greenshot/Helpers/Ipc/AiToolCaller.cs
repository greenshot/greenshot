/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dapplo.Ini;
using Greenshot.Base.Core;
using log4net;
using Microsoft.Win32.SafeHandles;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// The program which uses Greenshot through greenshot-mcp.exe (e.g. Claude, VS Code), identified by Greenshot itself.
    /// </summary>
    public sealed class AiToolClient
    {
        /// <summary>
        /// Full path of the program's executable, this is what the user allows (AiToolsAllowedClients).
        /// </summary>
        public string ExePath { get; set; }

        /// <summary>
        /// Name to show the user (file description or product name of the executable)
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The verified Authenticode signer of the executable, null when it is not signed (or the signature is invalid)
        /// </summary>
        public string Signer { get; set; }

        public override string ToString() => $"{DisplayName} ({ExePath})";
    }

    /// <summary>
    /// Identifies who is on the other end of a named pipe connection with source "mcp", instead of trusting the HELLO frame:
    /// the client process must be greenshot-mcp.exe from Greenshot's own directory (or a path the user configured in
    /// AiToolsMcpServerPaths), and the AI tool is the program which started it (shells like cmd.exe in between are skipped).
    /// </summary>
    public static class AiToolCaller
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolCaller));

        public const string McpServerFileName = "greenshot-mcp.exe";

        /// <summary>
        /// Programs which only launch the AI tool's command, the AI tool is their parent
        /// </summary>
        private static readonly HashSet<string> LauncherProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cmd.exe", "powershell.exe", "pwsh.exe"
        };

        private const int MaxLauncherDepth = 5;

        /// <summary>
        /// Identifies the AI tool behind the pipe connection.
        /// </summary>
        /// <param name="pipe">Connected server stream</param>
        /// <param name="client">The AI tool, when the connection is trusted</param>
        /// <param name="error">Why the connection is not trusted</param>
        public static bool TryIdentify(NamedPipeServerStream pipe, out AiToolClient client, out string error)
        {
            client = null;
            if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint serverProcessId))
            {
                error = $"Could not get the process of the connection (error {Marshal.GetLastWin32Error()}).";
                return false;
            }

            string serverPath = GetProcessPath(serverProcessId);
            var configuration = IniConfigRegistry.GetSection<ICoreConfiguration>();
            if (!IsTrustedMcpServer(serverPath, AppDomain.CurrentDomain.BaseDirectory, configuration?.AiToolsMcpServerPaths))
            {
                error = $"Only {McpServerFileName} from Greenshot's directory may connect as an AI tool, not '{serverPath ?? "unknown"}'.";
                return false;
            }

            string clientPath = FindAiToolPath(serverProcessId);
            if (clientPath == null)
            {
                error = $"Could not identify the program which started {McpServerFileName}.";
                return false;
            }

            client = Describe(clientPath);
            error = null;
            return true;
        }

        /// <summary>
        /// True when the path is greenshot-mcp.exe in Greenshot's directory, or one of the additionally allowed paths
        /// (the executable itself or its directory).
        /// </summary>
        internal static bool IsTrustedMcpServer(string serverPath, string greenshotDirectory, IEnumerable<string> additionalPaths)
        {
            if (string.IsNullOrWhiteSpace(serverPath) ||
                !string.Equals(Path.GetFileName(serverPath), McpServerFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string serverDirectory = NormalizeDirectory(Path.GetDirectoryName(serverPath));
            if (string.Equals(serverDirectory, NormalizeDirectory(greenshotDirectory), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (string additionalPath in additionalPaths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(additionalPath))
                {
                    continue;
                }
                string allowed = additionalPath.Trim().Trim('"');
                if (string.Equals(allowed, serverPath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeDirectory(allowed), serverDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// True for programs which only launch the AI tool's command (cmd.exe etc.)
        /// </summary>
        internal static bool IsLauncher(string exePath)
        {
            return !string.IsNullOrEmpty(exePath) && LauncherProcesses.Contains(Path.GetFileName(exePath));
        }

        private static string NormalizeDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return string.Empty;
            }
            try
            {
                return Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception)
            {
                return directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        /// <summary>
        /// The executable of the program which started greenshot-mcp.exe, skipping launchers like cmd.exe.
        /// </summary>
        private static string FindAiToolPath(uint serverProcessId)
        {
            uint childId = serverProcessId;
            for (int depth = 0; depth <= MaxLauncherDepth; depth++)
            {
                if (!TryGetParent(childId, out uint parentId))
                {
                    return null;
                }
                string parentPath = GetProcessPath(parentId);
                if (parentPath == null)
                {
                    return null;
                }
                if (!IsLauncher(parentPath))
                {
                    return parentPath;
                }
                childId = parentId;
            }
            return null;
        }

        /// <summary>
        /// The parent process; only when it was started before the child (a parent id can be reused after the parent exited).
        /// </summary>
        private static bool TryGetParent(uint processId, out uint parentId)
        {
            parentId = 0;
            using var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (process.IsInvalid)
            {
                return false;
            }
            var info = new ProcessBasicInformation();
            if (NtQueryInformationProcess(process, 0, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _) != 0)
            {
                return false;
            }
            parentId = (uint)info.InheritedFromUniqueProcessId.ToInt64();
            if (parentId == 0)
            {
                return false;
            }

            using var parent = OpenProcess(ProcessQueryLimitedInformation, false, parentId);
            if (parent.IsInvalid ||
                !GetProcessTimes(process, out long childStart, out _, out _, out _) ||
                !GetProcessTimes(parent, out long parentStart, out _, out _, out _))
            {
                return false;
            }
            return parentStart <= childStart;
        }

        private static string GetProcessPath(uint processId)
        {
            using var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (process.IsInvalid)
            {
                return null;
            }
            var buffer = new StringBuilder(1024);
            int size = buffer.Capacity;
            return QueryFullProcessImageName(process, 0, buffer, ref size) ? buffer.ToString(0, size) : null;
        }

        /// <summary>
        /// Name and verified signer of the executable, for the consent question.
        /// </summary>
        internal static AiToolClient Describe(string exePath)
        {
            string displayName = null;
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                displayName = !string.IsNullOrWhiteSpace(versionInfo.FileDescription) ? versionInfo.FileDescription : versionInfo.ProductName;
            }
            catch (Exception ex)
            {
                Log.Debug($"No version information for {exePath}", ex);
            }

            return new AiToolClient
            {
                ExePath = exePath,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(exePath) : displayName.Trim(),
                Signer = GetVerifiedSigner(exePath)
            };
        }

        /// <summary>
        /// The signer's name when the file has a valid Authenticode signature, otherwise null.
        /// </summary>
        private static string GetVerifiedSigner(string filePath)
        {
            if (!VerifyAuthenticode(filePath))
            {
                return null;
            }
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

        private static bool VerifyAuthenticode(string filePath)
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

        private const uint ProcessQueryLimitedInformation = 0x1000;
        private const uint WtdUiNone = 2;
        private const uint WtdRevokeNone = 0;
        private const uint WtdChoiceFile = 1;
        private const uint WtdStateActionVerify = 1;
        private const uint WtdStateActionClose = 2;
        private const uint WtdCacheOnlyUrlRetrieval = 0x1000;
        private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessBasicInformation
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

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

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder exeName, ref int size);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessTimes(SafeProcessHandle process, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(SafeProcessHandle process, int processInformationClass, ref ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid actionId, IntPtr data);
    }
}
