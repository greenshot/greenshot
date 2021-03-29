/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Greenshot.Base.IniFile;
using Greenshot.Base.UnmanagedHelpers;
using Microsoft.Win32;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of EnvironmentInfo.
    /// </summary>
    public static class EnvironmentInfo
    {
        private static bool? _isWindows;

        public static bool IsWindows
        {
            get
            {
                if (_isWindows.HasValue)
                {
                    return _isWindows.Value;
                }

                _isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
                return _isWindows.Value;
            }
        }

        public static bool IsNet45OrNewer()
        {
            // Class "ReflectionContext" exists from .NET 4.5 onwards.
            return Type.GetType("System.Reflection.ReflectionContext", false) != null;
        }

        public static string GetGreenshotVersion(bool shortVersion = false)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            // Use assembly version
            string greenshotVersion = executingAssembly.GetName().Version.ToString();

            // Use AssemblyFileVersion if available
            var assemblyFileVersionAttribute = executingAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (!string.IsNullOrEmpty(assemblyFileVersionAttribute?.Version))
            {
                var assemblyFileVersion = new Version(assemblyFileVersionAttribute.Version);
                greenshotVersion = assemblyFileVersion.ToString(3);
            }

            if (!shortVersion)
            {
                // Use AssemblyInformationalVersion if available
                var informationalVersionAttribute = executingAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (!string.IsNullOrEmpty(informationalVersionAttribute?.InformationalVersion))
                {
                    greenshotVersion = informationalVersionAttribute.InformationalVersion;
                }
            }

            return greenshotVersion.Replace("+", " - ");
        }

        public static string EnvironmentToString(bool newline)
        {
            StringBuilder environment = new StringBuilder();
            environment.Append("Software version: " + GetGreenshotVersion());
            if (IniConfig.IsPortable)
            {
                environment.Append(" Portable");
            }

            environment.Append(" (" + OsInfo.Bits + " bit)");

            if (newline)
            {
                environment.AppendLine();
            }
            else
            {
                environment.Append(", ");
            }

            environment.Append(".NET runtime version: " + Environment.Version);
            if (IsNet45OrNewer())
            {
                environment.Append("+");
            }

            if (newline)
            {
                environment.AppendLine();
            }
            else
            {
                environment.Append(", ");
            }

            environment.Append("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));

            if (IsWindows)
            {
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                environment.Append($"OS: {OsInfo.Name}");
                if (!string.IsNullOrEmpty(OsInfo.Edition))
                {
                    environment.Append($" {OsInfo.Edition}");
                }

                if (!string.IsNullOrEmpty(OsInfo.ServicePack))
                {
                    environment.Append($" {OsInfo.ServicePack}");
                }

                environment.Append($" x{OsInfo.Bits}");
                environment.Append($" {OsInfo.VersionString}");
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                // Get some important information for fixing GDI related Problems
                environment.AppendFormat("GDI object count: {0}", User32.GetGuiResourcesGDICount());
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                environment.AppendFormat("User object count: {0}", User32.GetGuiResourcesUserCount());
            }
            else
            {
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                environment.AppendFormat("OS: {0}", Environment.OSVersion.Platform);
            }

            if (newline)
            {
                environment.AppendLine();
            }
            else
            {
                environment.Append(", ");
            }
            // TODO: Is this needed?
            // environment.AppendFormat("Surface count: {0}", Surface.Count);

            return environment.ToString();
        }

        public static string ExceptionToString(Exception ex)
        {
            if (ex == null)
                return "null\r\n";

            StringBuilder report = new StringBuilder();

            report.AppendLine("Exception: " + ex.GetType());
            report.AppendLine("Message: " + ex.Message);
            if (ex.Data.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Additional Information:");
                foreach (object key in ex.Data.Keys)
                {
                    object data = ex.Data[key];
                    if (data != null)
                    {
                        report.AppendLine(key + " : " + data);
                    }
                }
            }

            if (ex is ExternalException externalException)
            {
                // e.g. COMException
                report.AppendLine().AppendLine("ErrorCode: 0x" + externalException.ErrorCode.ToString("X"));
            }

            report.AppendLine().AppendLine("Stack:").AppendLine(ex.StackTrace);

            if (ex is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                report.AppendLine().AppendLine("LoaderExceptions: ");
                foreach (Exception cbE in reflectionTypeLoadException.LoaderExceptions)
                {
                    report.AppendLine(cbE.Message);
                }
            }

            if (ex.InnerException != null)
            {
                report.AppendLine("--- InnerException: ---");
                report.AppendLine(ExceptionToString(ex.InnerException));
            }

            return report.ToString();
        }

        public static string BuildReport(Exception exception)
        {
            StringBuilder exceptionText = new StringBuilder();
            exceptionText.AppendLine(EnvironmentToString(true));
            exceptionText.AppendLine(ExceptionToString(exception));
            exceptionText.AppendLine("Configuration dump:");

            return exceptionText.ToString();
        }
    }

    /// <summary>
    /// Provides detailed information about the host operating system.
    /// Code is available at: https://www.csharp411.com/determine-windows-version-and-edition-with-c/
    /// </summary>
    public static class OsInfo
    {
        /// <summary>
        /// Determines if the current application is 32 or 64-bit.
        /// </summary>
        public static int Bits => IntPtr.Size * 8;

        private static string _sEdition;

        /// <summary>
        /// Gets the edition of the operating system running on this computer.
        /// </summary>
        public static string Edition
        {
            get
            {
                if (_sEdition != null)
                {
                    return _sEdition; //***** RETURN *****//
                }

                string edition = string.Empty;

                OperatingSystem osVersion = Environment.OSVersion;
                OSVERSIONINFOEX osVersionInfo = OSVERSIONINFOEX.Create();

                if (GetVersionEx(ref osVersionInfo))
                {
                    int majorVersion = osVersion.Version.Major;
                    int minorVersion = osVersion.Version.Minor;
                    byte productType = osVersionInfo.ProductType;
                    ushort suiteMask = osVersionInfo.SuiteMask;

                    if (majorVersion == 4)
                    {
                        if (productType == VER_NT_WORKSTATION)
                        {
                            // Windows NT 4.0 Workstation
                            edition = "Workstation";
                        }
                        else if (productType == VER_NT_SERVER)
                        {
                            edition = (suiteMask & VER_SUITE_ENTERPRISE) != 0 ? "Enterprise Server" : "Standard Server";
                        }
                    }

                    else if (majorVersion == 5)
                    {
                        if (productType == VER_NT_WORKSTATION)
                        {
                            if ((suiteMask & VER_SUITE_PERSONAL) != 0)
                            {
                                // Windows XP Home Edition
                                edition = "Home";
                            }
                            else
                            {
                                // Windows XP / Windows 2000 Professional
                                edition = "Professional";
                            }
                        }
                        else if (productType == VER_NT_SERVER)
                        {
                            if (minorVersion == 0)
                            {
                                if ((suiteMask & VER_SUITE_DATACENTER) != 0)
                                {
                                    // Windows 2000 Datacenter Server
                                    edition = "Datacenter Server";
                                }
                                else if ((suiteMask & VER_SUITE_ENTERPRISE) != 0)
                                {
                                    // Windows 2000 Advanced Server
                                    edition = "Advanced Server";
                                }
                                else
                                {
                                    // Windows 2000 Server
                                    edition = "Server";
                                }
                            }
                            else
                            {
                                if ((suiteMask & VER_SUITE_DATACENTER) != 0)
                                {
                                    // Windows Server 2003 Datacenter Edition
                                    edition = "Datacenter";
                                }
                                else if ((suiteMask & VER_SUITE_ENTERPRISE) != 0)
                                {
                                    // Windows Server 2003 Enterprise Edition
                                    edition = "Enterprise";
                                }
                                else if ((suiteMask & VER_SUITE_BLADE) != 0)
                                {
                                    // Windows Server 2003 Web Edition
                                    edition = "Web Edition";
                                }
                                else
                                {
                                    // Windows Server 2003 Standard Edition
                                    edition = "Standard";
                                }
                            }
                        }
                    }

                    else if (majorVersion == 6)
                    {
                        if (GetProductInfo(majorVersion, minorVersion, osVersionInfo.ServicePackMajor, osVersionInfo.ServicePackMinor, out var ed))
                        {
                            switch (ed)
                            {
                                case PRODUCT_BUSINESS:
                                    edition = "Business";
                                    break;
                                case PRODUCT_BUSINESS_N:
                                    edition = "Business N";
                                    break;
                                case PRODUCT_CLUSTER_SERVER:
                                    edition = "HPC Edition";
                                    break;
                                case PRODUCT_DATACENTER_SERVER:
                                    edition = "Datacenter Server";
                                    break;
                                case PRODUCT_DATACENTER_SERVER_CORE:
                                    edition = "Datacenter Server (core installation)";
                                    break;
                                case PRODUCT_ENTERPRISE:
                                    edition = "Enterprise";
                                    break;
                                case PRODUCT_ENTERPRISE_N:
                                    edition = "Enterprise N";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER:
                                    edition = "Enterprise Server";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_CORE:
                                    edition = "Enterprise Server (core installation)";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_CORE_V:
                                    edition = "Enterprise Server without Hyper-V (core installation)";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_IA64:
                                    edition = "Enterprise Server for Itanium-based Systems";
                                    break;
                                case PRODUCT_ENTERPRISE_SERVER_V:
                                    edition = "Enterprise Server without Hyper-V";
                                    break;
                                case PRODUCT_HOME_BASIC:
                                    edition = "Home Basic";
                                    break;
                                case PRODUCT_HOME_BASIC_N:
                                    edition = "Home Basic N";
                                    break;
                                case PRODUCT_HOME_PREMIUM:
                                    edition = "Home Premium";
                                    break;
                                case PRODUCT_HOME_PREMIUM_N:
                                    edition = "Home Premium N";
                                    break;
                                case PRODUCT_HYPERV:
                                    edition = "Microsoft Hyper-V Server";
                                    break;
                                case PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT:
                                    edition = "Windows Essential Business Management Server";
                                    break;
                                case PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING:
                                    edition = "Windows Essential Business Messaging Server";
                                    break;
                                case PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY:
                                    edition = "Windows Essential Business Security Server";
                                    break;
                                case PRODUCT_SERVER_FOR_SMALLBUSINESS:
                                    edition = "Windows Essential Server Solutions";
                                    break;
                                case PRODUCT_SERVER_FOR_SMALLBUSINESS_V:
                                    edition = "Windows Essential Server Solutions without Hyper-V";
                                    break;
                                case PRODUCT_SMALLBUSINESS_SERVER:
                                    edition = "Windows Small Business Server";
                                    break;
                                case PRODUCT_STANDARD_SERVER:
                                    edition = "Standard Server";
                                    break;
                                case PRODUCT_STANDARD_SERVER_CORE:
                                    edition = "Standard Server (core installation)";
                                    break;
                                case PRODUCT_STANDARD_SERVER_CORE_V:
                                    edition = "Standard Server without Hyper-V (core installation)";
                                    break;
                                case PRODUCT_STANDARD_SERVER_V:
                                    edition = "Standard Server without Hyper-V";
                                    break;
                                case PRODUCT_STARTER:
                                    edition = "Starter";
                                    break;
                                case PRODUCT_STORAGE_ENTERPRISE_SERVER:
                                    edition = "Enterprise Storage Server";
                                    break;
                                case PRODUCT_STORAGE_EXPRESS_SERVER:
                                    edition = "Express Storage Server";
                                    break;
                                case PRODUCT_STORAGE_STANDARD_SERVER:
                                    edition = "Standard Storage Server";
                                    break;
                                case PRODUCT_STORAGE_WORKGROUP_SERVER:
                                    edition = "Workgroup Storage Server";
                                    break;
                                case PRODUCT_UNDEFINED:
                                    edition = "Unknown product";
                                    break;
                                case PRODUCT_ULTIMATE:
                                    edition = "Ultimate";
                                    break;
                                case PRODUCT_ULTIMATE_N:
                                    edition = "Ultimate N";
                                    break;
                                case PRODUCT_WEB_SERVER:
                                    edition = "Web Server";
                                    break;
                                case PRODUCT_WEB_SERVER_CORE:
                                    edition = "Web Server (core installation)";
                                    break;
                            }
                        }
                    }
                }

                _sEdition = edition;
                return edition;
            }
        }

        private static string _name;

        /// <summary>
        /// Gets the name of the operating system running on this computer.
        /// </summary>
        public static string Name
        {
            get
            {
                if (_name != null)
                {
                    return _name; //***** RETURN *****//
                }

                string name = "unknown";

                OperatingSystem osVersion = Environment.OSVersion;
                OSVERSIONINFOEX osVersionInfo = OSVERSIONINFOEX.Create();
                if (GetVersionEx(ref osVersionInfo))
                {
                    int majorVersion = osVersion.Version.Major;
                    int minorVersion = osVersion.Version.Minor;
                    byte productType = osVersionInfo.ProductType;
                    ushort suiteMask = osVersionInfo.SuiteMask;
                    switch (osVersion.Platform)
                    {
                        case PlatformID.Win32Windows:
                            if (majorVersion == 4)
                            {
                                string csdVersion = osVersionInfo.ServicePackVersion;
                                switch (minorVersion)
                                {
                                    case 0:
                                        if (csdVersion == "B" || csdVersion == "C")
                                        {
                                            name = "Windows 95 OSR2";
                                        }
                                        else
                                        {
                                            name = "Windows 95";
                                        }

                                        break;
                                    case 10:
                                        name = csdVersion == "A" ? "Windows 98 Second Edition" : "Windows 98";
                                        break;
                                    case 90:
                                        name = "Windows Me";
                                        break;
                                }
                            }

                            break;
                        case PlatformID.Win32NT:
                            switch (majorVersion)
                            {
                                case 3:
                                    name = "Windows NT 3.51";
                                    break;
                                case 4:
                                    switch (productType)
                                    {
                                        case 1:
                                            name = "Windows NT 4.0";
                                            break;
                                        case 3:
                                            name = "Windows NT 4.0 Server";
                                            break;
                                    }

                                    break;
                                case 5:
                                    switch (minorVersion)
                                    {
                                        case 0:
                                            name = "Windows 2000";
                                            break;
                                        case 1:
                                            name = suiteMask switch
                                            {
                                                0x0200 => "Windows XP Professional",
                                                _ => "Windows XP"
                                            };
                                            break;
                                        case 2:
                                            name = suiteMask switch
                                            {
                                                0x0200 => "Windows XP Professional x64",
                                                0x0002 => "Windows Server 2003 Enterprise",
                                                0x0080 => "Windows Server 2003 Data Center",
                                                0x0400 => "Windows Server 2003 Web Edition",
                                                0x8000 => "Windows Home Server",
                                                _ => "Windows Server 2003"
                                            };
                                            break;
                                    }

                                    break;
                                case 6:
                                    switch (minorVersion)
                                    {
                                        case 0:
                                            name = productType switch
                                            {
                                                3 => "Windows Server 2008",
                                                _ => "Windows Vista"
                                            };
                                            break;
                                        case 1:
                                            name = productType switch
                                            {
                                                3 => "Windows Server 2008 R2",
                                                _ => "Windows 7"
                                            };
                                            break;
                                        case 2:
                                            name = "Windows 8";
                                            break;
                                        case 3:
                                            name = "Windows 8.1";
                                            break;
                                    }

                                    break;
                                case 10:
                                    string releaseId = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString();
                                    name = $"Windows 10 {releaseId}";
                                    break;
                            }

                            break;
                    }
                }

                _name = name;
                return name;
            }
        }

        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProductInfo(
            int osMajorVersion,
            int osMinorVersion,
            int spMajorVersion,
            int spMinorVersion,
            out int edition);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private unsafe struct OSVERSIONINFOEX
        {
            /// <summary>
            ///     The size of this data structure, in bytes. Set this member to sizeof(OSVERSIONINFOEX).
            /// </summary>
            private int _dwOSVersionInfoSize;

            private readonly int _dwMajorVersion;
            private readonly int _dwMinorVersion;
            private readonly int _dwBuildNumber;
            private readonly int _dwPlatformId;
            private fixed char _szCSDVersion[128];
            private readonly short _wServicePackMajor;
            private readonly short _wServicePackMinor;
            private readonly ushort _wSuiteMask;
            private readonly byte _wProductType;
            private readonly byte _wReserved;

            ///     A null-terminated string, such as "Service Pack 3", that indicates the latest Service Pack installed on the system.
            ///     If no Service Pack has been installed, the string is empty.
            /// </summary>
            public string ServicePackVersion
            {
                get
                {
                    fixed (char* servicePackVersion = _szCSDVersion)
                    {
                        return new string(servicePackVersion);
                    }
                }
            }

            /// <summary>
            ///     The major version number of the latest Service Pack installed on the system. For example, for Service Pack 3, the
            ///     major version number is 3.
            ///     If no Service Pack has been installed, the value is zero.
            /// </summary>
            public short ServicePackMajor => _wServicePackMajor;

            /// <summary>
            ///     The minor version number of the latest Service Pack installed on the system. For example, for Service Pack 3, the
            ///     minor version number is 0.
            /// </summary>
            public short ServicePackMinor => _wServicePackMinor;

            /// <summary>
            ///     A bit mask that identifies the product suites available on the system. This member can be a combination of the
            ///     following values.
            /// </summary>
            public ushort SuiteMask => _wSuiteMask;

            /// <summary>
            ///     Any additional information about the system.
            /// </summary>
            public byte ProductType => _wProductType;

            /// <summary>
            /// Factory for an empty OsVersionInfoEx
            /// </summary>
            /// <returns>OSVERSIONINFOEX</returns>
            public static OSVERSIONINFOEX Create()
            {
                return new OSVERSIONINFOEX
                {
                    _dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX))
                };
            }
        }

        private const int PRODUCT_UNDEFINED = 0x00000000;
        private const int PRODUCT_ULTIMATE = 0x00000001;
        private const int PRODUCT_HOME_BASIC = 0x00000002;
        private const int PRODUCT_HOME_PREMIUM = 0x00000003;
        private const int PRODUCT_ENTERPRISE = 0x00000004;
        private const int PRODUCT_HOME_BASIC_N = 0x00000005;
        private const int PRODUCT_BUSINESS = 0x00000006;
        private const int PRODUCT_STANDARD_SERVER = 0x00000007;
        private const int PRODUCT_DATACENTER_SERVER = 0x00000008;
        private const int PRODUCT_SMALLBUSINESS_SERVER = 0x00000009;
        private const int PRODUCT_ENTERPRISE_SERVER = 0x0000000A;
        private const int PRODUCT_STARTER = 0x0000000B;
        private const int PRODUCT_DATACENTER_SERVER_CORE = 0x0000000C;
        private const int PRODUCT_STANDARD_SERVER_CORE = 0x0000000D;
        private const int PRODUCT_ENTERPRISE_SERVER_CORE = 0x0000000E;
        private const int PRODUCT_ENTERPRISE_SERVER_IA64 = 0x0000000F;
        private const int PRODUCT_BUSINESS_N = 0x00000010;
        private const int PRODUCT_WEB_SERVER = 0x00000011;
        private const int PRODUCT_CLUSTER_SERVER = 0x00000012;
        private const int PRODUCT_STORAGE_EXPRESS_SERVER = 0x00000014;
        private const int PRODUCT_STORAGE_STANDARD_SERVER = 0x00000015;
        private const int PRODUCT_STORAGE_WORKGROUP_SERVER = 0x00000016;
        private const int PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x00000017;
        private const int PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x00000018;
        private const int PRODUCT_HOME_PREMIUM_N = 0x0000001A;
        private const int PRODUCT_ENTERPRISE_N = 0x0000001B;
        private const int PRODUCT_ULTIMATE_N = 0x0000001C;
        private const int PRODUCT_WEB_SERVER_CORE = 0x0000001D;
        private const int PRODUCT_MEDIUMBUSINESS_SERVER_MANAGEMENT = 0x0000001E;
        private const int PRODUCT_MEDIUMBUSINESS_SERVER_SECURITY = 0x0000001F;
        private const int PRODUCT_MEDIUMBUSINESS_SERVER_MESSAGING = 0x00000020;
        private const int PRODUCT_SERVER_FOR_SMALLBUSINESS_V = 0x00000023;
        private const int PRODUCT_STANDARD_SERVER_V = 0x00000024;
        private const int PRODUCT_ENTERPRISE_SERVER_V = 0x00000026;
        private const int PRODUCT_STANDARD_SERVER_CORE_V = 0x00000028;
        private const int PRODUCT_ENTERPRISE_SERVER_CORE_V = 0x00000029;
        private const int PRODUCT_HYPERV = 0x0000002A;

        private const int VER_NT_WORKSTATION = 1;
        private const int VER_NT_SERVER = 3;
        private const int VER_SUITE_ENTERPRISE = 2;
        private const int VER_SUITE_DATACENTER = 128;
        private const int VER_SUITE_PERSONAL = 512;
        private const int VER_SUITE_BLADE = 1024;

        /// <summary>
        /// Gets the service pack information of the operating system running on this computer.
        /// </summary>
        public static string ServicePack
        {
            get
            {
                string servicePack = string.Empty;
                OSVERSIONINFOEX osVersionInfo = OSVERSIONINFOEX.Create();

                if (GetVersionEx(ref osVersionInfo))
                {
                    servicePack = osVersionInfo.ServicePackVersion;
                }

                return servicePack;
            }
        }

        /// Gets the full version string of the operating system running on this computer.
        /// </summary>
        public static string VersionString
        {
            get
            {
                if (WindowsVersion.IsWindows10OrLater)
                {
                    return $"build {Environment.OSVersion.Version.Build}";
                }

                if (Environment.OSVersion.Version.Revision != 0)
                {
                    return
                        $"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor} build {Environment.OSVersion.Version.Build} revision {Environment.OSVersion.Version.Revision:X}";
                }

                return $"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor} build {Environment.OSVersion.Version.Build}";
            }
        }
    }
}