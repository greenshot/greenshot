#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
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

#endregion

#region Usings

using System;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Extensions to help with querying the Operating System
	/// </summary>
	public static class OperatingSystemExtensions
	{
		/// <summary>
		///     Test if the current OS is Windows 10
		/// </summary>
		/// <param name="operatingSystem">OperatingSystem from Environment.OSVersion</param>
		/// <returns>true if we are running on Windows 10</returns>
		public static bool IsWindows10(this OperatingSystem operatingSystem)
		{
			return operatingSystem.Version.Major == 10;
		}

		/// <summary>
		///     Test if the current OS is Windows 10 or later
		/// </summary>
		/// <param name="operatingSystem">OperatingSystem from Environment.OSVersion</param>
		/// <returns>true if we are running on Windows 10 or later</returns>
		public static bool IsWindows10OrLater(this OperatingSystem operatingSystem)
		{
			return operatingSystem.Version.Major >= 10;
		}

		/// <summary>
		///     Test if the current OS is Windows 8(.1)
		/// </summary>
		/// <param name="operatingSystem">OperatingSystem from Environment.OSVersion</param>
		/// <returns>true if we are running on Windows 8(.1)</returns>
		public static bool IsWindows8(this OperatingSystem operatingSystem)
		{
			return operatingSystem.Version.Major == 6 && operatingSystem.Version.Minor >= 2;
		}

		/// <summary>
		///     Test if the current OS is Windows 8 or later
		/// </summary>
		/// <param name="operatingSystem">OperatingSystem from Environment.OSVersion</param>
		/// <returns>true if we are running on Windows 8 or later</returns>
		public static bool IsWindows8OrLater(this OperatingSystem operatingSystem)
		{
			return operatingSystem.Version.Major == 6 && operatingSystem.Version.Minor >= 2 || operatingSystem.Version.Major > 6;
		}

		/// <summary>
		///     Test if the current OS is Windows 7 or later
		/// </summary>
		/// <param name="operatingSystem">OperatingSystem from Environment.OSVersion</param>
		/// <returns>true if we are running on Windows 7 or later</returns>
		public static bool IsWindows7OrLater(this OperatingSystem operatingSystem)
		{
			return operatingSystem.Version.Major == 6 && operatingSystem.Version.Minor >= 1 || operatingSystem.Version.Major > 6;
		}

		/// <summary>
		///     Test if the current OS is Windows Vista or later
		/// </summary>
		/// <param name="operatingSystem">OperatingSystem from Environment.OSVersion</param>
		/// <returns>true if we are running on Windows Vista or later</returns>
		public static bool IsWindowsVistaOrLater(this OperatingSystem operatingSystem)
		{
			return operatingSystem.Version.Major >= 6;
		}

		/// <summary>
		///     Test if the current OS is Windows XP or later
		/// </summary>
		/// <param name="operatingSystem">OperatingSystem from Environment.OSVersion</param>
		/// <returns>true if we are running on Windows XP or later</returns>
		public static bool IsWindowsXpOrLater(this OperatingSystem operatingSystem)
		{
			// Windows 2000 is Major 5 minor 0
			return Environment.OSVersion.Version.Major > 5 || Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1;
		}
	}
}