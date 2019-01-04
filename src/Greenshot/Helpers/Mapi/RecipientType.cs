﻿#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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


#endregion

namespace Greenshot.Helpers.Mapi
{
    /// <summary>
    ///     Specifies the valid RecipientTypes for a Recipient.
    /// </summary>
    public enum RecipientType
    {
        /// <summary>
        ///     Recipient will be in the TO list.
        /// </summary>
        To = 1,

        /// <summary>
        ///     Recipient will be in the CC list.
        /// </summary>
        CC = 2,

        /// <summary>
        ///     Recipient will be in the BCC list.
        /// </summary>
        BCC = 3
    }
}