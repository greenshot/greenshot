/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Threading;
using Greenshot.Base.Core;
using Greenshot.Base.Native.DirectX;
using log4net;

namespace Greenshot.Base.Native
{
    /// <summary>
    /// The 8-bit image of an HDR image which isn't on the screen (e.g. an HDR file which is opened): tone mapped like a capture of an HDR display
    /// (HdrContent, HdrToneMapper), from a texture made of the pixels. Without a Direct3D device (on an STA thread, no GPU and no WARP, or an image
    /// bigger than a texture can be) it is tone mapped on the CPU: the same white level, highlights clipped.
    /// </summary>
    public static class HdrImageToneMapping
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HdrImageToneMapping));

        // D3D11_REQ_TEXTURE2D_U_OR_V_DIMENSION of feature level 11
        private const int MaxTextureSize = 16384;
        private const int DxgiFormatR16G16B16A16Float = 10;
        private const int D3D11BindShaderResource = 0x8;

        /// <summary>
        /// The 8-bit image of the HDR image, the caller disposes it
        /// </summary>
        /// <param name="hdrImage">A complete HDR image</param>
        public static Bitmap ToBitmap(HdrImage hdrImage)
        {
            if (hdrImage == null) throw new ArgumentNullException(nameof(hdrImage));
            if (hdrImage.Width <= MaxTextureSize && hdrImage.Height <= MaxTextureSize && Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                try
                {
                    var bitmap = ToBitmapOnGpu(hdrImage);
                    if (bitmap != null)
                    {
                        return bitmap;
                    }
                }
                catch (Exception ex) when (ex is COMException || ex is InvalidOperationException)
                {
                    Log.Warn($"Couldn't tone map the HDR image with Direct3D, doing it on the CPU: {ex.Message}");
                }
            }
            return ToBitmapOnCpu(hdrImage);
        }

        /// <summary>
        /// The SDR white level and peak of the first HDR display, for an HDR file which doesn't say which it had: (80, 0) without an HDR display
        /// </summary>
        public static (float SdrWhiteLevelInNits, float DisplayPeakInNits) CurrentHdrDisplayLevels()
        {
            foreach (var display in Dapplo.Windows.User32.DisplayInfo.AllDisplayInfos)
            {
                if (HdrDisplayInfo.IsHdrActiveForMonitor(display.MonitorHandle))
                {
                    return (HdrDisplayInfo.GetSdrWhiteLevelInNits(display.MonitorHandle), HdrDisplayInfo.GetMaxLuminanceInNits(display.MonitorHandle));
                }
            }
            return (80f, 0f);
        }

        private static unsafe Bitmap ToBitmapOnGpu(HdrImage hdrImage)
        {
            // Direct2D takes the FP16 texture with premultiplied alpha, the HdrImage has straight alpha (the same when it is opaque)
            using var premultiplied = hdrImage.ClonePremultiplied();
            void UseTexturePixels(Action<IntPtr> action)
            {
                if (premultiplied == null)
                {
                    hdrImage.UsePixelMemory(action);
                    return;
                }
                if (!premultiplied.DangerousTryGetSinglePixelMemory(out Memory<SixLabors.ImageSharp.PixelFormats.HalfVector4> memory))
                {
                    throw new InvalidOperationException("The premultiplied pixels are not one block of memory.");
                }
                using var handle = memory.Pin();
                action((IntPtr)handle.Pointer);
            }

            return WindowsGraphicsCaptureInterop.UseDevice((device, context) =>
            {
                ID3D11Texture2D texture = null;
                UseTexturePixels(first =>
                {
                    var desc = new D3D11_TEXTURE2D_DESC
                    {
                        Width = hdrImage.Width,
                        Height = hdrImage.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = DxgiFormatR16G16B16A16Float,
                        SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                        Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                        BindFlags = D3D11BindShaderResource,
                        CPUAccessFlags = 0,
                        MiscFlags = 0
                    };
                    var initialData = new D3D11_SUBRESOURCE_DATA { SysMem = first, SysMemPitch = hdrImage.Stride };
                    var initialDataHandle = GCHandle.Alloc(initialData, GCHandleType.Pinned);
                    try
                    {
                        device.CreateTexture2D(ref desc, initialDataHandle.AddrOfPinnedObject(), out texture);
                    }
                    finally
                    {
                        initialDataHandle.Free();
                    }
                });
                try
                {
                    var sdrTexture = WindowsGraphicsCaptureInterop.ToneMapHdrTexture(texture, device, context, new Rectangle(0, 0, hdrImage.Width, hdrImage.Height),
                        hdrImage.SdrWhiteLevelInNits, hdrImage.DisplayPeakInNits, hdrImage);
                    try
                    {
                        return WindowsGraphicsCaptureInterop.TransformTextureToBitmap(sdrTexture, device, context);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(sdrTexture);
                    }
                }
                finally
                {
                    if (texture != null) Marshal.ReleaseComObject(texture);
                }
            });
        }

        /// <summary>
        /// The white level of HdrContent, everything above it clipped, sRGB encoded
        /// </summary>
        private static unsafe Bitmap ToBitmapOnCpu(HdrImage hdrImage)
        {
            var (peakInNits, highlightShare) = HdrContent.Measure(hdrImage, hdrImage.SdrWhiteLevelInNits);
            float whiteLevelInNits = HdrContent.WhiteLevelInNits(hdrImage.SdrWhiteLevelInNits, hdrImage.DisplayPeakInNits, peakInNits, highlightShare);
            // scRGB 1.0 is 80 nits
            float scale = 80f / whiteLevelInNits;
            Log.Info($"HDR tone mapping {hdrImage.Width}x{hdrImage.Height} on the CPU: white at {whiteLevelInNits:F0} nits.");

            var encode = new byte[4097];
            for (int i = 0; i < encode.Length; i++)
            {
                double linear = i / 4096.0;
                double srgb = linear <= 0.0031308 ? linear * 12.92 : 1.055 * Math.Pow(linear, 1 / 2.4) - 0.055;
                encode[i] = (byte)Math.Round(Math.Min(Math.Max(srgb, 0), 1) * 255);
            }
            // NaN (FP16 can have it) is black, like below 0
            byte Encode(float value) => float.IsNaN(value) ? (byte)0 : encode[(int)(Math.Min(Math.Max(value * scale, 0f), 1f) * 4096)];

            var bitmap = new Bitmap(hdrImage.Width, hdrImage.Height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                hdrImage.Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        byte* target = (byte*)bitmapData.Scan0 + (long)y * bitmapData.Stride;
                        for (int x = 0; x < row.Length; x++, target += 4)
                        {
                            var pixel = row[x].ToVector4();
                            // BGRA in memory
                            target[0] = Encode(pixel.Z);
                            target[1] = Encode(pixel.Y);
                            target[2] = Encode(pixel.X);
                            target[3] = (byte)Math.Round(Math.Min(Math.Max(pixel.W, 0f), 1f) * 255);
                        }
                    }
                });
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            return bitmap;
        }
    }
}
