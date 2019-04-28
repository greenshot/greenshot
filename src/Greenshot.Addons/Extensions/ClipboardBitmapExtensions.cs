// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.Structs;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Core.Enums;
using Greenshot.Gfx;

namespace Greenshot.Addons.Extensions
{
    public static class ClipboardBitmapExtensions
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly string[] SupportedBitmapFormats = 
        {
            "PNG",
            "PNG+Office Art",
            "Format17",
            "JPG",
            "JFIF",
            "JFIF+Office Art",
            "GIF",
            StandardClipboardFormats.Bitmap.AsString()
        };

        private static readonly string[] SupportedExtensions =
        {
            ".png",
            ".jpeg",
            ".jpg",
            ".bmp",
            ".gif"
        };

        /// <summary>
        /// Is there a Bitmap on the clipboard?
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <returns>bool</returns>
        public static bool HasImage(this IClipboardAccessToken clipboardAccessToken)
        {
            var formats = clipboardAccessToken.AvailableFormats();
            if (formats.Intersect(SupportedBitmapFormats).Any())
            {
                return true;
            }

            return clipboardAccessToken.GetFilenames()
                .Select(filename => Path.GetExtension(filename).ToLowerInvariant())
                .Intersect(SupportedExtensions)
                .Any();
        }

        /// <summary>
        /// Get a DIB from the Clipboard
        /// </summary>
        /// <param name="clipboardAccessToken"></param>
        /// <returns>Bitmap or null</returns>
        public static IBitmapWithNativeSupport GetAsDeviceIndependendBitmap(this IClipboardAccessToken clipboardAccessToken)
        {
            var formats = clipboardAccessToken.AvailableFormats().ToList();
            if (!formats.Contains(StandardClipboardFormats.Bitmap.AsString()))
            {
                return null;
            }

            var format17Bytes = clipboardAccessToken.GetAsBytes(StandardClipboardFormats.Bitmap.AsString());
            var infoHeader = BinaryStructHelper.FromByteArray<BitmapInfoHeader>(format17Bytes);
            if (infoHeader.IsDibV5)
            {
                Log.Warn().WriteLine("Getting DIBV5 (format 17) when requesting DIB");
                return null;
            }

            // Bitmap version older than 5
            var fileHeaderSize = Marshal.SizeOf(typeof(BitmapFileHeader));
            var fileHeader = BitmapFileHeader.Create(infoHeader);
            var fileHeaderBytes = BinaryStructHelper.ToByteArray(fileHeader);

            using (var bitmapStream = new MemoryStream())
            {
                bitmapStream.Write(fileHeaderBytes, 0, fileHeaderSize);
                bitmapStream.Write(format17Bytes, 0, format17Bytes.Length);
                bitmapStream.Seek(0, SeekOrigin.Begin);
                var image = BitmapHelper.FromStream(bitmapStream);
                if (image != null)
                {
                    return image;
                }
            }
            return null;
        }

        /// <summary>
        /// A special format 17 bitmap reader
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <returns>Bitmap or null</returns>
        public static IBitmapWithNativeSupport GetAsFormat17(this IClipboardAccessToken clipboardAccessToken)
        {
            var formats = clipboardAccessToken.AvailableFormats().ToList();
            if (!formats.Contains("Format17"))
            {
                return null;
            }

            var format17Bytes = clipboardAccessToken.GetAsBytes("Format17");
            var infoHeader = BinaryStructHelper.FromByteArray<BitmapInfoHeader>(format17Bytes);
            if (!infoHeader.IsDibV5)
            {
                return null;
            }

            // Using special DIBV5 / Format17 format reader
            // CF_DIBV5
            var gcHandle = IntPtr.Zero;
            try
            {
                var handle = GCHandle.Alloc(format17Bytes, GCHandleType.Pinned);
                gcHandle = GCHandle.ToIntPtr(handle);
                return
                    BitmapWrapper.FromBitmap(
                        new Bitmap(infoHeader.Width, infoHeader.Height,
                            -(int) (infoHeader.SizeImage / infoHeader.Height),
                            infoHeader.BitCount == 32 ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb,
                            new IntPtr(handle.AddrOfPinnedObject().ToInt32() + infoHeader.OffsetToPixels +
                                       (infoHeader.Height - 1) * (int) (infoHeader.SizeImage / infoHeader.Height))
                        ));
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Problem retrieving Format17 from clipboard.");
            }
            finally
            {
                if (gcHandle == IntPtr.Zero)
                {
                    GCHandle.FromIntPtr(gcHandle).Free();
                }
            }

            return null;
        }

