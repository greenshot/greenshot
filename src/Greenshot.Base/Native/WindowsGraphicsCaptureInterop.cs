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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.DesktopWindowsManager.Enums;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Native.DirectX;
using log4net;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Greenshot.Base.Threading;

namespace Greenshot.Base.Native
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
        private const int D3D_DRIVER_TYPE_WARP = 5;
        private const int D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;

        /// <summary>
        /// Creates a Direct3D 11 device and its associated device context with BGRA support.
        /// Uses the hardware (GPU) driver, and falls back to WARP (the software rasterizer) when there is no usable GPU,
        /// e.g. in a virtual machine without GPU or some RDP sessions.
        /// </summary>
        /// <param name="device">When this method returns, contains the created ID3D11Device instance representing the Direct3D device.</param>
        /// <param name="context">When this method returns, contains the created ID3D11DeviceContext instance used to issue rendering commands.</param>
        internal static void CreateD3D11Device(out ID3D11Device device, out ID3D11DeviceContext context)
        {
            int hr = CreateD3D11Device(D3D_DRIVER_TYPE_HARDWARE, out device, out context);
            if (hr == 0)
            {
                return;
            }

            Log.Info($"Creating a hardware Direct3D 11 device failed (0x{hr:X8}), using the WARP software device.");
            int warpHr = CreateD3D11Device(D3D_DRIVER_TYPE_WARP, out device, out context);
            if (warpHr != 0)
            {
                Log.Warn($"Creating the WARP Direct3D 11 device failed too (0x{warpHr:X8}).");
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Create a Direct3D 11 device for the driver type, returns the HRESULT
        /// </summary>
        internal static int CreateD3D11Device(int driverType, out ID3D11Device device, out ID3D11DeviceContext context)
        {
            return D3D11CreateDevice(
                IntPtr.Zero,
                driverType,
                IntPtr.Zero,
                D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                null,
                0,
                D3D11_SDK_VERSION,
                out device,
                out _,
                out context);
        }

        /// <summary>
        /// The WARP (software) driver type, for tests which force the fallback
        /// </summary>
        internal static int WarpDriverType => D3D_DRIVER_TYPE_WARP;

        private static readonly Lazy<bool> IsSupportedLazy = new(CheckIsSupported, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// True when Windows Graphics Capture can be used: Windows 10 1809 (build 17763) or newer and GraphicsCaptureSession.IsSupported().
        /// Checked once, used for the still captures and the video recording.
        /// </summary>
        public static bool IsSupported => IsSupportedLazy.Value;

        private static bool CheckIsSupported()
        {
            try
            {
                // Windows 10 Version 1809 (Build 17763) or higher for WGC, 19041+ recommended for cursor capture and Direct3D interop.
                var version = Environment.OSVersion.Version;
                if (version.Major < 10 || (version.Major == 10 && version.Build < 17763))
                {
                    return false;
                }

                bool isSupported = GraphicsCaptureSession.IsSupported();
                if (!isSupported)
                {
                    Log.Info("Windows Graphics Capture is not supported on this system, the legacy capture is used.");
                }

                return isSupported;
            }
            catch (Exception ex)
            {
                Log.Debug("GraphicsCaptureSession.IsSupported check failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Create the Direct3D device in the background, so the first capture doesn't pay for it (~200 ms).
        /// </summary>
        public static async Task PrewarmAsync(CancellationToken cancellationToken = default)
        {
            // The device is bound to the MTA (see GetOrCreateDevice)
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSupported)
            {
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            lock (DeviceLock)
            {
                if (GetOrCreateDevice(out _, out _, out _))
                {
                    Log.Debug($"Direct3D device for Windows Graphics Capture is ready after {stopwatch.ElapsedMilliseconds} ms.");
                }
            }
        }

        internal static readonly object DeviceLock = new object();
        private static ID3D11Device _cachedD3D11Device;
        private static ID3D11DeviceContext _cachedContext;
        private static IDirect3DDevice _cachedDirect3DDevice;
        private static HdrToneMapper _cachedToneMapper;

        /// <summary>
        /// Gets or creates the cached Direct3D 11 device, context, and WinRT Direct3D device.
        /// Must be called while holding DeviceLock, from an MTA thread (the capture methods switch to the thread pool for this).
        /// </summary>
        /// <remarks>
        /// .NET binds a COM object to the apartment it was created in, and Direct3D objects cannot be marshaled to another
        /// apartment (E_NOINTERFACE). All MTA threads share one apartment, so keeping every use of the cached device on the
        /// MTA makes it usable from any of those threads. An STA thread (like the UI thread) is refused here, instead of
        /// creating a device that the next capture on the thread pool could not use.
        /// </remarks>
        internal static bool GetOrCreateDevice(out ID3D11Device d3d11Device, out ID3D11DeviceContext context, out IDirect3DDevice winrtDevice)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                Log.Error("The Direct3D 11 device must be used from an MTA thread, not from an STA thread.");
                d3d11Device = null;
                context = null;
                winrtDevice = null;
                return false;
            }

            if (_cachedD3D11Device == null)
            {
                try
                {
                    CreateD3D11Device(out _cachedD3D11Device, out _cachedContext);
                    _cachedDirect3DDevice = CreateID3DDeviceFromD3D11Device(_cachedD3D11Device);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to create Direct3D11 device: {ex.Message}", ex);
                    InvalidateCachedDevice();
                    d3d11Device = null;
                    context = null;
                    winrtDevice = null;
                    return false;
                }
            }

            d3d11Device = _cachedD3D11Device;
            context = _cachedContext;
            winrtDevice = _cachedDirect3DDevice;
            return true;
        }

        /// <summary>
        /// Invalidates and releases all cached Direct3D and Direct2D resources.
        /// </summary>
        public static void InvalidateCachedDevice()
        {
            lock (DeviceLock)
            {
                if (_cachedToneMapper != null)
                {
                    try { _cachedToneMapper.Dispose(); } catch { }
                    _cachedToneMapper = null;
                }
                if (_cachedDirect3DDevice != null)
                {
                    try { (_cachedDirect3DDevice as IDisposable)?.Dispose(); } catch { }
                    _cachedDirect3DDevice = null;
                }
                if (_cachedContext != null)
                {
                    try { Marshal.ReleaseComObject(_cachedContext); } catch { }
                    _cachedContext = null;
                }
                if (_cachedD3D11Device != null)
                {
                    try { Marshal.ReleaseComObject(_cachedD3D11Device); } catch { }
                    _cachedD3D11Device = null;
                }
            }
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

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11SurfaceFromDXGISurface", PreserveSig = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr graphicsSurface);

        /// <summary>
        /// Creates a new IDirect3DSurface WinRT instance from the specified ID3D11Texture2D.
        /// </summary>
        internal static IDirect3DSurface CreateDirect3D11SurfaceFromTexture2D(ID3D11Texture2D texture)
        {
            var dxgiSurface = (IDXGISurface)texture;
            IntPtr pDxgiSurface = Marshal.GetComInterfaceForObject(dxgiSurface, typeof(IDXGISurface));

            try
            {
                int hr = CreateDirect3D11SurfaceFromDXGISurface(pDxgiSurface, out IntPtr pUnknown);
                if (hr == 0 && pUnknown != IntPtr.Zero)
                {
                    var surface = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DSurface;
                    Marshal.Release(pUnknown);
                    return surface;
                }
                Marshal.ThrowExceptionForHR(hr);
                return null;
            }
            finally
            {
                Marshal.Release(pDxgiSurface);
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
        internal static ID3D11Texture2D CreateTexture2DFromID3DSurface(IDirect3DSurface surface)
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
        /// <remarks>
        /// Windows Graphics Capture (and Direct2D) deliver premultiplied BGRA, Format32bppArgb is straight alpha:
        /// the colors of partly transparent pixels are un-premultiplied while copying. Otherwise those pixels are too dark,
        /// e.g. a dark fringe at the rounded corners of Windows 11 windows.
        /// </remarks>
        /// <param name="texture">The Direct3D 11 texture to be converted. Must be a valid ID3D11Texture2D instance.</param>
        /// <param name="device">The Direct3D 11 device used to create a staging texture for data transfer.</param>
        /// <param name="context">The Direct3D 11 device context used to copy and map the texture data.</param>
        /// <returns>A Bitmap containing the pixel data from the specified texture. The Bitmap is formatted as 32bpp ARGB.</returns>
        internal static unsafe Bitmap TransformTextureToBitmap(ID3D11Texture2D texture, ID3D11Device device, ID3D11DeviceContext context)
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
                        UnpremultiplyRow(destPtr, width);

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
        /// Convert a row of premultiplied BGRA pixels to straight alpha, in place.
        /// Opaque (the vast majority) and fully transparent pixels are left as they are.
        /// </summary>
        /// <param name="row">Pointer to the first pixel (B, G, R, A byte order)</param>
        /// <param name="width">Number of pixels in the row</param>
        internal static unsafe void UnpremultiplyRow(byte* row, int width)
        {
            byte* pixel = row;
            for (int x = 0; x < width; x++, pixel += 4)
            {
                int alpha = pixel[3];
                if (alpha == 255 || alpha == 0)
                {
                    continue;
                }

                int half = alpha / 2;
                pixel[0] = (byte)Math.Min(255, (pixel[0] * 255 + half) / alpha);
                pixel[1] = (byte)Math.Min(255, (pixel[1] * 255 + half) / alpha);
                pixel[2] = (byte)Math.Min(255, (pixel[2] * 255 + half) / alpha);
            }
        }

        /// <summary>
        /// Tone-maps an FP16 HDR texture to a standard 8-bit SDR bitmap.
        /// Tries GPU-accelerated D2D tone mapping first, falling back to CPU if that fails.
        /// </summary>
        /// <param name="hdrTexture">The captured FP16 (R16G16B16A16_FLOAT) texture.</param>
        /// <param name="device">The D3D11 device used for creating resources.</param>
        /// <param name="context">The D3D11 device context for copy/map operations.</param>
        /// <param name="sdrWhiteLevelInNits">The SDR white level in nits for the display.</param>
        /// <returns>A Bitmap with tone-mapped SDR content, or null if both paths fail.</returns>
        private static Bitmap ToneMapHdrTextureToBitmap(
            ID3D11Texture2D hdrTexture, ID3D11Device device,
            ID3D11DeviceContext context, float sdrWhiteLevelInNits)
        {
            D3D11_TEXTURE2D_DESC desc;
            hdrTexture.GetDesc(out desc);
            int width = desc.Width;
            int height = desc.Height;

            // Try GPU path first (Direct2D WhiteLevelAdjustment effect)
            try
            {
                lock (DeviceLock)
                {
                    _cachedToneMapper ??= new HdrToneMapper(device);
                }

                var sdrTexture = _cachedToneMapper.ToneMapToSdr(hdrTexture, device, sdrWhiteLevelInNits, width, height);
                try
                {
                    return TransformTextureToBitmap(sdrTexture, device, context);
                }
                finally
                {
                    if (sdrTexture != null) Marshal.ReleaseComObject(sdrTexture);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"GPU HDR tone mapping failed, falling back to CPU: {ex.Message}", ex);
                lock (DeviceLock)
                {
                    if (_cachedToneMapper != null)
                    {
                        try { _cachedToneMapper.Dispose(); } catch { }
                        _cachedToneMapper = null;
                    }
                }
            }

            // CPU fallback
            try
            {
                return CpuToneMapFp16(hdrTexture, device, context, sdrWhiteLevelInNits, width, height);
            }
            catch (Exception ex)
            {
                Log.Warn($"CPU HDR tone mapping also failed: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// CPU-based tone mapping: stages the FP16 texture to CPU-readable memory,
        /// then applies Reinhard tone mapping + gamma 2.2 per pixel.
        /// </summary>
        internal static Bitmap CpuToneMapFp16(
            ID3D11Texture2D hdrTexture, ID3D11Device device,
            ID3D11DeviceContext context, float sdrWhiteLevelInNits,
            int width, int height)
        {
            D3D11_TEXTURE2D_DESC desc;
            hdrTexture.GetDesc(out desc);

            // Create a staging texture to copy FP16 data to CPU
            var stagingDesc = new D3D11_TEXTURE2D_DESC
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = desc.Format, // R16G16B16A16_FLOAT
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
                BindFlags = 0,
                CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                MiscFlags = 0
            };

            device.CreateTexture2D(ref stagingDesc, IntPtr.Zero, out var stagingTexture);

            try
            {
                context.CopyResource(stagingTexture, hdrTexture);

                int hr = context.Map(stagingTexture, 0, D3D11_MAP.D3D11_MAP_READ, 0, out var mappedResource);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);

                try
                {
                    return HdrCpuToneMapper.ToneMapFp16ToBitmap(
                        mappedResource.pData, mappedResource.RowPitch,
                        width, height, sdrWhiteLevelInNits);
                }
                finally
                {
                    context.Unmap(stagingTexture, 0);
                }
            }
            finally
            {
                if (stagingTexture != null) Marshal.ReleaseComObject(stagingTexture);
            }
        }

        private static void ConfigureCaptureSession(GraphicsCaptureSession session)
        {
            // We do not want to have the cursor in the capture, as we do this separately.
            session.IsCursorCaptureEnabled = false;

            // We do not want the yellow border around the capture area if the OS supports disabling it (Windows 11+).
            if ((object)session is IGraphicsCaptureSession3 session3)
            {
                try
                {
                    session3.IsBorderRequired = false;
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to disable capture border: " + ex.Message);
                }
            }
        }

        private static readonly SemaphoreSlim CaptureSemaphore = new SemaphoreSlim(1, 1);
        private static readonly TimeSpan FrameArrivedTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Captures the visual content of the specified window and returns it as a Bitmap image.
        /// </summary>
        /// <remarks>This method uses Direct3D 11 to capture the window's content.
        /// A minimized window is restored first (a minimized window delivers no frames).
        /// Windows Graphics Capture only captures top-level windows: for a child window its top-level window is captured and cropped
        /// to the child window.
        /// The caller is responsible for disposing the returned Bitmap when it is no longer needed.
        /// </remarks>
        /// <param name="window">The handle to the window whose content is to be captured. Must be a valid window handle; otherwise, the capture may fail.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>A Bitmap object containing the captured image of the specified window. Returns null if the capture operation fails.</returns>
        public static async Task<Bitmap> CaptureWindowToBitmapAsync(IntPtr window, CancellationToken cancellationToken = default)
        {
            if (window == IntPtr.Zero || !IsSupported)
            {
                return null;
            }

            var topLevelWindow = GetAncestor(window, GetAncestorRoot);
            if (topLevelWindow == IntPtr.Zero)
            {
                topLevelWindow = window;
            }

            // A minimized window doesn't deliver frames, the capture would run into the timeout: restore it first (as the legacy capture does)
            var topLevelDetails = new WindowDetails(topLevelWindow);
            if (topLevelDetails.Iconic)
            {
                Log.Debug($"Restoring the minimized window {topLevelWindow} for the capture.");
                await topLevelDetails.RestoreAsync(cancellationToken).ConfigureAwait(false);
            }

            var bitmap = await CaptureItemToBitmapAsync(() => CreateCaptureItemForWindow(topLevelWindow), () => HdrDisplayInfo.GetMonitorForWindow(topLevelWindow), $"window {topLevelWindow}", cancellationToken).ConfigureAwait(false);
            if (bitmap == null || topLevelWindow == window)
            {
                return bitmap;
            }

            return CropToChildWindow(bitmap, topLevelWindow, window);
        }

        /// <summary>
        /// Crop the capture of the top-level window to the bounds of the child window, the capture is disposed.
        /// </summary>
        private static Bitmap CropToChildWindow(Bitmap topLevelCapture, IntPtr topLevelWindow, IntPtr childWindow)
        {
            using (topLevelCapture)
            {
                // The capture of a top-level window covers its visible frame (the extended frame bounds, without the invisible resize borders)
                if (!TryGetExtendedFrameBounds(topLevelWindow, out var captureBounds) && !GetWindowRect(topLevelWindow, out captureBounds))
                {
                    Log.Debug($"Can't determine the bounds of window {topLevelWindow}, the child window {childWindow} isn't captured.");
                    return null;
                }

                if (!GetWindowRect(childWindow, out var childBounds))
                {
                    Log.Debug($"Can't determine the bounds of the child window {childWindow}.");
                    return null;
                }

                var cropRectangle = new Rectangle(childBounds.X - captureBounds.X, childBounds.Y - captureBounds.Y, childBounds.Width, childBounds.Height);
                cropRectangle.Intersect(new Rectangle(0, 0, topLevelCapture.Width, topLevelCapture.Height));
                if (cropRectangle.Width <= 0 || cropRectangle.Height <= 0)
                {
                    Log.Debug($"The child window {childWindow} is outside of the capture of its top-level window {topLevelWindow}.");
                    return null;
                }

                Log.Debug($"Captured the top-level window {topLevelWindow} for the child window {childWindow}, cropped to {cropRectangle}.");
                return topLevelCapture.Clone(cropRectangle, topLevelCapture.PixelFormat);
            }
        }

        private static bool TryGetExtendedFrameBounds(IntPtr window, out NativeRect bounds)
        {
            var result = DwmApi.DwmGetWindowAttribute(window, DwmWindowAttributes.ExtendedFrameBounds, out bounds, Marshal.SizeOf(typeof(NativeRect)));
            return result.Succeeded() && bounds.Width > 0 && bounds.Height > 0;
        }

        private const uint GetAncestorRoot = 2; // GA_ROOT

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rectangle);

        /// <summary>
        /// Captures the image of the specified monitor and returns it as a Bitmap object.
        /// </summary>
        /// <remarks>This method uses Direct3D 11 to perform the capture. All COM resources are released
        /// after the operation to prevent memory leaks. Ensure that the monitor handle is valid and accessible before
        /// calling this method.</remarks>
        /// <param name="hMonitor">A handle to the monitor to capture. Must be a valid monitor handle obtained from the system.</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>A Bitmap containing the captured image of the monitor. Returns null if the capture operation fails.</returns>
        public static Task<Bitmap> CaptureMonitorToBitmapAsync(IntPtr hMonitor, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                return Task.FromResult<Bitmap>(null);
            }

            return CaptureItemToBitmapAsync(() => CreateCaptureItemForMonitor(hMonitor), () => hMonitor, $"monitor {hMonitor}", cancellationToken);
        }

        /// <summary>
        /// Capture one frame of the capture item: the frame arrival is awaited (TaskCompletionSource with a timeout), not blocked on.
        /// </summary>
        private static async Task<Bitmap> CaptureItemToBitmapAsync(Func<GraphicsCaptureItem> createItem, Func<IntPtr> getMonitor, string description, CancellationToken cancellationToken)
        {
            // The Direct3D objects are bound to the MTA (see GetOrCreateDevice): never run this on the UI (STA) thread
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            await CaptureSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            Direct3D11CaptureFramePool framePool = null;
            GraphicsCaptureSession session = null;
            try
            {
                var frameArrived = Tcs.Create<bool>();
                ID3D11Device d3d11Device;
                ID3D11DeviceContext context;
                bool isHdr;
                float sdrWhiteLevelInNits;
                lock (DeviceLock)
                {
                    if (!GetOrCreateDevice(out d3d11Device, out context, out var device))
                    {
                        return null;
                    }

                    GraphicsCaptureItem captureItem;
                    try
                    {
                        captureItem = createItem();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Creating the capture item failed for {description}: {ex.Message}");
                        return null;
                    }

                    if (captureItem == null)
                    {
                        Log.Debug($"No capture item for {description}.");
                        return null;
                    }

                    // Detect HDR on the monitor of the capture item
                    IntPtr hMonitor = getMonitor();
                    isHdr = HdrDisplayInfo.IsHdrActiveForMonitor(hMonitor);
                    sdrWhiteLevelInNits = isHdr ? HdrDisplayInfo.GetSdrWhiteLevelInNits(hMonitor) : 80.0f;
                    var pixelFormat = isHdr
                        ? DirectXPixelFormat.R16G16B16A16Float
                        : DirectXPixelFormat.B8G8R8A8UIntNormalized;

                    Log.Debug($"{description}: HDR={isHdr}, SDR white level={sdrWhiteLevelInNits} nits, format={pixelFormat}.");

                    framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, pixelFormat, 1, captureItem.Size);
                    session = framePool.CreateCaptureSession(captureItem);
                    ConfigureCaptureSession(session);
                    framePool.FrameArrived += (s, e) => frameArrived.TrySetResult(true);
                    session.StartCapture();
                }

                try
                {
                    await frameArrived.Task.WaitAsync(FrameArrivedTimeout, cancellationToken).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    Log.Debug($"Timeout waiting for FrameArrived on {description}.");
                    return null;
                }

                // Continues on a thread pool (MTA) thread
                lock (DeviceLock)
                {
                    using var frame = framePool.TryGetNextFrame();
                    if (frame == null)
                    {
                        Log.Debug($"TryGetNextFrame returned null after FrameArrived on {description}.");
                        return null;
                    }

                    var texture = CreateTexture2DFromID3DSurface(frame.Surface);
                    try
                    {
                        if (texture == null)
                        {
                            return null;
                        }

                        if (isHdr)
                        {
                            return ToneMapHdrTextureToBitmap(texture, d3d11Device, context, sdrWhiteLevelInNits);
                        }

                        return TransformTextureToBitmap(texture, d3d11Device, context);
                    }
                    finally
                    {
                        if (texture != null) Marshal.ReleaseComObject(texture);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Warn($"WindowsGraphicsCapture failed for {description}: {ex.Message}", ex);
                InvalidateCachedDevice();
                return null;
            }
            finally
            {
                session?.Dispose();
                framePool?.Dispose();
                CaptureSemaphore.Release();
            }
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
        public static async Task<Bitmap> CaptureRectangleAsync(NativeRect captureBounds, CancellationToken cancellationToken = default)
        {
            if (captureBounds.Height <= 0 || captureBounds.Width <= 0 || !IsSupported)
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

            // Fast-path: single monitor captured in its entirety avoids duplicate bitmap allocation and GDI+ blit
            if (displaysInCapture.Length == 1)
            {
                var single = displaysInCapture[0];
                if (single.Intersection.Equals(single.Display.Bounds) && single.Intersection.Equals(captureBounds))
                {
                    return await CaptureMonitorToBitmapAsync(single.Display.MonitorHandle, cancellationToken).ConfigureAwait(false);
                }
            }

            Bitmap resultBitmap = null;
            Graphics graphics = null;
            try
            {

                foreach (var item in displaysInCapture)
                {
                    using var monitorBitmap = await CaptureMonitorToBitmapAsync(item.Display.MonitorHandle, cancellationToken).ConfigureAwait(false);
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