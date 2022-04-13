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
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;
using Dapplo.Windows.Kernel32.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.Common.Extensions;
using Greenshot.Base.IniFile;
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
                greenshotVersion = assemblyFileVersion.ToString(2);
                try
                {
                    greenshotVersion = assemblyFileVersion.ToString(3);
                }
                catch (Exception)
                {
                    // Ignore
                }
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
            StringBuilder environment = new();
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
                environment.AppendFormat("GDI object count: {0}", User32Api.GetGuiResourcesGdiCount());
                if (newline)
                {
                    environment.AppendLine();
                }
                else
                {
                    environment.Append(", ");
                }

                environment.AppendFormat("User object count: {0}", User32Api.GetGuiResourcesUserCount());
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

            StringBuilder report = new();

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
            StringBuilder exceptionText = new();
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
                var osVersionInfo = OsVersionInfoEx.Create();

                if (Kernel32Api.GetVersionEx(ref osVersionInfo))
                {
                    int majorVersion = osVersion.Version.Major;
                    int minorVersion = osVersion.Version.Minor;
                    var productType = osVersionInfo.ProductType;
                    var suiteMask = osVersionInfo.SuiteMask;

                    if (majorVersion == 4)
                    {
                        if (productType == WindowsProductTypes.VER_NT_WORKSTATION)
                        {
                            // Windows NT 4.0 Workstation
                            edition = "Workstation";
                        }
                        else if (productType == WindowsProductTypes.VER_NT_SERVER)
                        {
                            edition = (suiteMask & WindowsSuites.Enterprise) != 0 ? "Enterprise Server" : "Standard Server";
                        }
                    }

                    else if (majorVersion == 5)
                    {
                        if (productType == WindowsProductTypes.VER_NT_WORKSTATION)
                        {
                            if ((suiteMask & WindowsSuites.Personal) != 0)
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
                        else if (productType == WindowsProductTypes.VER_NT_SERVER)
                        {
                            if (minorVersion == 0)
                            {
                                if ((suiteMask & WindowsSuites.DataCenter) != 0)
                                {
                                    // Windows 2000 Datacenter Server
                                    edition = "Datacenter Server";
                                }
                                else if ((suiteMask & WindowsSuites.Enterprise) != 0)
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
                                if ((suiteMask & WindowsSuites.DataCenter) != 0)
                                {
                                    // Windows Server 2003 Datacenter Edition
                                    edition = "Datacenter";
                                }
                                else if ((suiteMask & WindowsSuites.Enterprise) != 0)
                                {
                                    // Windows Server 2003 Enterprise Edition
                                    edition = "Enterprise";
                                }
                                else if ((suiteMask & WindowsSuites.Blade) != 0)
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
                        if (Kernel32Api.GetProductInfo(majorVersion, minorVersion, osVersionInfo.ServicePackMajor, osVersionInfo.ServicePackMinor, out var windowsProduct))
                        {
                            edition = windowsProduct.GetEnumDescription();
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
                var osVersionInfo = OsVersionInfoEx.Create();
                if (Kernel32Api.GetVersionEx(ref osVersionInfo))
                {
                    int majorVersion = osVersion.Version.Major;
                    int minorVersion = osVersion.Version.Minor;
                    var productType = osVersionInfo.ProductType;
                    var suiteMask = osVersionInfo.SuiteMask;
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
                                        case WindowsProductTypes.VER_NT_WORKSTATION:
                                            name = "Windows NT 4.0";
                                            break;
                                        case WindowsProductTypes.VER_NT_SERVER:
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
                                                WindowsSuites.Personal => "Windows XP Professional",
                                                _ => "Windows XP"
                                            };
                                            break;
                                        case 2:
                                            name = suiteMask switch
                                            {
                                                WindowsSuites.Personal => "Windows XP Professional x64",
                                                WindowsSuites.Enterprise => "Windows Server 2003 Enterprise",
                                                WindowsSuites.DataCenter => "Windows Server 2003 Data Center",
                                                WindowsSuites.Blade => "Windows Server 2003 Web Edition",
                                                WindowsSuites.WHServer => "Windows Home Server",
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
                                                WindowsProductTypes.VER_NT_SERVER => "Windows Server 2008",
                                                _ => "Windows Vista"
                                            };
                                            break;
                                        case 1:
                                            name = productType switch
                                            {
                                                WindowsProductTypes.VER_NT_SERVER => "Windows Server 2008 R2",
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

        /// <summary>
        /// Gets the service pack information of the operating system running on this computer.
        /// </summary>
        public static string ServicePack
        {
            get
            {
                string servicePack = string.Empty;
                OsVersionInfoEx osVersionInfo = OsVersionInfoEx.Create();

                if (Kernel32Api.GetVersionEx(ref osVersionInfo))
                {
                    servicePack = osVersionInfo.ServicePackVersion;
                }

                return servicePack;
            }
        }

        /// <summary>
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