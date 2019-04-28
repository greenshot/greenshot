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

namespace Greenshot.Addons.Components
{
    /// <summary>
    /// This is used to order the destinations
    /// </summary>
    public enum DestinationOrder
    {
        /// <summary>
        /// Order for the File without a dialog destination
        /// </summary>
        FileNoDialog = 0,
        /// <summary>
        /// Order for the file with dialog destination
        /// </summary>
        FileDialog = 0,
        /// <summary>
        /// Order for the picker destination
        /// </summary>
        Picker = 1,
        /// <summary>
        /// Order for the Printer destination
        /// </summary>
        Printer = 2,
        /// <summary>
        /// Order for the Clipboard destination
        /// </summary>
        Clipboard = 2,
        /// <summary>
        /// Order for the Email destination
        /// </summary>
        Email = 3,
        /// <summary>
        /// Order for the Outlook destination
        /// </summary>
        Outlook = 3,
        /// <summary>
        /// Order for the Word destination
        /// </summary>
        Word = 4,
        /// <summary>
        /// Order for the PowerPoint destination
        /// </summary>
        Powerpoint = 4,
        /// <summary>
        /// Order for the OneNote destination
        /// </summary>
        OneNote = 4,
        /// <summary>
        /// Order for the Excel destination
        /// </summary>
        Excel = 5,
        
    }
}
