/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowLongIndex
    {
        GWL_EXSTYLE = -20,	// Sets a new extended window style.
        GWL_HINSTANCE = -6,	// Sets a new application instance handle.
        GWL_ID = -12,	// Sets a new identifier of the child window. The window cannot be a top-level window.
        GWL_STYLE = -16,	// Sets a new window style.
        GWL_USERDATA = -21,	// Sets the user data associated with the window. This data is intended for use by the application that created the window. Its value is initially zero.
        GWL_WNDPROC = -4 // Sets a new address for the window procedure. You cannot change this attribute if the window does not belong to the same process as the calling thread.
    }
}