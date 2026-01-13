using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using log4net;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using D3D11Device = SharpDX.Direct3D11.Device;
using D3D11MapFlags = SharpDX.Direct3D11.MapFlags;
using DXGIDevice = SharpDX.DXGI.Device3;

namespace Greenshot.Helpers.WindowsRT
{
    internal class WindowsGraphicsCaptureInterop
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsGraphicsCaptureInterop));

        public static Bitmap CaptureWindowToBitmap(IntPtr window) {
            using var device = new D3D11Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            using var texture = CaptureWindowToTexture2D(window, device);
            return TransformTextureToBitmap(texture, device.ImmediateContext);
        }

        public static Bitmap CaptureMonitorToBitmap(IntPtr hMonitor)
        {
            using var device = new D3D11Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            using var texture = CaptureMonitorToTexture2D(hMonitor, device);
            return TransformTextureToBitmap(texture, device.ImmediateContext);
        }

        private static Texture2D CaptureWindowToTexture2D(IntPtr window, D3D11Device d3D11Device) {
            var captureItem = CreateCaptureItemForWindow(window);
            using var device = CreateID3DDeviceFromD3D11Device(d3D11Device);
            using var framePool = Direct3D11CaptureFramePool.Create(device,DirectXPixelFormat.B8G8R8A8UIntNormalized,1,captureItem.Size);
            var session = framePool.CreateCaptureSession(captureItem);
            //session.IsBorderRequired = false;
            session.IsCursorCaptureEnabled = false;
            //session.IncludeSecondaryWindows = false;
            session.StartCapture();
            Direct3D11CaptureFrame frameTemp = null;
            while (frameTemp == null)
            {
                Log.Debug("Waiting for next frame...");
                frameTemp = framePool.TryGetNextFrame();
            }
            using var frame = frameTemp;
            return CreateTexture2DFromID3DSurface(frame.Surface);
        }

        private static Texture2D CaptureMonitorToTexture2D(IntPtr hMonitor, D3D11Device d3D11Device)
        {
            var captureItem = CreateCaptureItemForMonitor(hMonitor);
            using var device = CreateID3DDeviceFromD3D11Device(d3D11Device);
            using var framePool = Direct3D11CaptureFramePool.Create(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, captureItem.Size);
            var session = framePool.CreateCaptureSession(captureItem);
            //session.IsBorderRequired = false;
            session.IsCursorCaptureEnabled = false;
            //session.IncludeSecondaryWindows = false;
            session.StartCapture();
            Direct3D11CaptureFrame frameTemp = null;
            while (frameTemp == null)
            {
                frameTemp = framePool.TryGetNextFrame();
            }
            using var frame = frameTemp;
            return CreateTexture2DFromID3DSurface(frame.Surface);
        }

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

        private static IDirect3DDevice CreateID3DDeviceFromD3D11Device(D3D11Device d3dDevice)
        {
            IDirect3DDevice device = null;
            using var dxgiDevice = d3dDevice.QueryInterface<DXGIDevice>();
            var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr pUnknown);
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

        [ComImport]
        [ComVisible(true)]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        };

        private static readonly Guid ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        private static Texture2D CreateTexture2DFromID3DSurface(IDirect3DSurface surface)
        {
            var access = (IDirect3DDxgiInterfaceAccess)surface;
            var d3dPointer = access.GetInterface(ID3D11Texture2D);
            var d3dSurface = new Texture2D(d3dPointer);
            return d3dSurface;
        }

        private static Bitmap TransformTextureToBitmap(Texture2D texture, DeviceContext d3dContext)
        {
            var width = texture.Description.Width;
            var height = texture.Description.Height;

            using var textureCopy = new Texture2D( d3dContext.Device, new Texture2DDescription {
                    Width = width,
                    Height = height,
                    Format = texture.Description.Format,
                    Usage = ResourceUsage.Staging,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = new SampleDescription(1, 0),
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None,
                    BindFlags = BindFlags.None,
                });
            d3dContext.CopyResource(texture, textureCopy);

            var dataBox = d3dContext.MapSubresource(
                textureCopy,
                0,
                0,
                MapMode.Read,
                D3D11MapFlags.None,
                out var dataStream);

            try
            {
                // 3. Create the destination Bitmap
                // Note: This assumes the Texture2D is in B8G8R8A8_UNorm format (standard for GDI+)
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                // Lock the bitmap bits so we can write to them directly
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),ImageLockMode.WriteOnly, bitmap.PixelFormat);

                // 4. Copy line by line
                // We cannot do a single block copy because the GPU 'RowPitch' 
                // is often wider than the Bitmap 'Stride' due to memory alignment.
                IntPtr sourcePtr = dataStream.DataPointer;
                IntPtr destPtr = bmpData.Scan0;

                for (int y = 0; y < height; y++)
                {
                    // Copy one row of pixels
                    Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                    // Advance pointers based on their respective strides/pitches
                    sourcePtr = IntPtr.Add(sourcePtr, dataBox.RowPitch);
                    destPtr = IntPtr.Add(destPtr, bmpData.Stride);
                }

                bitmap.UnlockBits(bmpData);
                return bitmap;
            }
            finally
            {
                // 5. Always unmap the resource to release the CPU lock
                d3dContext.UnmapSubresource(texture, 0);
            }
        }
    }
}
