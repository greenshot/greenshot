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
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Native.DirectX;
using log4net;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace Greenshot.Native
{
    /// <summary>
    /// Provides static methods for capturing the visual content of windows and monitors via Windows Graphics Capture API
    /// Utilizing Direct3D 11 for high-performance access to the captured frames.
    /// </summary>
    internal partial class WindowsGraphicsCaptureInterop
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsGraphicsCaptureInterop));
        // Constants
        private const int D3D11_SDK_VERSION = 7;
        private const int D3D_DRIVER_TYPE_HARDWARE = 1;
        private const int D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;

        /// <summary>
        /// Creates a Direct3D 11 device and its associated device context for hardware rendering with BGRA support.
        /// </summary>
        /// <param name="device">When this method returns, contains the created ID3D11Device instance representing the Direct3D device.</param>
        /// <param name="context">When this method returns, contains the created ID3D11DeviceContext instance used to issue rendering commands.</param>
        public static void CreateD3D11Device(out ID3D11Device device, out ID3D11DeviceContext context)
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

        /// <summary>
        /// The GUID representing the GraphicsCaptureItem interface for interop operations.
        /// </summary>
        private static readonly Guid GraphicsCaptureItemGuid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");

        /// <summary>
        /// Creates a new instance of a GraphicsCaptureItem that represents the specified window, enabling capture of
        /// its visual content.
        /// </summary>
        /// <remarks>The caller must ensure that the window handle is valid and that the application has
        /// the necessary permissions to capture the window's content. This method is typically used for screen capture
        /// scenarios involving specific windows.</remarks>
        /// <param name="window">The handle to the window for which the capture item is created. Must be a valid window handle.</param>
        /// <returns>A GraphicsCaptureItem that corresponds to the specified window, allowing its content to be captured.</returns>
        public static GraphicsCaptureItem CreateCaptureItemForWindow(IntPtr window)
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var itemPointer = interop.CreateForWindow(window, GraphicsCaptureItemGuid);
            var item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
            Marshal.Release(itemPointer);
            return item;
        }

        /// <summary>
        /// Creates a new instance of a GraphicsCaptureItem that represents the specified monitor.
        /// </summary>
        /// <remarks>This method requires that the monitor handle is valid and accessible. Ensure that the
        /// application has the necessary permissions to capture the monitor's content.</remarks>
        /// <param name="hMonitor">The handle to the monitor for which the capture item is created. Must be a valid monitor handle obtained
        /// from the system.</param>
        /// <returns>A GraphicsCaptureItem that represents the specified monitor. Returns null if the creation fails.</returns>
        public static GraphicsCaptureItem CreateCaptureItemForMonitor(IntPtr hMonitor)
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var itemPointer = interop.CreateForMonitor(hMonitor, GraphicsCaptureItemGuid);
            var item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
            Marshal.Release(itemPointer);
            return item;
        }

        /// <summary>
        /// Creates a Direct3D 11 device from an existing DXGI device handle.
        /// </summary>
        /// <remarks>This method enables integration between Direct3D 11 and DXGI by allowing the creation
        /// of a Direct3D 11 device from an existing DXGI device. Ensure that the DXGI device is properly initialized
        /// before calling this method. The created device can be used for resource sharing and interoperability
        /// scenarios.</remarks>
        /// <param name="dxgiDevice">A handle to the DXGI device from which the Direct3D 11 device will be created. Must be a valid and
        /// initialized DXGI device.</param>
        /// <param name="graphicsDevice">When the method returns, contains the handle to the newly created Direct3D 11 device.</param>
        /// <returns>A value of zero if the operation succeeds; otherwise, an error code indicating the reason for failure.</returns>
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

        /// <summary>
        /// Creates a new IDirect3DDevice instance from the specified ID3D11Device.
        /// </summary>
        /// <remarks>This method internally marshals the ID3D11Device to a DXGI device and uses it to
        /// create a Direct3D device. If the operation fails, an exception is thrown. Ensure that the provided device is
        /// valid before calling this method.</remarks>
        /// <param name="d3dDevice">The ID3D11Device to convert. This parameter cannot be null and must be properly initialized.</param>
        /// <returns>An IDirect3DDevice representing the Direct3D device created from the provided ID3D11Device. Returns null if
        /// the creation fails.</returns>
        internal static IDirect3DDevice CreateID3DDeviceFromD3D11Device(ID3D11Device d3dDevice)
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

        private static readonly Guid IID_ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        /// <summary>
        /// Creates a new ID3D11Texture2D instance from the specified Direct3D surface.
        /// </summary>
        /// <remarks>This method retrieves the underlying ID3D11Texture2D interface from the given surface.
        /// Ensure that the surface supports ID3D11Texture2D to avoid runtime errors.</remarks>
        /// <param name="surface">The IDirect3DSurface from which to create the texture. Must be a valid Direct3D surface compatible with ID3D11Texture2D.</param>
        /// <returns>An ID3D11Texture2D object representing the texture created from the provided surface.</returns>
        private static ID3D11Texture2D CreateTexture2DFromID3DSurface(IDirect3DSurface surface)
        {
            var access = (IDirect3DDxgiInterfaceAccess)surface;
            var d3dPointer = access.GetInterface(IID_ID3D11Texture2D);
            var d3dSurface = (ID3D11Texture2D)Marshal.GetObjectForIUnknown(d3dPointer);
            Marshal.Release(d3dPointer);
            return d3dSurface;
        }

        /// <summary>
        /// Transforms a Direct3D 11 texture into a .NET Bitmap object in 32bpp ARGB format.
        /// </summary>
        /// <param name="texture">The Direct3D 11 texture to be converted. Must be a valid ID3D11Texture2D instance.</param>
        /// <param name="device">The Direct3D 11 device used to create a staging texture for data transfer.</param>
        /// <param name="context">The Direct3D 11 device context used to copy and map the texture data.</param>
        /// <returns>A Bitmap containing the pixel data from the specified texture. The Bitmap is formatted as 32bpp ARGB.</returns>
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

                // Test HRESULT of Map
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

        /// <summary>
        /// Captures the visual content of the specified window and returns it as a Bitmap image.
        /// </summary>
        /// <remarks>This method uses Direct3D 11 to capture the window's content.
        /// The window must be visible and not minimized for accurate results.
        /// The caller is responsible for disposing the returned Bitmap when it is no longer needed.
        /// </remarks>
        /// <param name="window">The handle to the window whose content is to be captured. Must be a valid window handle; otherwise, the capture may fail.</param>
        /// <returns>A Bitmap object containing the captured image of the specified window. Returns null if the capture operation fails.</returns>
        public static Bitmap CaptureWindowToBitmap(IntPtr window)
        {
            return Task.Run(() =>
            {
                CreateD3D11Device(out var d3d11Device, out var context);

                try
                {
                    var captureItem = CreateCaptureItemForWindow(window);
                    var device = CreateID3DDeviceFromD3D11Device(d3d11Device);

                    using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, captureItem.Size);
                    using var session = framePool.CreateCaptureSession(captureItem);

                    // We do not want to have the cursor in the capture, as we do this separately.
                    session.IsCursorCaptureEnabled = false;

                    using var frameArrivedEvent = new ManualResetEvent(false);
                    framePool.FrameArrived += (s, e) => frameArrivedEvent.Set();

                    session.StartCapture();

                    if (!frameArrivedEvent.WaitOne(1000))
                    {
                        Log.Debug($"Timeout waiting for FrameArrived on window {window}.");
                        return null;
                    }
                    using var frame = framePool.TryGetNextFrame();
                    if (frame == null)
                    {
                        Log.Debug($"TryGetNextFrame returned null after FrameArrived on window {window}.");
                        return null;
                    }
                    var texture = CreateTexture2DFromID3DSurface(frame.Surface);
                    try
                    {
                        if (texture == null)
                        {
                            return null;
                        }
                        return TransformTextureToBitmap(texture, d3d11Device, context);
                    }
                    finally
                    {
                        if (texture != null) Marshal.ReleaseComObject(texture);
                    }
                }
                finally
                {
                    if (context != null) Marshal.ReleaseComObject(context);
                    if (d3d11Device != null) Marshal.ReleaseComObject(d3d11Device);
                }
            }).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Captures the image of the specified monitor and returns it as a Bitmap object.
        /// </summary>
        /// <remarks>This method uses Direct3D 11 to perform the capture. All COM resources are released
        /// after the operation to prevent memory leaks. Ensure that the monitor handle is valid and accessible before
        /// calling this method.</remarks>
        /// <param name="hMonitor">A handle to the monitor to capture. Must be a valid monitor handle obtained from the system.</param>
        /// <returns>A Bitmap containing the captured image of the monitor. Returns null if the capture operation fails.</returns>
        public static Bitmap CaptureMonitorToBitmap(IntPtr hMonitor)
        {
            return Task.Run(() =>
            {
                CreateD3D11Device(out var d3d11Device, out var context);
                try
                {
                    var captureItem = CreateCaptureItemForMonitor(hMonitor);
                    var device = CreateID3DDeviceFromD3D11Device(d3d11Device);

                    using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, captureItem.Size);
                    using var session = framePool.CreateCaptureSession(captureItem);

                    // We do not want to have the cursor in the capture, as we do this separately.
                    session.IsCursorCaptureEnabled = false;

                    using var frameArrivedEvent = new ManualResetEvent(false);
                    framePool.FrameArrived += (s, e) => frameArrivedEvent.Set();

                    session.StartCapture();

                    if (!frameArrivedEvent.WaitOne(1000))
                    {
                        Log.Debug($"Timeout waiting for FrameArrived on monitor {hMonitor}.");
                        return null;
                    }

                    using var frame = framePool.TryGetNextFrame();
                    if (frame == null)
                    {
                        Log.Debug($"TryGetNextFrame returned null after FrameArrived on monitor {hMonitor}.");
                        return null;
                    }

                    var texture = CreateTexture2DFromID3DSurface(frame.Surface);
                    try
                    {
                        if (texture == null)
                        {
                            return null;
                        }
                        return TransformTextureToBitmap(texture, d3d11Device, context);
                    }
                    finally
                    {
                        if (texture != null) Marshal.ReleaseComObject(texture);
                    }
                }
                finally
                {
                    if (context != null) Marshal.ReleaseComObject(context);
                    if (d3d11Device != null) Marshal.ReleaseComObject(d3d11Device);
                }
            }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Captures all monitors within the specified bounds using Windows Graphics Capture API and stitches them into a single bitmap.
        /// </summary>
        /// <remarks>Each monitor that intersects with <paramref name="captureBounds"/> is individually captured
        /// and composited into a result bitmap at the correct position. Monitors that do not intersect the bounds are
        /// skipped. The result bitmap uses 32bpp ARGB format with a transparent background. If one or more monitors
        /// fail to capture, the corresponding region in the result bitmap will remain transparent.
        /// The caller is responsible for disposing the returned Bitmap when it is no longer needed.</remarks>
        /// <param name="captureBounds">The screen-coordinate rectangle to capture. Only monitors that intersect this rectangle are included.</param>
        /// <returns>A Bitmap containing the stitched capture of all intersecting monitors, or null if no monitors
        /// intersect the specified bounds or the bounds are empty.</returns>
        public static Bitmap CaptureRectangle(NativeRect captureBounds)
        {
            if (captureBounds.Height <= 0 || captureBounds.Width <= 0)
            {
                return null;
            }

            // Compute the intersection for each display once, and keep only the displays that actually overlap
            var displaysInCapture = DisplayInfo.AllDisplayInfos
                .Select(d => new { Display = d, Intersection = d.Bounds.Intersect(captureBounds) })
                .Where(x => !x.Intersection.IsEmpty)
                .ToArray();

            if (displaysInCapture.Length == 0)
            {
                return null;
            }

            Bitmap resultBitmap = null;
            Graphics graphics = null;
            try
            {

                foreach (var item in displaysInCapture)
                {
                    using var monitorBitmap = CaptureMonitorToBitmap(item.Display.MonitorHandle);
                    if (monitorBitmap == null) continue;
                    resultBitmap ??= new Bitmap(captureBounds.Width, captureBounds.Height, PixelFormat.Format32bppArgb);

                    var intersection = item.Intersection;

                    // Source rectangle within the monitor bitmap
                    var srcRect = new Rectangle(
                        intersection.X - item.Display.Bounds.X,
                        intersection.Y - item.Display.Bounds.Y,
                        intersection.Width,
                        intersection.Height);

                    // Destination rectangle within the result bitmap
                    var destRect = new Rectangle(
                        intersection.X - captureBounds.X,
                        intersection.Y - captureBounds.Y,
                        intersection.Width,
                        intersection.Height);
                    graphics ??= Graphics.FromImage(resultBitmap);
                    graphics.DrawImage(monitorBitmap, destRect, srcRect, GraphicsUnit.Pixel);
                }
            }
            catch
            {
                resultBitmap?.Dispose();
                throw;
            }
            finally
            {
                graphics?.Dispose();
            }

            return resultBitmap;
        }
    }
}