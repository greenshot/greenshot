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
/// Represents a DirectX Graphics Infrastructure (DXGI) device used to create and manage Direct3D resources.
/// </summary>
/// <remarks>This interface enables interaction with graphics devices and facilitates resource management and
/// coordination with the Direct3D runtime. It is typically used in graphics applications to support rendering
/// operations and device-level resource handling.</remarks>
[ComImport]
[Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDXGIDevice { }
