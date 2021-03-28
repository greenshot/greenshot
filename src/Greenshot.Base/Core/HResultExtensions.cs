// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Diagnostics.Contracts;
using Greenshot.Base.Core.Enums;

namespace Greenshot.Base.Core
{
    /// <summary>
    ///     Extensions to handle the HResult
    /// </summary>
    public static class HResultExtensions
    {
        /// <summary>
        ///     Test if the HResult represents a fail
        /// </summary>
        /// <param name="hResult">HResult</param>
        /// <returns>bool</returns>
        [Pure]
        public static bool Failed(this HResult hResult)
        {
            return hResult < 0;
        }

        /// <summary>
        ///     Test if the HResult represents a success
        /// </summary>
        /// <param name="hResult">HResult</param>
        /// <returns>bool</returns>
        [Pure]
        public static bool Succeeded(this HResult hResult)
        {
            return hResult >= HResult.S_OK;
        }
    }
}