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

using System;
using System.Diagnostics.CodeAnalysis;

namespace Greenshot.Base.UnmanagedHelpers.Enums
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/aa909766.aspx
    /// </summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum SoundFlags
    {
        SND_ASYNC = 0x0001, // play asynchronously
        SND_MEMORY = 0x0004, // pszSound points to a memory file
        SND_NOSTOP = 0x0010, // don't stop any currently playing sound
        SND_NOWAIT = 0x00002000, // don't wait if the driver is busy
    }
}