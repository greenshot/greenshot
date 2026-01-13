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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace Greenshot.Native
{
    internal class WindowsGraphicsCaptureInterop
    {
        // Constants
        private const int D3D11_SDK_VERSION = 7;
        private const int D3D_DRIVER_TYPE_HARDWARE = 1;
        private const int D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;

        public static Bitmap CaptureWindowToBitmap(IntPtr window)
        {
            CreateD3D11Device(out var device, out var context);

            try
            {
                var texture = CaptureWindowToTexture2D(window, device);

                try
                {
                    return TransformTextureToBitmap(texture, device, context);
                }
                finally
                {
                    if (texture != null) Marshal.ReleaseComObject(texture);
                }
            }
            finally
            {
                if (context != null) Marshal.ReleaseComObject(context);
                if (device != null) Marshal.ReleaseComObject(device);
            }
        }

        public static Bitmap CaptureMonitorToBitmap(IntPtr hMonitor)
        {
            CreateD3D11Device(out var device, out var context);

            try
            {
                var texture = CaptureMonitorToTexture2D(hMonitor, device);
                try
                {
                    return TransformTextureToBitmap(texture, device, context);
                }
                finally
                {
                    if (texture != null) Marshal.ReleaseComObject(texture);
                }
            }
            finally
            {
                if (context != null) Marshal.ReleaseComObject(context);
                if (device != null) Marshal.ReleaseComObject(device);
            }
        }

        private static ID3D11Texture2D CaptureWindowToTexture2D(IntPtr window, ID3D11Device d3d11Device)
        {
            var captureItem = CreateCaptureItemForWindow(window);
            var device = CreateID3DDeviceFromD3D11Device(d3d11Device);

            using var framePool = Direct3D11CaptureFramePool.Create(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, captureItem.Size);
            var session = framePool.CreateCaptureSession(captureItem);
            //session.IsBorderRequired = false;
            session.IsCursorCaptureEnabled = false;
            session.StartCapture();

            Direct3D11CaptureFrame frameTemp = null;
            while (frameTemp == null)
            {
                frameTemp = framePool.TryGetNextFrame();
            }

            using var frame = frameTemp;
            return CreateTexture2DFromID3DSurface(frame.Surface);
        }

        private static ID3D11Texture2D CaptureMonitorToTexture2D(IntPtr hMonitor, ID3D11Device d3d11Device)
        {
            var captureItem = CreateCaptureItemForMonitor(hMonitor);
            var device = CreateID3DDeviceFromD3D11Device(d3d11Device);

            using var framePool = Direct3D11CaptureFramePool.Create(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, captureItem.Size);
            var session = framePool.CreateCaptureSession(captureItem);
            //session.IsBorderRequired = false;
            session.IsCursorCaptureEnabled = false;
            session.StartCapture();

            Direct3D11CaptureFrame frameTemp = null;
            while (frameTemp == null)
            {
                frameTemp = framePool.TryGetNextFrame();
            }

            using var frame = frameTemp;
            return CreateTexture2DFromID3DSurface(frame.Surface);
        }

        private static void CreateD3D11Device(out ID3D11Device device, out ID3D11DeviceContext context)
        {
            int hr = D3D11CreateDevice(
                IntPtr.Zero,
                D3D_DRIVER_TYPE_HARDWARE,
                IntPtr.Zero,
                D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                null,
                0,
                D3D11_SDK_VERSION,
                out device,
                out _,
                out context);

            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        }

        // --- WinRT Interop Helpers ---

        [ComImport]
        [ComVisible(true)]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
            IntPtr CreateForMonitor([In] IntPtr hMonitor, [In] ref Guid iid);
        }

        private static readonly Guid GraphicsCaptureItemGuid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");

        private static GraphicsCaptureItem CreateCaptureItemForWindow(IntPtr window)
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var itemPointer = interop.CreateForWindow(window, GraphicsCaptureItemGuid);
            var item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
            Marshal.Release(itemPointer);
            return item;
        }

        private static GraphicsCaptureItem CreateCaptureItemForMonitor(IntPtr hMonitor)
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var itemPointer = interop.CreateForMonitor(hMonitor, GraphicsCaptureItemGuid);
            var item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
            Marshal.Release(itemPointer);
            return item;
        }

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern UInt32 CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int D3D11CreateDevice(
            IntPtr pAdapter,
            int driverType,
            IntPtr software,
            int flags,
            IntPtr[] pFeatureLevels,
            int featureLevels,
            int sdkVersion,
            out ID3D11Device ppDevice,
            out int pFeatureLevel,
            out ID3D11DeviceContext ppImmediateContext);

        private static IDirect3DDevice CreateID3DDeviceFromD3D11Device(ID3D11Device d3dDevice)
        {
            IDirect3DDevice device = null;
            var dxgiDevice = (IDXGIDevice)d3dDevice;
            IntPtr pDxgiDevice = Marshal.GetComInterfaceForObject(dxgiDevice, typeof(IDXGIDevice));

            try
            {
                var hr = CreateDirect3D11DeviceFromDXGIDevice(pDxgiDevice, out IntPtr pUnknown);
                if (hr == 0)
                {
                    device = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DDevice;
                    Marshal.Release(pUnknown);
                }
                else
                {
                    Marshal.ThrowExceptionForHR((int)hr);
                }
                return device;
            }
            finally
            {
                Marshal.Release(pDxgiDevice);
            }
        }

        [ComImport]
        [ComVisible(true)]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        };

        private static readonly Guid IID_ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        private static ID3D11Texture2D CreateTexture2DFromID3DSurface(IDirect3DSurface surface)
        {
            var access = (IDirect3DDxgiInterfaceAccess)surface;
            var d3dPointer = access.GetInterface(IID_ID3D11Texture2D);
            var d3dSurface = (ID3D11Texture2D)Marshal.GetObjectForIUnknown(d3dPointer);
            Marshal.Release(d3dPointer);
            return d3dSurface;
        }

        private static unsafe Bitmap TransformTextureToBitmap(ID3D11Texture2D texture, ID3D11Device device, ID3D11DeviceContext context)
        {
            D3D11_TEXTURE2D_DESC desc;
            texture.GetDesc(out desc);

            var width = desc.Width;
            var height = desc.Height;
            long bytesPerRow = (long)width * 4;

            var stagingDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = desc.Format,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
                BindFlags = 0,
                CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                MiscFlags = 0
            };

            device.CreateTexture2D(ref stagingDesc, IntPtr.Zero, out var textureCopy);

            try
            {
                context.CopyResource(textureCopy, texture);

                // Check HRESULT of Map
                int hr = context.Map(textureCopy, 0, D3D11_MAP.D3D11_MAP_READ, 0, out var mappedResource);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);

                try
                {
                    Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

                    byte* sourcePtr = (byte*)mappedResource.pData;
                    byte* destPtr = (byte*)bmpData.Scan0;

                    for (int y = 0; y < height; y++)
                    {
                        // Use Buffer.MemoryCopy for fast, safe unmanaged copy
                        Buffer.MemoryCopy(sourcePtr, destPtr, bytesPerRow, bytesPerRow);

                        sourcePtr += mappedResource.RowPitch;
                        destPtr += bmpData.Stride;
                    }

                    bitmap.UnlockBits(bmpData);
                    return bitmap;
                }
                finally
                {
                    context.Unmap(textureCopy, 0);
                }
            }
            finally
            {
                if (textureCopy != null) Marshal.ReleaseComObject(textureCopy);
            }
        }

        // --- Native Direct3D 11 Definitions ---

        [ComImport]
        [Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ID3D11Device
        {
            void CreateBuffer();
            void CreateTexture1D();
            void CreateTexture2D([In] ref D3D11_TEXTURE2D_DESC pDesc, [In] IntPtr pInitialData, [MarshalAs(UnmanagedType.Interface)] out ID3D11Texture2D ppTexture2D);
        }

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

        [ComImport]
        [Guid("dc8e63f3-d12b-4952-b47b-5e45026a862d")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ID3D11Resource { }

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

        [ComImport]
        [Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDXGIDevice { }

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

        [StructLayout(LayoutKind.Sequential)]
        internal struct DXGI_SAMPLE_DESC
        {
            public int Count;
            public int Quality;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct D3D11_MAPPED_SUBRESOURCE
        {
            public IntPtr pData;
            public int RowPitch;
            public int DepthPitch;
        }

        internal enum D3D11_USAGE
        {
            D3D11_USAGE_DEFAULT = 0,
            D3D11_USAGE_IMMUTABLE = 1,
            D3D11_USAGE_DYNAMIC = 2,
            D3D11_USAGE_STAGING = 3
        }

        internal enum D3D11_CPU_ACCESS_FLAG
        {
            D3D11_CPU_ACCESS_WRITE = 0x10000,
            D3D11_CPU_ACCESS_READ = 0x20000
        }

        internal enum D3D11_MAP
        {
            D3D11_MAP_READ = 1,
            D3D11_MAP_WRITE = 2,
            D3D11_MAP_READ_WRITE = 3,
            D3D11_MAP_WRITE_DISCARD = 4,
            D3D11_MAP_WRITE_NO_OVERWRITE = 5
        }
    }
}