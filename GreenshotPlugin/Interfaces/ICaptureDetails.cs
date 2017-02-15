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
using System.Collections.Generic;

#endregion

namespace GreenshotPlugin.Interfaces
{
	//, Video };

	/// <summary>
	///     Details for the capture, like the window title and date/time etc.
	/// </summary>
	public interface ICaptureDetails
	{
		string Filename { get; set; }

		string Title { get; set; }

		DateTime DateTime { get; set; }

		List<IDestination> CaptureDestinations { get; set; }

		Dictionary<string, string> MetaData { get; }

		CaptureMode CaptureMode { get; set; }

		float DpiX { get; set; }

		float DpiY { get; set; }

		/// <summary>
		///     Helper method to prevent complex code which needs to check every key
		/// </summary>
		/// <param name="key">The key for the meta-data</param>
		/// <param name="value">The value for the meta-data</param>
		void AddMetaData(string key, string value);

		void ClearDestinations();
		void RemoveDestination(IDestination captureDestination);
		void AddDestination(IDestination captureDestination);
		bool HasDestination(string designation);
	}
}