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