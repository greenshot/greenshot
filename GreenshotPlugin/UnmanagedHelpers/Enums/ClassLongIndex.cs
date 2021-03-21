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
    public enum ClassLongIndex
    {
        GCL_CBCLSEXTRA = -20, // the size, in bytes, of the extra memory associated with the class. Setting this value does not change the number of extra bytes already allocated.
        GCL_CBWNDEXTRA = -18, // the size, in bytes, of the extra window memory associated with each window in the class. Setting this value does not change the number of extra bytes already allocated. For information on how to access this memory, see SetWindowLong.
        GCL_HBRBACKGROUND = -10, // a handle to the background brush associated with the class.
        GCL_HCURSOR = -12, // a handle to the cursor associated with the class.
        GCL_HICON = -14, // a handle to the icon associated with the class.
        GCL_HICONSM = -34, // a handle to the small icon associated with the class.
        GCL_HMODULE = -16, // a handle to the module that registered the class.
        GCL_MENUNAME = -8, // the address of the menu name string. The string identifies the menu resource associated with the class.
        GCL_STYLE = -26, // the window-class style bits.
        GCL_WNDPROC = -24, // the address of the window procedure, or a handle representing the address of the window procedure. You must use the CallWindowProc function to call the window procedure. 
    }
}