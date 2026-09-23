/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
/// COM interop interface for Windows.Graphics.Capture.IGraphicsCaptureSession3.
/// Introduced in Windows 11 Build 22000 (UniversalApiContract v12.0) to control
/// the capture border.
/// </summary>
[ComImport]
[Guid("F2CDD966-22AE-5EA1-9596-3A289344C3BE")]
[InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
internal interface IGraphicsCaptureSession3
{
    bool IsBorderRequired { get; set; }
}
