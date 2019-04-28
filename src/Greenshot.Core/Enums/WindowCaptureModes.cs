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

namespace Greenshot.Core.Enums
{
    /// <summary>
    /// These are the possible ways to capture a window
    /// </summary>
	public enum WindowCaptureModes
	{
        /// <summary>
        /// USe the screen
        /// </summary>
		Screen,
        /// <summary>
        /// Use GDI "print"
        /// </summary>
		Gdi,
        /// <summary>
        /// Use Aero to clone the window and the screen to capture.
        /// This mode will remove transparency
        /// </summary>
		Aero,
        /// <summary>
        /// Use Aero to clone the window and the screen to capture.
        /// This mode will maintain transparency
        /// </summary>
		AeroTransparent,
        /// <summary>
        /// Automatically select the modus
        /// </summary>
		Auto
	}
}