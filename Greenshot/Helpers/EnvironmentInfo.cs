/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using GreenshotPlugin.UnmanagedHelpers;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using Greenshot.Drawing;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of EnvironmentInfo.
	/// </summary>
	public static class EnvironmentInfo {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(EnvironmentInfo));
		private static bool? isWindows = null;

		public static bool IsWindows {
			get {
				if (isWindows.HasValue) {
					return isWindows.Value;
				}
				isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
				return isWindows.Value;
			}
		}

		public static bool IsNet45OrNewer() {
			// Class "ReflectionContext" exists from .NET 4.5 onwards.
			return Type.GetType("System.Reflection.ReflectionContext", false) != null;
		}

		public static string EnvironmentToString(bool newline) {
			StringBuilder environment = new StringBuilder();
			environment.Append("Software version: " + Application.ProductVersion);
			if (IniConfig.IsPortable) {
				environment.Append(" Portable");
			}
			environment.Append(" (" + OSInfo.Bits + " bit)");

			if (newline) {
				environment.AppendLine();
			} else {
				environment.Append(", ");
			}
			environment.Append(".NET runtime version: " + Environment.Version);
			if (IsNet45OrNewer()) {
				environment.Append("+");

			}
			if (newline) {
				environment.AppendLine();
			} else {
				environment.Append(", ");
			}
			environment.Append("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));

			if (IsWindows) {
				if (newline) {
					environment.AppendLine();
				} else {
					environment.Append(", ");
				}
				environment.Append(String.Format("OS: {0} {1} {2} (x{3})  {4}", OSInfo.Name, OSInfo.Edition, OSInfo.ServicePack, OSInfo.Bits, OSInfo.VersionString));
				if (newline) {
					environment.AppendLine();
				} else {
					environment.Append(", ");
				}
				// Get some important information for fixing GDI related Problems
				environment.Append("GDI object count: " + User32.GetGuiResourcesGDICount());
				if (newline) {
					environment.AppendLine();
				} else {
					environment.Append(", ");
				}
				environment.Append("User object count: " + User32.GetGuiResourcesUserCount());
			} else {
				if (newline) {
					environment.AppendLine();
				} else {
					environment.Append(", ");
				}
				environment.Append("OS: " + Environment.OSVersion.Platform.ToString());
			}
			if (newline) {
				environment.AppendLine();
			} else {
				environment.Append(", ");
			}
			environment.Append("Surface count: " + Surface.Count);

			return environment.ToString();
		}

		public static string ExceptionToString(Exception ex) {
			if (ex == null)
				return "null\r\n";

			StringBuilder report = new StringBuilder();

			report.AppendLine("Exception: " + ex.GetType().ToString());
			report.AppendLine("Message: " + ex.Message);
			if (ex.Data != null && ex.Data.Count > 0) {
				report.AppendLine();
				report.AppendLine("Additional Information:");
				foreach (object key in ex.Data.Keys) {
					object data = ex.Data[key];
					if (data != null) {
						report.AppendLine(key + " : " + data);
					}
				}
			}
			if (ex is ExternalException) {
				// e.g. COMException
				report.AppendLine().AppendLine("ErrorCode: 0x" + (ex as ExternalException).ErrorCode.ToString("X"));
			}

			report.AppendLine().AppendLine("Stack:").AppendLine(ex.StackTrace);

			if (ex is ReflectionTypeLoadException) {
				report.AppendLine().AppendLine("LoaderExceptions: ");
				foreach (Exception cbE in (ex as ReflectionTypeLoadException).LoaderExceptions) {
					report.AppendLine(cbE.Message);
				}
			}

			if (ex.InnerException != null) {
				report.AppendLine("--- InnerException: ---");
				report.AppendLine(ExceptionToString(ex.InnerException));
			}
			return report.ToString();
		}

		public static string BuildReport(Exception exception) {
			StringBuilder exceptionText = new StringBuilder();
			exceptionText.AppendLine(EnvironmentInfo.EnvironmentToString(true));
			exceptionText.AppendLine(EnvironmentInfo.ExceptionToString(exception));
			exceptionText.AppendLine("Configuration dump:");
			using (TextWriter writer = new StringWriter(exceptionText)) {
				IniConfig.GetIniSection<CoreConfiguration>().Write(writer, true);
			}

			return exceptionText.ToString();
		}
	}

	/// <summary>
	/// Provides detailed information about the host operating system.
	/// Code is available at: http://www.csharp411.com/determine-windows-version-and-edition-with-c/
	/// </summary>
	static public class OSInfo {
		#region BITS
		/// <summary>
		/// Determines if the current application is 32 or 64-bit.
		/// </summary>
		static public int Bits {
			get {
				return IntPtr.Size * 8;
			}
		}
		#endregion BITS

		#region EDITION
		static private string s_Edition;
		/// <summary>
		/// Gets the edition of the operating system running on this computer.
		/// </summary>
		static public string Edition {
			get {
				if (s_Edition != null) {
					return s_Edition;  //***** RETURN *****//
				}

				string edition = String.Empty;

				OperatingSystem osVersion = Environment.OSVersion;
				OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
				osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

				if (GetVersionEx(ref osVersionInfo)) {
					int majorVersion = osVersion.Version.Major;
					int minorVersion = osVersion.Version.Minor;
					byte productType = osVersionInfo.wProductType;
					short suiteMask = osVersionInfo.wSuiteMask;

					#region VERSION 4
					if (majorVersion == 4) {
						if (productType == VER_NT_WORKSTATION) {
							// Windows NT 4.0 Workstation
							edition = "Workstation";
						} else if (productType == VER_NT_SERVER) {
							if ((suiteMask & VER_SUITE_ENTERPRISE) != 0) {
								// Windows NT 4.0 Server Enterprise
								edition = "Enterprise Server";
							} else {
								// Windows NT 4.0 Server
								edition = "Standard Server";
							}
						}
					}
					#endregion VERSION 4

					#region VERSION 5
 else if (majorVersion == 5) {
						if (productType == VER_NT_WORKSTATION) {
							if ((suiteMask & VER_SUITE_PERSONAL) != 0) {
								// Windows XP Home Edition
								edition = "Home";
							} else {
								// Windows XP / Windows 2000 Professional
								edition = "Professional";
							}
						} else if (productType == VER_NT_SERVER) {
							if (minorVersion == 0) {
								if ((suiteMask & VER_SUITE_DATACENTER) != 0) {
									// Windows 2000 Datacenter Server
									edition = "Datacenter Server";
								} else if ((suiteMask & VER_SUITE_ENTERPRISE) != 0) {
									// Windows 2000 Advanced Server
									edition = "Advanced Server";
								} else {
									// Windows 2000 Server
									edition = "Server";
								}
							} else {
								if ((suiteMask & VER_SUITE_DATACENTER) != 0) {
									// Windows Server 2003 Datacenter Edition
									edition = "Datacenter";
								} else if ((suiteMask & VER_SUITE_ENTERPRISE) != 0) {
									// Windows Server 2003 Enterprise Edition
									edition = "Enterprise";
								} else if ((suiteMask & VER_SUITE_BLADE) != 0) {
									// Windows Server 2003 Web Edition
									edition = "Web Edition";
								} else {
									// Windows Server 2003 Standard Edition
									edition = "Standard";
								}
							}
						}
					}
					#endregion VERSION 5

					#region VERSION 6
 else if (majorVersion == 6) {
						int ed;
						if (GetProductInfo(majorVersion, minorVersion, osVersionInfo.wServicePackMajor, osVersionInfo.wServicePackMinor, out ed)) {
							switch (ed) {
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
					#endregion VERSION 6
				}

				s_Edition = edition;
				return edition;
			}
		}
		#endregion EDITION

		#region NAME
		static private string s_Name;
		/// <summary>
		/// Gets the name of the operating system running on this computer.
		/// </summary>
		static public string Name {
			get {
				if (s_Name != null) {
					return s_Name;  //***** RETURN *****//
				}

				string name = "unknown";

				OperatingSystem osVersion = Environment.OSVersion;
				OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();
				osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

				if (GetVersionEx(ref osVersionInfo)) {
					int majorVersion = osVersion.Version.Major;
					int minorVersion = osVersion.Version.Minor;
					byte productType = osVersionInfo.wProductType;
					short suiteMask = osVersionInfo.wSuiteMask;
					switch (osVersion.Platform) {
						case PlatformID.Win32Windows:
							if (majorVersion == 4) {
								string csdVersion = osVersionInfo.szCSDVersion;
								switch (minorVersion) {
									case 0:
										if (csdVersion == "B" || csdVersion == "C") {
											name = "Windows 95 OSR2";
										} else {
											name = "Windows 95";
										}
										break;
									case 10:
										if (csdVersion == "A") {
											name = "Windows 98 Second Edition";
										} else {
											name = "Windows 98";
										}
										break;
									case 90:
										name = "Windows Me";
										break;
								}
							}
							break;
						case PlatformID.Win32NT:
							switch (majorVersion) {
								case 3:
									name = "Windows NT 3.51";
									break;
								case 4:
									switch (productType) {
										case 1:
											name = "Windows NT 4.0";
											break;
										case 3:
											name = "Windows NT 4.0 Server";
											break;
									}
									break;
								case 5:
									switch (minorVersion) {
										case 0:
											name = "Windows 2000";
											break;
										case 1:
											switch (suiteMask) {
												case 0x0200:
													name = "Windows XP Professional";
													break;
												default:
													name = "Windows XP";
													break;
											}
											break;
										case 2:
											switch (suiteMask) {
												case 0x0200:
													name = "Windows XP Professional x64";
													break;
												case 0x0002:
													name = "Windows Server 2003 Enterprise";
													break;
												case 0x0080:
													name = "Windows Server 2003 Data Center";
													break;
												case 0x0400:
													name = "Windows Server 2003 Web Edition";
													break;
												case unchecked((short)0x8000):
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
									switch (minorVersion) {
										case 0:
											switch (productType) {
												case 3:
													name = "Windows Server 2008";
													break;
												default:
													name = "Windows Vista";
													break;
											}
											break;
										case 1:
											switch (productType) {
												case 3:
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
									}
									break;
							}
							break;
					}
				}

				s_Name = name;
				return name;
			}
		}
		#endregion NAME

		#region PINVOKE
		#region GET
		#region PRODUCT INFO
		[DllImport("Kernel32.dll")]
		internal static extern bool GetProductInfo(
			int osMajorVersion,
			int osMinorVersion,
			int spMajorVersion,
			int spMinorVersion,
			out int edition);
		#endregion PRODUCT INFO

		#region VERSION
		[DllImport("kernel32.dll")]
		private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);
		#endregion VERSION
		#endregion GET

		#region OSVERSIONINFOEX
		[StructLayout(LayoutKind.Sequential)]
		private struct OSVERSIONINFOEX {
			public int dwOSVersionInfoSize;
			public int dwMajorVersion;
			public int dwMinorVersion;
			public int dwBuildNumber;
			public int dwPlatformId;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szCSDVersion;
			public short wServicePackMajor;
			public short wServicePackMinor;
			public short wSuiteMask;
			public byte wProductType;
			public byte wReserved;
		}
		#endregion OSVERSIONINFOEX

		#region PRODUCT
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
		private const int PRODUCT_HOME_SERVER = 0x00000013;
		private const int PRODUCT_STORAGE_EXPRESS_SERVER = 0x00000014;
		private const int PRODUCT_STORAGE_STANDARD_SERVER = 0x00000015;
		private const int PRODUCT_STORAGE_WORKGROUP_SERVER = 0x00000016;
		private const int PRODUCT_STORAGE_ENTERPRISE_SERVER = 0x00000017;
		private const int PRODUCT_SERVER_FOR_SMALLBUSINESS = 0x00000018;
		private const int PRODUCT_SMALLBUSINESS_SERVER_PREMIUM = 0x00000019;
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
		#endregion PRODUCT

		#region VERSIONS
		private const int VER_NT_WORKSTATION = 1;
		private const int VER_NT_DOMAIN_CONTROLLER = 2;
		private const int VER_NT_SERVER = 3;
		private const int VER_SUITE_SMALLBUSINESS = 1;
		private const int VER_SUITE_ENTERPRISE = 2;
		private const int VER_SUITE_TERMINAL = 16;
		private const int VER_SUITE_DATACENTER = 128;
		private const int VER_SUITE_SINGLEUSERTS = 256;
		private const int VER_SUITE_PERSONAL = 512;
		private const int VER_SUITE_BLADE = 1024;
		#endregion VERSIONS
		#endregion PINVOKE

		#region SERVICE PACK
		/// <summary>
		/// Gets the service pack information of the operating system running on this computer.
		/// </summary>
		static public string ServicePack {
			get {
				string servicePack = String.Empty;
				OSVERSIONINFOEX osVersionInfo = new OSVERSIONINFOEX();

				osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

				if (GetVersionEx(ref osVersionInfo)) {
					servicePack = osVersionInfo.szCSDVersion;
				}

				return servicePack;
			}
		}
		#endregion SERVICE PACK

		#region VERSION
		#region BUILD
		/// <summary>
		/// Gets the build version number of the operating system running on this computer.
		/// </summary>
		public static int BuildVersion {
			get {
				return Environment.OSVersion.Version.Build;
			}
		}
		#endregion BUILD

		#region FULL
		#region STRING
		/// <summary>
		/// Gets the full version string of the operating system running on this computer.
		/// </summary>
		static public string VersionString {
			get {
				return string.Format("{0}.{1} build {3} revision {2:X}", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, Environment.OSVersion.Version.Revision, Environment.OSVersion.Version.Build);
			}
		}
		#endregion STRING

		#region VERSION
		/// <summary>
		/// Gets the full version of the operating system running on this computer.
		/// </summary>
		static public Version Version {
			get {
				return Environment.OSVersion.Version;
			}
		}
		#endregion VERSION
		#endregion FULL

		#region MAJOR
		/// <summary>
		/// Gets the major version number of the operating system running on this computer.
		/// </summary>
		static public int MajorVersion {
			get {
				return Environment.OSVersion.Version.Major;
			}
		}
		#endregion MAJOR

		#region MINOR
		/// <summary>
		/// Gets the minor version number of the operating system running on this computer.
		/// </summary>
		static public int MinorVersion {
			get {
				return Environment.OSVersion.Version.Minor;
			}
		}
		#endregion MINOR

		#region REVISION
		/// <summary>
		/// Gets the revision version number of the operating system running on this computer.
		/// </summary>
		static public int RevisionVersion {
			get {
				return Environment.OSVersion.Version.Revision;
			}
		}
		#endregion REVISION
		#endregion VERSION
	}
}
