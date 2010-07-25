/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using Greenshot.UnmanagedHelpers;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of EnvironmentInfo.
	/// </summary>
	public class EnvironmentInfo {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger("Greenshot");

		private EnvironmentInfo(){}
		
		public static string EnvironmentToString(bool newline) {
			StringBuilder environment = new StringBuilder(); 
			environment.Append("Software version: " + Application.ProductVersion);
			if (newline) {
				environment.AppendLine();
			} else {
				environment.Append(", ");
			}
			environment.Append(".NET runtime version: " + Assembly.GetEntryAssembly().ImageRuntimeVersion);
			if (newline) {
				environment.AppendLine();
			} else {
				environment.Append(", ");
			}
			environment.Append("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"));
			if (newline) {
				environment.AppendLine();
			} else {
				environment.Append(", ");
			}
			
			environment.Append("OS: " + OSName + " Build " + OSBuild + " " + OSServicePack + (Is64Bit ? " x64" : ""));
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
				foreach(string key in ex.Data.Keys) {
					report.AppendLine(key + " = " + ex.Data[key]);
				}
			}
			if (ex is ExternalException) {
				// e.g. COMException
				report.AppendLine().AppendLine("ErrorCode: 0x" + (ex as ExternalException).ErrorCode.ToString("X"));
			}

			report.AppendLine().AppendLine("Stack:").AppendLine(ex.StackTrace);

			if(ex is ReflectionTypeLoadException) {
				report.AppendLine().AppendLine("LoaderExceptions: ");
				foreach(Exception cbE in (ex as ReflectionTypeLoadException).LoaderExceptions) {
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
			return exceptionText.ToString();
		}
		
		#region OS version detection
		/// <summary>
		/// Enumeration of known operating systems
		/// </summary>
		public enum OSVersionEnum : int {
			/// <summary>
			/// Unknown
			/// </summary>
			Unknown = 0,
			/// <summary>
			/// Microsoft Windows 95
			/// </summary>
			Win95,
			/// <summary>
			/// Microsoft Windows 98
			/// </summary>
			Win98,
			/// <summary>
			/// Microsoft Windows ME
			/// </summary>
			WinME,
			/// <summary>
			/// Microsoft Windows NT 4.0
			/// </summary>
			WinNT4,
			/// <summary>
			/// Microsoft Windows 2000
			/// </summary>
			Win2000,
			/// <summary>
			/// Microsoft Windows XP
			/// </summary>
			WinXP,
			/// <summary>
			/// Microsoft Windows Server 2003
			/// </summary>
			Win2003,
			/// <summary>
			/// Microsoft Windows Vista
			/// </summary>
			WinVista,
			/// <summary>
			/// Microsoft Windows 7
			/// </summary>
			Win7
		}
		
		/// <summary>
		/// Liefert das aktuelle Betriebsystem.
		/// </summary>
		/// <returns></returns>
		public static OSVersionEnum OSVersion {
			get {
				OperatingSystem os = Environment.OSVersion;
				if (os.Platform == PlatformID.Win32Windows && os.Version.Major >= 4 && os.Version.Minor == 0)
					return OSVersionEnum.Win95;
				if (os.Platform == PlatformID.Win32Windows && os.Version.Major >= 4 && os.Version.Minor > 0 && os.Version.Minor < 90)
					return OSVersionEnum.Win98;
				if (os.Platform == PlatformID.Win32Windows && os.Version.Major >= 4 && os.Version.Minor >= 90)
					return OSVersionEnum.WinME;
				if (os.Platform == PlatformID.Win32NT && os.Version.Major <= 4)
					return OSVersionEnum.WinNT4;
				if (os.Platform == PlatformID.Win32NT && os.Version.Major == 5 && os.Version.Minor == 0)
					return OSVersionEnum.Win2000;
				if (os.Platform == PlatformID.Win32NT && os.Version.Major == 5 && os.Version.Minor == 1)
					return OSVersionEnum.WinXP;
				if (os.Platform == PlatformID.Win32NT && os.Version.Major == 5 && os.Version.Minor == 2)
					return OSVersionEnum.Win2003;
				if (os.Platform == PlatformID.Win32NT && os.Version.Major == 6 && os.Version.Minor == 0)
					return OSVersionEnum.WinVista;
				if (os.Platform == PlatformID.Win32NT && os.Version.Major == 7)
					return OSVersionEnum.Win7;
				return OSVersionEnum.Unknown;
			}
		}
		
		/// <summary>
		/// Gibt an, ob ein 64-Bit-Betriebssystem läuft
		/// </summary>
		/// <returns></returns>
		public static bool Is64Bit {
			get {
				return IntPtr.Size == 8;
			}
		}
		
		/// <summary>
		/// Liefert den Namen des Betriebssytem (z.B. "Windows XP")
		/// </summary>
		/// <returns></returns>
		public static string OSName {
			get {
				switch (OSVersion) {
					case OSVersionEnum.Win95: return "Windows 95";
					case OSVersionEnum.Win98: return "Windows 98";
					case OSVersionEnum.WinME: return "Windows ME";
					case OSVersionEnum.WinNT4: return "Windows NT 4";
					case OSVersionEnum.Win2000: return "Windows 2000";
					case OSVersionEnum.WinXP: return "Windows XP" + (Is64Bit ? " x64" : "");
					case OSVersionEnum.Win2003: return "Windows Server 2003" + (Is64Bit ? " x64" : "");
					case OSVersionEnum.WinVista: return "Windows Vista" + (Is64Bit ? " x64" : "");
					case OSVersionEnum.Win7: return "Windows 7" + (Is64Bit ? " x64" : "");
				}
				return "";
			}
		}
		
		public static string OSBuild {
			get {
				OperatingSystem os = Environment.OSVersion;
				return os.Version.Build.ToString();
			}
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct OSVERSIONINFO {
			public int dwOSVersionInfoSize;
			public int dwMajorVersion;
			public int dwMinorVersion;
			public int dwBuildNumber;
			public int dwPlatformId;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szCSDVersion;
		}
		[DllImport("kernel32.Dll")]
		public static extern short GetVersionEx(ref OSVERSIONINFO o);
		static public string OSServicePack {
			get {
				OSVERSIONINFO os = new OSVERSIONINFO();
				os.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFO));
				GetVersionEx(ref os);
				return os.szCSDVersion;
			}
		}
		#endregion OS version detection
	}
}
