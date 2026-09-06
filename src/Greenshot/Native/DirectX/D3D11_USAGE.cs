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
/// Specifies the usage options for Direct3D 11 resources, indicating how resources are intended to be accessed and
/// managed by the CPU and GPU.
/// </summary>
/// <remarks>Each value of the D3D11_USAGE enumeration defines a distinct resource usage pattern:
/// D3D11_USAGE_DEFAULT is optimized for GPU access and is suitable for resources that are updated infrequently;
/// D3D11_USAGE_IMMUTABLE is for resources that are set once and never changed; D3D11_USAGE_DYNAMIC allows frequent
/// updates from the CPU, typically for resources that change often; D3D11_USAGE_STAGING is used for resources that are
/// read from or written to by the CPU, often for data transfer operations. The selected usage affects performance
/// characteristics and determines which operations are permitted on the resource.</remarks>
internal enum D3D11_USAGE
{
    D3D11_USAGE_DEFAULT = 0,
    D3D11_USAGE_IMMUTABLE = 1,
    D3D11_USAGE_DYNAMIC = 2,
    D3D11_USAGE_STAGING = 3
}