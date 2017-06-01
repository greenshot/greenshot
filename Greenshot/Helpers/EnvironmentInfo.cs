#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;
using Dapplo.Windows.Kernel32.Structs;
using Dapplo.Windows.User32;
using GreenshotPlugin.IniFile;

#endregion

namespace Greenshot.Helpers
{
	/// <summary>
	///     Description of EnvironmentInfo.
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

		public static string EnvironmentToString(bool newline)
		{
			var environment = new StringBuilder();
			environment.Append("Software version: " + Application.ProductVersion);
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
				environment.Append(string.Format("OS: {0} {1} {2} (x{3})  {4}", OsInfo.Name, OsInfo.Edition, OsInfo.ServicePack, OsInfo.Bits, OsInfo.VersionString));
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
			{
				return "null\r\n";
			}

			var report = new StringBuilder();

			report.AppendLine("Exception: " + ex.GetType());
			report.AppendLine("Message: " + ex.Message);
			if (ex.Data.Count > 0)
			{
				report.AppendLine();
				report.AppendLine("Additional Information:");
				foreach (var key in ex.Data.Keys)
				{
					var data = ex.Data[key];
					if (data != null)
					{
						report.AppendLine(key + " : " + data);
					}
				}
			}
			if (ex is ExternalException)
			{
				// e.g. COMException
				report.AppendLine().AppendLine("ErrorCode: 0x" + (ex as ExternalException).ErrorCode.ToString("X"));
			}

			report.AppendLine().AppendLine("Stack:").AppendLine(ex.StackTrace);

			if (ex is ReflectionTypeLoadException)
			{
				report.AppendLine().AppendLine("LoaderExceptions: ");
				foreach (var cbE in (ex as ReflectionTypeLoadException).LoaderExceptions)
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
			var exceptionText = new StringBuilder();
			exceptionText.AppendLine(EnvironmentToString(true));
			exceptionText.AppendLine(ExceptionToString(exception));
			exceptionText.AppendLine("Configuration dump:");

			return exceptionText.ToString();
		}
	}

	/// <summary>
	///     Provides detailed information about the host operating system.
	///     Code is available at: http://www.csharp411.com/determine-windows-version-and-edition-with-c/
	/// </summary>
	public static class OsInfo
	{
		#region BITS

		/// <summary>
		///     Determines if the current application is 32 or 64-bit.
		/// </summary>
		public static int Bits => IntPtr.Size * 8;

		#endregion BITS

		#region SERVICE PACK

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

		#endregion SERVICE PACK

		#region EDITION

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

				#region VERSION 4

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
				#endregion VERSION 4

				#region VERSION 5

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
				#endregion VERSION 5

				#region VERSION 6

				else if (majorVersion == 6)
				{
					WindowsProducts ed;
					if (Kernel32Api.GetProductInfo(majorVersion, minorVersion, osVersionInfo.ServicePackMajor, osVersionInfo.ServicePackMinor, out ed))
					{
						var memberInfo = ed.GetType().GetMember(ed.ToString()).FirstOrDefault();

						if (memberInfo != null)
						{
							var descriptionAttribute = (DescriptionAttribute) memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
							edition = descriptionAttribute?.Description ?? "unknown";
						}
					}
				}

				#endregion VERSION 6

				_sEdition = edition;
				return edition;
			}
		}

		#endregion EDITION

		#region NAME

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

		#endregion NAME

		#region VERSION

		#region BUILD

		/// <summary>
		///     Gets the build version number of the operating system running on this computer.
		/// </summary>
		public static int BuildVersion => Environment.OSVersion.Version.Build;

		#endregion BUILD

		#region FULL

		#region STRING

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

		#endregion STRING

		#region VERSION

		/// <summary>
		///     Gets the full version of the operating system running on this computer.
		/// </summary>
		public static Version Version
		{
			get { return Environment.OSVersion.Version; }
		}

		#endregion VERSION

		#endregion FULL

		#region MAJOR

		/// <summary>
		///     Gets the major version number of the operating system running on this computer.
		/// </summary>
		public static int MajorVersion
		{
			get { return Environment.OSVersion.Version.Major; }
		}

		#endregion MAJOR

		#region MINOR

		/// <summary>
		///     Gets the minor version number of the operating system running on this computer.
		/// </summary>
		public static int MinorVersion
		{
			get { return Environment.OSVersion.Version.Minor; }
		}

		#endregion MINOR

		#region REVISION

		/// <summary>
		///     Gets the revision version number of the operating system running on this computer.
		/// </summary>
		public static int RevisionVersion
		{
			get { return Environment.OSVersion.Version.Revision; }
		}

		#endregion REVISION

		#endregion VERSION
	}
}