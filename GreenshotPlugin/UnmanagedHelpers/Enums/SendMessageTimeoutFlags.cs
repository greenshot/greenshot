﻿/*
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

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    /// <summary>
    /// See: http://www.pinvoke.net/default.aspx/Enums/SendMessageTimeoutFlags.html
    /// </summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum SendMessageTimeoutFlags : uint {
        SMTO_NORMAL				= 0x0,
        SMTO_BLOCK				= 0x1,
        SMTO_ABORTIFHUNG		= 0x2,
        SMTO_NOTIMEOUTIFNOTHUNG	= 0x8
    }
}