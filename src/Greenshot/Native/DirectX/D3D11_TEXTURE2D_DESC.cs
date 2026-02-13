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

using System.Runtime.InteropServices;

namespace Greenshot.Native.DirectX;

/// <summary>
/// Describes the properties of a two-dimensional texture resource used in Direct3D 11, including its dimensions,
/// format, mip levels, array size, sampling options, usage, binding, CPU access, and miscellaneous flags.
/// </summary>
/// <remarks>This structure is used to configure and define the characteristics of a 2D texture when creating
/// resources in Direct3D 11. It allows developers to specify how the texture will be stored, accessed, and utilized
/// within the graphics pipeline. The fields correspond to key aspects such as width, height, pixel format, number of
/// mipmap levels, array slices, multisampling settings, intended usage, binding options, CPU access permissions, and
/// additional resource flags. Proper configuration of these properties is essential for optimal performance and
/// compatibility in graphics applications.</remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct D3D11_TEXTURE2D_DESC
{
    public int Width;
    public int Height;
    public int MipLevels;
    public int ArraySize;
    public int Format;
    public DXGI_SAMPLE_DESC SampleDesc;
    public D3D11_USAGE Usage;
    public int BindFlags;
    public D3D11_CPU_ACCESS_FLAG CPUAccessFlags;
    public int MiscFlags;
}