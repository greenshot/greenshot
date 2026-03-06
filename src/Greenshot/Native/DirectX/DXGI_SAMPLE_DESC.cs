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
/// Describes the multi-sample parameters for a DirectX swap chain, including the number of samples and the quality
/// level used for anti-aliasing.
/// </summary>
/// <remarks>This structure is used to configure multi-sampling in graphics rendering. The Count field specifies
/// how many samples are used per pixel, while the Quality field indicates the quality level of those samples. Higher
/// values for Count and Quality can improve visual fidelity but may impact performance. These settings should match the
/// capabilities of the graphics device and the requirements of the application.</remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct DXGI_SAMPLE_DESC
{
    public int Count;
    public int Quality;
}