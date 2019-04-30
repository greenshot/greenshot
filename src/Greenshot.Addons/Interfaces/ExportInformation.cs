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

namespace Greenshot.Addons.Interfaces
{
	/// <summary>
	/// This contains information about an export
	/// </summary>
	public class ExportInformation
	{
        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="destinationDesignation">string</param>
        /// <param name="destinationDescription">string</param>
        public ExportInformation(string destinationDesignation, string destinationDescription)
		{
			DestinationDesignation = destinationDesignation;
			DestinationDescription = destinationDescription;
		}

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="destinationDesignation">string</param>
        /// <param name="destinationDescription">string</param>
        /// <param name="exportMade">bool</param>
        public ExportInformation(string destinationDesignation, string destinationDescription, bool exportMade) : this(destinationDesignation, destinationDescription)
		{
			ExportMade = exportMade;
		}

	    /// <summary>
	    /// Was there an error in this export?
	    /// </summary>
	    public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Was there not an error in tthishe export?
        /// </summary>
	    public bool IsOk => string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Did we export to file?
        /// </summary>
        public bool IsFileExport => !string.IsNullOrEmpty(Filepath);

        /// <summary>
        /// Did we export to a "cloud" service?
        /// </summary>
	    public bool IsCloudExport => !string.IsNullOrEmpty(Uri);

        /// <summary>
        /// What is the designation of the destination this was exported to?
        /// </summary>
        public string DestinationDesignation { get; }

        /// <summary>
        /// What is the description of the destination
        /// </summary>
		public string DestinationDescription { get; set; }

		/// <summary>
		///     Set to true to specify if the export worked.
		/// </summary>
		public bool ExportMade { get; set; }

		/// <summary>
		/// The uri where the export can be found
		/// </summary>
		public string Uri { get; set; }

        /// <summary>
        /// The error message when an error occured
        /// </summary>
		public string ErrorMessage { get; set; }

        /// <summary>
        /// The path to the file where the export can be found
        /// </summary>
		public string Filepath { get; set; }
	}
}