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

using System;
using System.Runtime.InteropServices;

namespace Greenshot.Native.DirectX;

/// <summary>
/// Represents a two-dimensional texture resource used in Direct3D 11 for storing and manipulating image data.
/// </summary>
/// <remarks>This interface provides methods to retrieve the texture's description and manage its resource
/// properties. It is essential for rendering operations that require texture mapping in graphics
/// applications.</remarks>
[ComImport]
[Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ID3D11Texture2D : ID3D11Resource
{
    // ID3D11DeviceChild methods (inherited)
    void GetDevice();
    void GetPrivateData();
    void SetPrivateData();
    void SetPrivateDataInterface();

    // ID3D11Resource methods (inherited)
    void GetType();
    void SetEvictionPriority();
    void GetEvictionPriority();

    // ID3D11Texture2D methods
    void GetDesc(out D3D11_TEXTURE2D_DESC pDesc);
}
