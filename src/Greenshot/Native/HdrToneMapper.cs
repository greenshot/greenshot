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
using Greenshot.Native.DirectX;
using log4net;

namespace Greenshot.Native;

/// <summary>
/// GPU-accelerated HDR-to-SDR tone mapper using Direct2D effects via raw COM vtable calls.
/// </summary>
/// <remarks>
/// <para>
/// Creates a D2D device and device context from the same D3D11 device used for capture,
/// then applies the built-in <c>CLSID_D2D1WhiteLevelAdjustment</c> effect to scale scRGB
/// linear content by the SDR white level ratio, rendering the result into an 8-bit
/// <c>B8G8R8A8_UNORM</c> texture. D2D automatically applies the sRGB transfer function
/// when rendering to the 8-bit target.
/// </para>
/// <para>
/// Because ID2D1DeviceContext inherits from ID2D1RenderTarget (53+ methods), declaring
/// a full <c>[ComImport]</c> interface would require 80+ vtable stubs. Instead, this class
/// uses raw vtable pointer arithmetic via <c>Marshal.GetDelegateForFunctionPointer</c>,
/// which is compact and avoids vtable-ordering errors.
/// </para>
/// </remarks>
internal sealed class HdrToneMapper : IDisposable
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(HdrToneMapper));

    // COM pointers (raw IntPtr, released via Marshal.Release)
    private IntPtr _pD2D1Device;
    private IntPtr _pDeviceContext;
    private bool _disposed;

    #region GUIDs

    private static readonly Guid IID_IDXGISurface = new Guid("cafcb56c-6ac3-4889-bf47-9e23bbd260ec");

    // From d2d1effects_2.h (Windows SDK 10.0.22621.0):
    // DEFINE_GUID(CLSID_D2D1WhiteLevelAdjustment, 0x44a1cadb, 0x6cdd, 0x4818, 0x8f, 0xf4, 0x26, 0xc1, 0xcf, 0xe9, 0x5b, 0xdb);
    private static readonly Guid CLSID_D2D1WhiteLevelAdjustment = new Guid("44a1cadb-6cdd-4818-8ff4-26c1cfe95bdb");

    #endregion

    #region Constants

    // DXGI_FORMAT values (D3D11_TEXTURE2D_DESC.Format is int)
    private const int DXGI_FORMAT_R16G16B16A16_FLOAT = 10;
    private const int DXGI_FORMAT_B8G8R8A8_UNORM = 87;
    private const int DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91;

    // D2D1_ALPHA_MODE
    private const int D2D1_ALPHA_MODE_PREMULTIPLIED = 1;

    // D2D1_BITMAP_OPTIONS
    private const int D2D1_BITMAP_OPTIONS_NONE = 0;
    private const int D2D1_BITMAP_OPTIONS_TARGET = 1;

    // D2D1_INTERPOLATION_MODE
    private const int D2D1_INTERPOLATION_MODE_LINEAR = 1;

    // D2D1_COMPOSITE_MODE
    private const int D2D1_COMPOSITE_MODE_SOURCE_OVER = 0;

    // D2D1_PROPERTY_TYPE
    private const int D2D1_PROPERTY_TYPE_UNKNOWN = 0;
    private const int D2D1_PROPERTY_TYPE_FLOAT = 5;

    // D3D11_BIND_FLAG
    private const int D3D11_BIND_RENDER_TARGET = 0x20;
    private const int D3D11_BIND_SHADER_RESOURCE = 0x8;

    // D2D1_WHITELEVELADJUSTMENT_PROP
    private const uint D2D1_WHITELEVELADJUSTMENT_PROP_INPUT_WHITE_LEVEL = 0;
    private const uint D2D1_WHITELEVELADJUSTMENT_PROP_OUTPUT_WHITE_LEVEL = 1;

    // Vtable slot indices (0-indexed from IUnknown)
    //
    // ID2D1Device inherits ID2D1Resource (IUnknown[0-2] + GetFactory[3])
    private const int VT_D2D1Device_CreateDeviceContext = 4;

    // ID2D1DeviceContext inherits ID2D1RenderTarget (IUnknown[0-2] + ID2D1Resource[3] + ID2D1RenderTarget[4-56])
    private const int VT_D2D1DC_BeginDraw = 48;
    private const int VT_D2D1DC_EndDraw = 49;
    // ID2D1DeviceContext own methods start at slot 57
    private const int VT_D2D1DC_CreateBitmapFromDxgiSurface = 62;
    private const int VT_D2D1DC_CreateEffect = 63;
    private const int VT_D2D1DC_SetTarget = 74;
    private const int VT_D2D1DC_DrawImage = 83;

    // ID2D1Effect inherits ID2D1Properties (IUnknown[0-2] + ID2D1Properties[3-13])
    private const int VT_D2D1Effect_SetValue = 9;
    private const int VT_D2D1Effect_SetInput = 14;
    private const int VT_D2D1Effect_GetOutput = 18;

    #endregion

    #region P/Invoke

    [DllImport("d2d1.dll")]
    private static extern int D2D1CreateDevice(
        IntPtr dxgiDevice,
        IntPtr creationProperties,
        out IntPtr d2dDevice);

    #endregion

    #region Vtable delegate types

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateDeviceContextDelegate(IntPtr pThis, int options, out IntPtr deviceContext);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void BeginDrawDelegate(IntPtr pThis);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EndDrawDelegate(IntPtr pThis, IntPtr tag1, IntPtr tag2);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateBitmapFromDxgiSurfaceDelegate(
        IntPtr pThis, IntPtr surface, ref D2D1BitmapProperties1 props, out IntPtr bitmap);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateEffectDelegate(IntPtr pThis, ref Guid effectId, out IntPtr effect);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void SetTargetDelegate(IntPtr pThis, IntPtr image);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void DrawImageDelegate(
        IntPtr pThis, IntPtr image, IntPtr targetOffset, IntPtr imageRect,
        int interpolationMode, int compositeMode);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EffectSetValueDelegate(IntPtr pThis, uint index, int type, IntPtr data, uint dataSize);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void EffectSetInputDelegate(IntPtr pThis, uint index, IntPtr input, int invalidate);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void EffectGetOutputDelegate(IntPtr pThis, out IntPtr outputImage);

    #endregion

    #region D2D1 structs

    [StructLayout(LayoutKind.Sequential)]
    private struct D2D1PixelFormat
    {
        public int Format;
        public int AlphaMode;
    }

    /// <summary>
    /// Sequential layout automatically matches native alignment for both 32-bit (24 bytes)
    /// and 64-bit (32 bytes with 4-byte padding before IntPtr).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct D2D1BitmapProperties1
    {
        public D2D1PixelFormat PixelFormat;
        public float DpiX;
        public float DpiY;
        public int BitmapOptions;
        public IntPtr ColorContext;
    }

    #endregion

    /// <summary>
    /// Creates a new <see cref="HdrToneMapper"/> backed by the given D3D11 device.
    /// </summary>
    /// <param name="d3dDevice">The D3D11 device used for capture. Must be the same device
    /// that owns the textures passed to <see cref="ToneMapToSdr"/>.</param>
    public HdrToneMapper(ID3D11Device d3dDevice)
    {
        // Get IDXGIDevice from ID3D11Device via COM QueryInterface
        var dxgiDevice = (IDXGIDevice)d3dDevice;
        IntPtr pDxgiDevice = Marshal.GetComInterfaceForObject(dxgiDevice, typeof(IDXGIDevice));
        try
        {
            // Create D2D1 Device from DXGI Device
            int hr = D2D1CreateDevice(pDxgiDevice, IntPtr.Zero, out _pD2D1Device);
            Marshal.ThrowExceptionForHR(hr);

            // Create D2D1 Device Context (D2D1_DEVICE_CONTEXT_OPTIONS_NONE = 0)
            var createDC = GetVtableDelegate<CreateDeviceContextDelegate>(
                _pD2D1Device, VT_D2D1Device_CreateDeviceContext);
            hr = createDC(_pD2D1Device, 0, out _pDeviceContext);
            Marshal.ThrowExceptionForHR(hr);

            Log.Debug("D2D1 device context created successfully for HDR tone mapping.");
        }
        catch
        {
            Dispose();
            throw;
        }
        finally
        {
            Marshal.Release(pDxgiDevice);
        }
    }

    /// <summary>
    /// Tone-maps an FP16 HDR texture to an 8-bit SDR texture using the D2D1 WhiteLevelAdjustment effect.
    /// </summary>
    /// <param name="hdrTexture">The captured FP16 (<c>R16G16B16A16_FLOAT</c>) texture.</param>
    /// <param name="d3dDevice">The D3D11 device to create the render target texture on.</param>
    /// <param name="sdrWhiteLevelInNits">The SDR white level in nits for the captured display.</param>
    /// <param name="width">Texture width in pixels.</param>
    /// <param name="height">Texture height in pixels.</param>
    /// <returns>A new <c>B8G8R8A8_UNORM</c> texture containing the tone-mapped SDR result.
    /// The caller owns this texture and must release it via <c>Marshal.ReleaseComObject</c>.</returns>
    public ID3D11Texture2D ToneMapToSdr(
        ID3D11Texture2D hdrTexture, ID3D11Device d3dDevice,
        float sdrWhiteLevelInNits, int width, int height)
    {
        IntPtr pHdrSurface = IntPtr.Zero;
        IntPtr pSdrSurface = IntPtr.Zero;
        IntPtr pSourceBitmap = IntPtr.Zero;
        IntPtr pTargetBitmap = IntPtr.Zero;
        IntPtr pEffect = IntPtr.Zero;
        IntPtr pEffectOutput = IntPtr.Zero;
        ID3D11Texture2D sdrTexture = null;

        try
        {
            // 1. Create 8-bit render target texture with hardware sRGB encoding
            var sdrDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE,
                CPUAccessFlags = (D3D11_CPU_ACCESS_FLAG)0,
                MiscFlags = 0
            };
            d3dDevice.CreateTexture2D(ref sdrDesc, IntPtr.Zero, out sdrTexture);

            // 2. Get IDXGISurface from both textures via QueryInterface
            pHdrSurface = QueryComInterface(hdrTexture, IID_IDXGISurface);
            pSdrSurface = QueryComInterface(sdrTexture, IID_IDXGISurface);

            // 3. Create D2D bitmaps wrapping the DXGI surfaces
            var createBitmapFn = GetVtableDelegate<CreateBitmapFromDxgiSurfaceDelegate>(_pDeviceContext, VT_D2D1DC_CreateBitmapFromDxgiSurface);

            var sourceProps = new D2D1BitmapProperties1
            {
                PixelFormat = new D2D1PixelFormat
                {
                    Format = DXGI_FORMAT_R16G16B16A16_FLOAT,
                    AlphaMode = D2D1_ALPHA_MODE_PREMULTIPLIED
                },
                DpiX = 96.0f,
                DpiY = 96.0f,
                BitmapOptions = D2D1_BITMAP_OPTIONS_NONE,
                ColorContext = IntPtr.Zero
            };
            int hr = createBitmapFn(_pDeviceContext, pHdrSurface, ref sourceProps, out pSourceBitmap);
            Marshal.ThrowExceptionForHR(hr);

            var targetProps = new D2D1BitmapProperties1
            {
                PixelFormat = new D2D1PixelFormat
                {
                    Format = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,
                    AlphaMode = D2D1_ALPHA_MODE_PREMULTIPLIED
                },
                DpiX = 96.0f,
                DpiY = 96.0f,
                BitmapOptions = D2D1_BITMAP_OPTIONS_TARGET,
                ColorContext = IntPtr.Zero
            };
            hr = createBitmapFn(_pDeviceContext, pSdrSurface, ref targetProps, out pTargetBitmap);
            Marshal.ThrowExceptionForHR(hr);

            // 4. Create the WhiteLevelAdjustment effect
            Guid clsid = CLSID_D2D1WhiteLevelAdjustment;
            var createEffectFn = GetVtableDelegate<CreateEffectDelegate>(_pDeviceContext, VT_D2D1DC_CreateEffect);
            hr = createEffectFn(_pDeviceContext, ref clsid, out pEffect);
            Marshal.ThrowExceptionForHR(hr);

            // 5. Configure effect: input white level = display SDR nits, output = 80 nits (sRGB reference)
            var setValueFn = GetVtableDelegate<EffectSetValueDelegate>(pEffect, VT_D2D1Effect_SetValue);
            unsafe
            {
                float inputWhiteLevel = 80.0f;
                float outputWhiteLevel = sdrWhiteLevelInNits;

                hr = setValueFn(pEffect, D2D1_WHITELEVELADJUSTMENT_PROP_INPUT_WHITE_LEVEL, D2D1_PROPERTY_TYPE_FLOAT, (IntPtr)(&inputWhiteLevel), sizeof(float));
                Marshal.ThrowExceptionForHR(hr);

                hr = setValueFn(pEffect, D2D1_WHITELEVELADJUSTMENT_PROP_OUTPUT_WHITE_LEVEL, D2D1_PROPERTY_TYPE_FLOAT, (IntPtr)(&outputWhiteLevel), sizeof(float));
                Marshal.ThrowExceptionForHR(hr);
            }

            // 6. Wire effect input → source bitmap
            var setInputFn = GetVtableDelegate<EffectSetInputDelegate>(pEffect, VT_D2D1Effect_SetInput);
            setInputFn(pEffect, 0, pSourceBitmap, 1 /* TRUE = invalidate */);

            // 7. Get effect output image
            var getOutputFn = GetVtableDelegate<EffectGetOutputDelegate>(pEffect, VT_D2D1Effect_GetOutput);
            getOutputFn(pEffect, out pEffectOutput);

            // 8. Set target, draw, finalize
            var setTargetFn = GetVtableDelegate<SetTargetDelegate>(_pDeviceContext, VT_D2D1DC_SetTarget);
            setTargetFn(_pDeviceContext, pTargetBitmap);

            var beginDrawFn = GetVtableDelegate<BeginDrawDelegate>(_pDeviceContext, VT_D2D1DC_BeginDraw);
            beginDrawFn(_pDeviceContext);

            var drawImageFn = GetVtableDelegate<DrawImageDelegate>(_pDeviceContext, VT_D2D1DC_DrawImage);
            drawImageFn(_pDeviceContext, pEffectOutput,
                IntPtr.Zero /* targetOffset = origin */,
                IntPtr.Zero /* imageRect = full image */,
                D2D1_INTERPOLATION_MODE_LINEAR,
                D2D1_COMPOSITE_MODE_SOURCE_OVER);

            var endDrawFn = GetVtableDelegate<EndDrawDelegate>(_pDeviceContext, VT_D2D1DC_EndDraw);
            hr = endDrawFn(_pDeviceContext, IntPtr.Zero, IntPtr.Zero);
            Marshal.ThrowExceptionForHR(hr);

            Log.Debug($"GPU tone-mapping completed: {width}x{height}, SDR white = {sdrWhiteLevelInNits} nits.");

            // 9. Transfer ownership to caller
            var result = sdrTexture;
            sdrTexture = null; // Prevent release in finally
            return result;
        }
        finally
        {
            if (pEffectOutput != IntPtr.Zero) Marshal.Release(pEffectOutput);
            if (pEffect != IntPtr.Zero) Marshal.Release(pEffect);
            if (pTargetBitmap != IntPtr.Zero) Marshal.Release(pTargetBitmap);
            if (pSourceBitmap != IntPtr.Zero) Marshal.Release(pSourceBitmap);
            if (pSdrSurface != IntPtr.Zero) Marshal.Release(pSdrSurface);
            if (pHdrSurface != IntPtr.Zero) Marshal.Release(pHdrSurface);
            if (sdrTexture != null) Marshal.ReleaseComObject(sdrTexture);
        }
    }

    #region Helpers

    /// <summary>
    /// Queries a COM object for a specific interface, returning a raw IntPtr.
    /// The caller must call <c>Marshal.Release</c> on the returned pointer.
    /// </summary>
    private static IntPtr QueryComInterface(object comObject, Guid iid)
    {
        IntPtr pUnk = Marshal.GetIUnknownForObject(comObject);
        try
        {
            int hr = Marshal.QueryInterface(pUnk, ref iid, out IntPtr pInterface);
            Marshal.ThrowExceptionForHR(hr);
            return pInterface;
        }
        finally
        {
            Marshal.Release(pUnk);
        }
    }

    /// <summary>
    /// Reads a function pointer from a COM object's vtable at the given slot index
    /// and marshals it to a managed delegate.
    /// </summary>
    private static unsafe T GetVtableDelegate<T>(IntPtr comObject, int slot) where T : Delegate
    {
        IntPtr vtable = *(IntPtr*)comObject;
        IntPtr method = *((IntPtr*)vtable + slot);
        return Marshal.GetDelegateForFunctionPointer<T>(method);
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_pDeviceContext != IntPtr.Zero)
        {
            Marshal.Release(_pDeviceContext);
            _pDeviceContext = IntPtr.Zero;
        }

        if (_pD2D1Device != IntPtr.Zero)
        {
            Marshal.Release(_pD2D1Device);
            _pD2D1Device = IntPtr.Zero;
        }
    }
}
