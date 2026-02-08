/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Native.DirectX;

/// <summary>
/// Defines flags that specify CPU access options for Direct3D 11 resources.
/// </summary>
/// <remarks>This enumeration is used to indicate the type of access that the CPU has to a resource, which can
/// affect performance and resource management. The flags can be combined to specify multiple access types.</remarks>
internal enum D3D11_CPU_ACCESS_FLAG
{
    D3D11_CPU_ACCESS_WRITE = 0x10000,
    D3D11_CPU_ACCESS_READ = 0x20000
}