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

namespace Greenshot.Addon.ExternalCommand.Entities
{
    /// <summary>
    /// This enum specifies behavior of how the external command is called
    /// </summary>
    [Flags]
    public enum CommandBehaviors
    {
        /// <summary>
        /// Just export and call the command, ignore everything else
        /// </summary>
        None,

        /// <summary>
        /// Remove the export if the command finishes, this implicitly means the command is awaited
        /// </summary>
        DeleteOnExit,

        /// <summary>
        /// Look at the return code, if this is an error the export fails and might be offered again (destination picker)
        /// </summary>
        ProcessReturncode,

        /// <summary>
        /// Scan the output for Uris
        /// These are than available in the export information
        /// </summary>
        ParseOutputForUris,

        /// <summary>
        /// Show the standard error output in the greenshot log as warnings
        /// </summary>
        ShowErrorOutputInLog,

        /// <summary>
        /// Show the standard output in the greenshot log
        /// </summary>
        ShowStandardOutputInLog,

        /// <summary>
        /// Copy the output to the clipboard
        /// </summary>
        OutputToClipboard,
 
        /// <summary>
        /// Copy any uris to the clipboard
        /// </summary>
        UriToClipboard,

        /// <summary>
        /// This is the default
        /// </summary>
        Default = DeleteOnExit | ParseOutputForUris | ProcessReturncode | ShowErrorOutputInLog |ShowStandardOutputInLog | UriToClipboard 
    }
}