        /// <summary>
        /// Place the bitmap on the clipboard
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <param name="surface">ISurface</param>
        /// <param name="outputSettings">SurfaceOutputSettings specifying how to output the surface</param>
        public static void SetAsBitmap(this IClipboardAccessToken clipboardAccessToken, ISurface surface, SurfaceOutputSettings outputSettings)
        {
            using (var bitmapStream = new MemoryStream())
            {
                ImageOutput.SaveToStream(surface, bitmapStream, outputSettings);
                bitmapStream.Seek(0, SeekOrigin.Begin);
                // Set the stream
                var clipboardFormat = ClipboardFormatExtensions.MapFormatToId(outputSettings.Format.ToString().ToUpperInvariant());
                clipboardAccessToken.SetAsStream(clipboardFormat, bitmapStream);
            }
        }

        /// <summary>
        /// Place the surface as Format17 bitmap on the clipboard
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <param name="surface">ISurface</param>
        public static void SetAsFormat17(this IClipboardAccessToken clipboardAccessToken, ISurface surface, ICoreConfiguration coreConfiguration)
        {
            // Create the stream for the clipboard
            using (var dibV5Stream = new MemoryStream())
            {
                var outputSettings = new SurfaceOutputSettings(coreConfiguration, OutputFormats.bmp, 100, false);
                bool dispose = ImageOutput.CreateBitmapFromSurface(surface, outputSettings, out var bitmapToSave);
                // Create the BITMAPINFOHEADER
                var header = BitmapInfoHeader.Create(bitmapToSave.Width, bitmapToSave.Height, 32);
                // Make sure we have BI_BITFIELDS, this seems to be normal for Format17?
                header.Compression = BitmapCompressionMethods.BI_BITFIELDS;

                var headerBytes = BinaryStructHelper.ToByteArray(header);
                // Write the BITMAPINFOHEADER to the stream
                dibV5Stream.Write(headerBytes, 0, headerBytes.Length);

                // As we have specified BI_COMPRESSION.BI_BITFIELDS, the BitfieldColorMask needs to be added
                var colorMask = BitfieldColorMask.Create();
                // Create the byte[] from the struct
                var colorMaskBytes = BinaryStructHelper.ToByteArray(colorMask);
                Array.Reverse(colorMaskBytes);
                // Write to the stream
                dibV5Stream.Write(colorMaskBytes, 0, colorMaskBytes.Length);

                // Create the raw bytes for the pixels only
                var bitmapBytes = BitmapToByteArray(bitmapToSave);
                // Write to the stream
                dibV5Stream.Write(bitmapBytes, 0, bitmapBytes.Length);
                // Reset the stream to the beginning so it can be written
                dibV5Stream.Seek(0, SeekOrigin.Begin);
                // Set the DIBv5 to the clipboard DataObject
                clipboardAccessToken.SetAsStream("Format17", dibV5Stream);
                if (dispose)
                {
                    bitmapToSave.Dispose();
                }
            }
        }

        /// <summary>
        ///     Helper method so get the bitmap bytes
        ///     See: http://stackoverflow.com/a/6570155
        /// </summary>
        /// <param name="bitmap">IBitmapWithNativeSupport</param>
        /// <returns>byte[]</returns>
        private static byte[] BitmapToByteArray(IBitmapWithNativeSupport bitmap)
        {
            // Lock the bitmap's bits.  
            var rect = new NativeRect(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.NativeBitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var absStride = Math.Abs(bmpData.Stride);
            var bytes = absStride * bitmap.Height;
            long ptr = bmpData.Scan0.ToInt32();
            // Declare an array to hold the bytes of the bitmap.
            var rgbValues = new byte[bytes];

            for (var i = 0; i < bitmap.Height; i++)
            {
                var pointer = new IntPtr(ptr + bmpData.Stride * i);
                Marshal.Copy(pointer, rgbValues, absStride * (bitmap.Height - i - 1), absStride);
            }

            // Unlock the bits.
            bitmap.NativeBitmap.UnlockBits(bmpData);

            return rgbValues;
        }

        /// <summary>
        /// Place the bitmap on the clipboard as DIB
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <param name="surface">ISurface</param>
        public static void SetAsDeviceIndependendBitmap(this IClipboardAccessToken clipboardAccessToken, ISurface surface, ICoreConfiguration coreConfiguration)
        {
            using (var bitmapStream = new MemoryStream())
            {
                ImageOutput.SaveToStream(surface, bitmapStream, new SurfaceOutputSettings(coreConfiguration) {Format = OutputFormats.bmp});
                bitmapStream.Seek(Marshal.SizeOf(typeof(BitmapFileHeader)), SeekOrigin.Begin);
                // Set the stream
                clipboardAccessToken.SetAsStream(StandardClipboardFormats.DeviceIndependentBitmap, bitmapStream);
            }
        }
    }
}
