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
/// Represents a Direct3D 11 device context, providing methods for issuing rendering commands and managing graphics
/// resources within the Direct3D pipeline.
/// </summary>
/// <remarks>The ID3D11DeviceContext interface enables interaction with the Direct3D 11 rendering pipeline,
/// including setting shaders, drawing geometry, and manipulating resource states. It is typically used in conjunction
/// with an ID3D11Device to execute graphics operations and manage GPU resources. This interface is essential for
/// performing tasks such as rendering, resource mapping, and configuring pipeline stages in Direct3D 11
/// applications.</remarks>
[ComImport]
[Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ID3D11DeviceContext
{
    // --- ID3D11DeviceChild Methods (Inherited) ---
    // These placeholders ensure the VTable offsets are correct (Slots 3, 4, 5, 6)
    void GetDevice();
    void GetPrivateData();
    void SetPrivateData();
    void SetPrivateDataInterface();

    // --- ID3D11DeviceContext Methods (Starts at Slot 7) ---
    void VSSetConstantBuffers();
    void PSSetShaderResources();
    void PSSetShader();
    void PSSetSamplers();
    void VSSetShader();
    void DrawIndexed();
    void Draw();

    // Map is Slot 14 (relative to DeviceContext start at 7? No, standard indexes map sequentially)
    // IUnknown (3) + DeviceChild (4) + DeviceContext methods...
    // Map is the 8th method defined in ID3D11DeviceContext itself.
    // 7 + 7 = 14. This is correct.

    [PreserveSig]
    int Map([MarshalAs(UnmanagedType.Interface)] ID3D11Resource pResource, int Subresource, D3D11_MAP MapType, int MapFlags, out D3D11_MAPPED_SUBRESOURCE pMappedResource);

    void Unmap([MarshalAs(UnmanagedType.Interface)] ID3D11Resource pResource, int Subresource);

    void PSSetConstantBuffers();
    void IASetInputLayout();
    void IASetVertexBuffers();
    void IASetIndexBuffer();
    void DrawIndexedInstanced();
    void DrawInstanced();
    void GSSetConstantBuffers();
    void GSSetShader();
    void IASetPrimitiveTopology();
    void VSSetShaderResources();
    void VSSetSamplers();
    void Begin();
    void End();
    void GetData();
    void SetPredication();
    void GSSetShaderResources();
    void GSSetSamplers();
    void OMSetRenderTargets();
    void OMSetRenderTargetsAndUnorderedAccessViews();
    void OMSetBlendState();
    void OMSetDepthStencilState();
    void SOSetTargets();
    void DrawAuto();
    void DrawIndexedInstancedIndirect();
    void DrawInstancedIndirect();
    void Dispatch();
    void DispatchIndirect();
    void RSSetState();
    void RSSetViewports();
    void RSSetScissorRects();
    void CopySubresourceRegion();

    void CopyResource([MarshalAs(UnmanagedType.Interface)] ID3D11Resource pDstResource, [MarshalAs(UnmanagedType.Interface)] ID3D11Resource pSrcResource);
}