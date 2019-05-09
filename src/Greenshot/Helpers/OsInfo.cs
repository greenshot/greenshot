// Dapplo - building blocks for desktop applications
// Copyright (C) 2019 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System;
using System.ComponentModel;
using System.Linq;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;
using Dapplo.Windows.Kernel32.Structs;

namespace Greenshot.Helpers
{
    /// <summary>
    ///     Provides detailed information about the host operating system.
    ///     Code is available at: http://www.csharp411.com/determine-windows-version-and-edition-with-c/
    /// </summary>
    public static class OsInfo
    {
        /// <summary>
        ///     Determines if the current application is 32 or 64-bit.
        /// </summary>
        public static int Bits => IntPtr.Size * 8;

        /// <summary>
        ///     Gets the service pack information of the operating system running on this computer.
        /// </summary>
        public static string ServicePack
        {
            get
            {
                var servicePack = string.Empty;
                var osVersionInfo = OsVersionInfoEx.Create();

                if (Kernel32Api.GetVersionEx(ref osVersionInfo))
                {
                    servicePack = osVersionInfo.ServicePackVersion;
                }

                return servicePack;
            }
        }

        private static string _sEdition;

        /// <summary>
        ///     Gets the edition of the operating system running on this computer.
        /// </summary>
        public static string Edition
        {
            get
            {
                if (_sEdition != null)
                {
                    return _sEdition; //***** RETURN *****//
                }

                var edition = string.Empty;

                var osVersion = Environment.OSVersion;
                var osVersionInfo = OsVersionInfoEx.Create();
                if (!Kernel32Api.GetVersionEx(ref osVersionInfo))
                {
                    _sEdition = edition;
                    return edition;
                }

                var majorVersion = osVersion.Version.Major;
                var minorVersion = osVersion.Version.Minor;
                var productType = osVersionInfo.ProductType;
                var suiteMask = osVersionInfo.SuiteMask;

                if (majorVersion == 4)
                {
                    if (productType.HasFlag(WindowsProductTypes.VER_NT_WORKSTATION))
                    {
                        // Windows NT 4.0 Workstation
                        edition = "Workstation";
                    }
                    else if (productType.HasFlag(WindowsProductTypes.VER_NT_SERVER))
                    {
                        edition = suiteMask.HasFlag(WindowsSuites.Enterprise) ? "Enterprise Server" : "Standard Server";
                    }
                }

                else if (majorVersion == 5)
                {
                    if (productType.HasFlag(WindowsProductTypes.VER_NT_WORKSTATION))
                    {
                        if (suiteMask.HasFlag(WindowsSuites.Personal))
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
                    else if (productType.HasFlag(WindowsProductTypes.VER_NT_SERVER))
                    {
                        if (minorVersion == 0)
                        {
                            if (suiteMask.HasFlag(WindowsSuites.DataCenter))
                            {
                                // Windows 2000 Datacenter Server
                                edition = "Datacenter Server";
                            }
                            else if (suiteMask.HasFlag(WindowsSuites.Enterprise))
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
                            if (suiteMask.HasFlag(WindowsSuites.DataCenter))
                            {
                                // Windows Server 2003 Datacenter Edition
                                edition = "Datacenter";
                            }
                            else if (suiteMask.HasFlag(WindowsSuites.Enterprise))
                            {
                                // Windows Server 2003 Enterprise Edition
                                edition = "Enterprise";
                            }
                            else if (suiteMask.HasFlag(WindowsSuites.Blade))
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
                    if (Kernel32Api.GetProductInfo(majorVersion, minorVersion, osVersionInfo.ServicePackMajor, osVersionInfo.ServicePackMinor, out var ed))
                    {
                        var memberInfo = ed.GetType().GetMember(ed.ToString()).FirstOrDefault();

                        if (memberInfo != null)
                        {
                            var descriptionAttribute = (DescriptionAttribute) memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                            edition = descriptionAttribute?.Description ?? "unknown";
                        }
                    }
                }

                _sEdition = edition;
                return edition;
            }
        }

        private static string _name;

        /// <summary>
        ///     Gets the name of the operating system running on this computer.
        /// </summary>
        public static string Name
        {
            get
            {
                if (_name != null)
                {
                    return _name; //***** RETURN *****//
                }

                var name = "unknown";

                var osVersion = Environment.OSVersion;
                var osVersionInfo = OsVersionInfoEx.Create();
                if (!Kernel32Api.GetVersionEx(ref osVersionInfo))
                {
                    _name = name;
                    return name;
                }

                var majorVersion = osVersion.Version.Major;
                var minorVersion = osVersion.Version.Minor;
                var productType = osVersionInfo.ProductType;
                var suiteMask = osVersionInfo.SuiteMask;
                switch (osVersion.Platform)
                {
                    case PlatformID.Win32Windows:
                        if (majorVersion == 4)
                        {
                            var csdVersion = osVersionInfo.ServicePackVersion;
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
                                    case WindowsProductTypes.VER_NT_DOMAIN_CONTROLLER:
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
                                        switch (suiteMask)
                                        {
                                            case WindowsSuites.Personal:
                                                name = "Windows XP Professional";
                                                break;
                                            default:
                                                name = "Windows XP";
                                                break;
                                        }
                                        break;
                                    case 2:
                                        switch (suiteMask)
                                        {
                                            case WindowsSuites.Personal:
                                                name = "Windows XP Professional x64";
                                                break;
                                            case WindowsSuites.Enterprise:
                                                name = "Windows Server 2003 Enterprise";
                                                break;
                                            case WindowsSuites.DataCenter:
                                                name = "Windows Server 2003 Data Center";
                                                break;
                                            case WindowsSuites.Blade:
                                                name = "Windows Server 2003 Web Edition";
                                                break;
                                            case WindowsSuites.WHServer:
                                                name = "Windows Home Server";
                                                break;
                                            default:
                                                name = "Windows Server 2003";
                                                break;
                                        }
                                        break;
                                }
                                break;
                            case 6:
                                switch (minorVersion)
                                {
                                    case 0:
                                        switch (productType)
                                        {
                                            case WindowsProductTypes.VER_NT_SERVER:
                                                name = "Windows Server 2008";
                                                break;
                                            default:
                                                name = "Windows Vista";
                                                break;
                                        }
                                        break;
                                    case 1:
                                        switch (productType)
                                        {
                                            case WindowsProductTypes.VER_NT_SERVER:
                                                name = "Windows Server 2008 R2";
                                                break;
                                            default:
                                                name = "Windows 7";
                                                break;
                                        }
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
                                name = "Windows 10";
                                break;
                        }
                        break;
                }

                _name = name;
                return name;
            }
        }

        /// <summary>
        ///     Gets the build version number of the operating system running on this computer.
        /// </summary>
        public static int BuildVersion => Environment.OSVersion.Version.Build;

        /// <summary>
        ///     Gets the full version string of the operating system running on this computer.
        /// </summary>
        public static string VersionString
        {
            get
            {
                return string.Format("{0}.{1} build {3} revision {2:X}", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor,
                    Environment.OSVersion.Version.Revision, Environment.OSVersion.Version.Build);
            }
        }

        /// <summary>
        ///     Gets the full version of the operating system running on this computer.
        /// </summary>
        public static Version Version
        {
            get { return Environment.OSVersion.Version; }
        }

        /// <summary>
        ///     Gets the major version number of the operating system running on this computer.
        /// </summary>
        public static int MajorVersion
        {
            get { return Environment.OSVersion.Version.Major; }
        }

        /// <summary>
        ///     Gets the minor version number of the operating system running on this computer.
        /// </summary>
        public static int MinorVersion
        {
            get { return Environment.OSVersion.Version.Minor; }
        }

        /// <summary>
        ///     Gets the revision version number of the operating system running on this computer.
        /// </summary>
        public static int RevisionVersion
        {
            get { return Environment.OSVersion.Version.Revision; }
        }
    }
}