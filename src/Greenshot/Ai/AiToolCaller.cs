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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Ipc;
using log4net;
using Microsoft.Win32.SafeHandles;

namespace Greenshot.Ai
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

        /// <summary>
        /// The process id of greenshot-mcp.exe: one MCP session of the AI tool (window ids from list_windows belong to it)
        /// </summary>
        public uint ServerProcessId { get; set; }

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
            if (!PipeClientProcess.TryGetClientProcess(pipe, out uint serverProcessId, out string serverPath))
            {
                error = $"Could not get the process of the connection (error {Marshal.GetLastWin32Error()}).";
                return false;
            }

            if (!IsTrustedMcpServer(serverPath, AppDomain.CurrentDomain.BaseDirectory, GetAdditionalMcpServerPaths()))
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
            client.ServerProcessId = serverProcessId;
            error = null;
            return true;
        }

        /// <summary>
        /// AiToolsMcpServerPaths, for development builds only: in a release build the ini file must not be able to add a server location.
        /// </summary>
        internal static IEnumerable<string> GetAdditionalMcpServerPaths()
        {
#if DEBUG
            return IniConfigRegistry.GetSection<ICoreConfiguration>()?.AiToolsMcpServerPaths;
#else
            return null;
#endif
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

            string serverDirectory = PipeClientProcess.NormalizeDirectory(Path.GetDirectoryName(serverPath));
            if (string.Equals(serverDirectory, PipeClientProcess.NormalizeDirectory(greenshotDirectory), StringComparison.OrdinalIgnoreCase))
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
                    string.Equals(PipeClientProcess.NormalizeDirectory(allowed), serverDirectory, StringComparison.OrdinalIgnoreCase))
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
                string parentPath = PipeClientProcess.GetProcessPath(parentId);
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
            using var process = PipeClientProcess.OpenProcess(PipeClientProcess.ProcessQueryLimitedInformation, false, processId);
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

            using var parent = PipeClientProcess.OpenProcess(PipeClientProcess.ProcessQueryLimitedInformation, false, parentId);
            if (parent.IsInvalid ||
                !GetProcessTimes(process, out long childStart, out _, out _, out _) ||
                !GetProcessTimes(parent, out long parentStart, out _, out _, out _))
            {
                return false;
            }
            return parentStart <= childStart;
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

            // A helper without a name of its own (like Antigravity's resources\bin\language_server.exe) is named after its application.
            // Only the name: the program which is allowed stays the helper itself.
            if (string.IsNullOrWhiteSpace(displayName) || string.Equals(displayName.Trim(), Path.GetFileNameWithoutExtension(exePath), StringComparison.OrdinalIgnoreCase))
            {
                string application = FindApplicationName(exePath);
                if (application != null)
                {
                    displayName = $"{application} ({Path.GetFileNameWithoutExtension(exePath)})";
                }
            }

            return new AiToolClient
            {
                ExePath = exePath,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? Path.GetFileNameWithoutExtension(exePath) : displayName.Trim(),
                Signer = AuthenticodeHelper.GetVerifiedSigner(exePath)
            };
        }

        /// <summary>
        /// The name of the application a helper executable belongs to: up to three directories above it, an executable named like
        /// its directory (…\Antigravity\resources\bin\language_server.exe → …\Antigravity\Antigravity.exe), null when there is none.
        /// </summary>
        internal static string FindApplicationName(string exePath)
        {
            try
            {
                var directory = new DirectoryInfo(Path.GetDirectoryName(exePath) ?? string.Empty);
                for (int level = 0; level < 4 && directory != null; level++, directory = directory.Parent)
                {
                    string candidate = Path.Combine(directory.FullName, directory.Name + ".exe");
                    if (string.Equals(candidate, exePath, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate))
                    {
                        continue;
                    }
                    var versionInfo = FileVersionInfo.GetVersionInfo(candidate);
                    string name = !string.IsNullOrWhiteSpace(versionInfo.ProductName) ? versionInfo.ProductName : versionInfo.FileDescription;
                    return string.IsNullOrWhiteSpace(name) ? directory.Name : name.Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"No application found for {exePath}", ex);
            }
            return null;
        }

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

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessTimes(SafeProcessHandle process, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(SafeProcessHandle process, int processInformationClass, ref ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

    }
}
