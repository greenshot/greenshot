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
/// Represents a Direct3D 11 device that provides methods for creating and managing graphics resources such as buffers
/// and textures.
/// </summary>
/// <remarks>This interface is essential for initializing and handling Direct3D resources in graphics
/// applications. It enables the creation of various resource types required for rendering operations and serves as the
/// entry point for resource management in Direct3D 11. Typically, instances of this interface are obtained through
/// Direct3D initialization routines and are used throughout the application's lifetime to allocate and manage GPU
/// resources.</remarks>
[ComImport]
[Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ID3D11Device
{
    void CreateBuffer();
    void CreateTexture1D();
    void CreateTexture2D([In] ref D3D11_TEXTURE2D_DESC pDesc, [In] IntPtr pInitialData, [MarshalAs(UnmanagedType.Interface)] out ID3D11Texture2D ppTexture2D);
}
