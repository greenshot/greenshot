/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Runtime.InteropServices;
using Greenshot.Base.UnmanagedHelpers.Structs;

namespace Greenshot.Base.UnmanagedHelpers.Enums
{
    /// <summary>
    /// See <a href="https://docs.microsoft.com/en-gb/windows/win32/api/dwmapi/ns-dwmapi-dwm_thumbnail_properties">DWM_THUMBNAIL_PROPERTIES</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DWM_THUMBNAIL_PROPERTIES
    {
        // A bitwise combination of DWM thumbnail constant values that indicates which members of this structure are set.
        public int dwFlags;

        // The area in the destination window where the thumbnail will be rendered.
        public RECT rcDestination;

        // The region of the source window to use as the thumbnail. By default, the entire window is used as the thumbnail.
        public RECT rcSource;

        // The opacity with which to render the thumbnail. 0 is fully transparent while 255 is fully opaque. The default value is 255.
        public byte opacity;

        // TRUE to make the thumbnail visible; otherwise, FALSE. The default is FALSE.
        public bool fVisible;

        // TRUE to use only the thumbnail source's client area; otherwise, FALSE. The default is FALSE.
        public bool fSourceClientAreaOnly;

        public RECT Destination
        {
            set
            {
                dwFlags |= DWM_TNP_RECTDESTINATION;
                rcDestination = value;
            }
        }

        public RECT Source
        {
            set
            {
                dwFlags |= DWM_TNP_RECTSOURCE;
                rcSource = value;
            }
        }

        public byte Opacity
        {
            set
            {
                dwFlags |= DWM_TNP_OPACITY;
                opacity = value;
            }
        }

        public bool Visible
        {
            set
            {
                dwFlags |= DWM_TNP_VISIBLE;
                fVisible = value;
            }
        }

        public bool SourceClientAreaOnly
        {
            set
            {
                dwFlags |= DWM_TNP_SOURCECLIENTAREAONLY;
                fSourceClientAreaOnly = value;
            }
        }

        // A value for the rcDestination member has been specified.
        public const int DWM_TNP_RECTDESTINATION = 0x00000001;

        // A value for the rcSource member has been specified.
        public const int DWM_TNP_RECTSOURCE = 0x00000002;

        // A value for the opacity member has been specified.
        public const int DWM_TNP_OPACITY = 0x00000004;

        // A value for the fVisible member has been specified.
        public const int DWM_TNP_VISIBLE = 0x00000008;

        // A value for the fSourceClientAreaOnly member has been specified.
        public const int DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010;
    }
}