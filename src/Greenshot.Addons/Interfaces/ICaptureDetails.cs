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
using System.Collections.Generic;
using Greenshot.Addons.Components;

namespace Greenshot.Addons.Interfaces
{
	/// <summary>
	///     Details for the capture, like the window title and date/time etc.
	/// </summary>
	public interface ICaptureDetails
	{
		/// <summary>
		/// The filename for the capture
		/// </summary>
		string Filename { get; set; }

        /// <summary>
        /// Title of the capture
        /// </summary>
		string Title { get; set; }

        /// <summary>
        /// Date and time when the capture was taken
        /// </summary>
		DateTime DateTime { get; set; }

        /// <summary>
        /// A list of destinations (TODO: to what end?
        /// </summary>
		List<IDestination> CaptureDestinations { get; set; }

        /// <summary>
        /// Meta data for the capture
        /// </summary>
		Dictionary<string, string> MetaData { get; }

        /// <summary>
        /// What mode the capture was taken with
        /// </summary>
		CaptureMode CaptureMode { get; set; }

        /// <summary>
        /// DPI-X settings for the capture
        /// </summary>
		float DpiX { get; set; }

        /// <summary>
        /// DPI-Y settings for the capture
        /// </summary>
		float DpiY { get; set; }

		/// <summary>
		///     Helper method to prevent complex code which needs to check every key
		/// </summary>
		/// <param name="key">The key for the meta-data</param>
		/// <param name="value">The value for the meta-data</param>
		void AddMetaData(string key, string value);

        /// <summary>
        /// Clear all destinations
        /// </summary>
		void ClearDestinations();

        /// <summary>
        /// Remove a destination
        /// </summary>
        /// <param name="captureDestination"></param>
		void RemoveDestination(IDestination captureDestination);

        /// <summary>
        /// Add a destination
        /// </summary>
        /// <param name="captureDestination"></param>
		void AddDestination(IDestination captureDestination);

        /// <summary>
        /// Is a certain destination available?
        /// </summary>
        /// <param name="designation">string</param>
        /// <returns>bool</returns>
		bool HasDestination(string designation);
	}
}