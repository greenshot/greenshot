// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dapplo.Windows.User32;

namespace Greenshot.Helpers
{
	/// <summary>
	/// EnvironmentInfo provides information on the current environment
	/// </summary>
	public static class EnvironmentInfo
	{
		private static bool? _isWindows;

		/// <summary>
		/// Are we running on Windows?
		/// </summary>
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

        /// <summary>
        /// Are we using Net45 or newer?
        /// </summary>
        /// <returns>bool</returns>
        public static bool IsNet45OrNewer()
		{
			// Class "ReflectionContext" exists from .NET 4.5 onwards.
			return Type.GetType("System.Reflection.ReflectionContext", false) != null;
		}

        /// <summary>
        /// Create a description of the environment
        /// </summary>
        /// <param name="newline">bool specifying if a newline or , nees to be used</param>
        /// <returns>string</returns>
        public static string EnvironmentToString(bool newline)
		{
			var environment = new StringBuilder("Software version: " + Application.ProductVersion);
            if (SingleExeHelper.IsRunningAsSingleExe)
            {
                environment.Append(" SE");
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

				environment.AppendFormat("OS: {0} {1} {2} (x{3}) {4}", OsInfo.Name, OsInfo.Edition, OsInfo.ServicePack, OsInfo.Bits, OsInfo.VersionString);
				if (newline)
				{
					environment.AppendLine();
				}
				else
				{
					environment.Append(", ");
				}

                if (SingleExeHelper.IsRunningAsSingleExe)
                {
                    environment.AppendFormat("Exe Location: {0}", SingleExeHelper.SingleExeLocation);
                    if (newline)
                    {
                        environment.AppendLine();
                    }
                    else
                    {
                        environment.Append(", ");
                    }
                    environment.AppendFormat("Unpacked into: {0}", SingleExeHelper.UnpackPath);
                    if (newline)
                    {
                        environment.AppendLine();
                    }
                    else
                    {
                        environment.Append(", ");
                    }
                }
                else
                {
                    environment.AppendFormat("Exe Location: {0}", Assembly.GetEntryAssembly()?.Location);
                    if (newline)
                    {
                        environment.AppendLine();
                    }
                    else
                    {
                        environment.Append(", ");
                    }
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
			return environment.ToString();
		}

        /// <summary>
        /// Create a nice string for the specified exception
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns>string</returns>
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
}