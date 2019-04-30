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

namespace Greenshot.Addons.Interfaces.Drawing
{
	/// <summary>
	/// Flags for a field, which define a behavior
	/// </summary>
	[Flags]
	public enum FieldFlag
	{
        /// <summary>
        /// No flags
        /// </summary>
		None = 0,
        /// <summary>
        /// This field needs to be confirmed
        /// </summary>
		Confirmable = 1,
        /// <summary>
        /// This field is a counter
        /// </summary>
		Counter = 2
	}
}