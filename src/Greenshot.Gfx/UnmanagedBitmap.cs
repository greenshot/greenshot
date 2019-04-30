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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Gfx.Structs;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Greenshot.Gfx
{
    /// <summary>
    /// A bitmap wrapper with memory from Marshal.AllocHGlobal
    /// </summary>
    /// <typeparam name="TPixelLayout">struct for the pixel information</typeparam>
    public class UnmanagedBitmap<TPixelLayout> : IBitmapWithNativeSupport where TPixelLayout : unmanaged
    {
        private readonly float _horizontalPixelsPerInch;
        private readonly float _verticalPixelsPerInch;
        // Bytes per line
        private readonly int _stride;
        // IntPtr to a global handle with the bitmap data, this will be freed on dispose
        private IntPtr _hGlobal;
        // IntPtr to the bits of the bitmap, if using the constructor with the IntPtr this will not be freed
        private readonly IntPtr _bits;
        // Optionally created when the user calls NativeBitmap
        private Bitmap _nativeBitmap;

        /// <summary>
        /// Width of the bitmap
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the bitmap
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The constructor for the UnmanagedBitmap
        /// </summary>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <param name="horizontalPixelsPerInch">float</param>
        /// <param name="verticalPixelsPerInch">float</param>
        public UnmanagedBitmap(int width, int height, float horizontalPixelsPerInch = 0.96f, float verticalPixelsPerInch = 0.96f)
        {
            _horizontalPixelsPerInch = horizontalPixelsPerInch;
            _verticalPixelsPerInch = verticalPixelsPerInch;
            var bytesPerPixel = Marshal.SizeOf<TPixelLayout>();
            Width = width;
            Height = height;
            _stride = bytesPerPixel * width;
            var bytesAllocated = height * _stride;
            _bits = _hGlobal = Marshal.AllocHGlobal(bytesAllocated);
            GC.AddMemoryPressure(bytesAllocated);
        }


        /// <summary>
        /// The constructor for the UnmanagedBitmap with already initialized bits
        /// </summary>
        /// <param name="bits">IntPtr to the bits, this will not be freed</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <param name="horizontalPixelsPerInch">float</param>
        /// <param name="verticalPixelsPerInch">float</param>
        public UnmanagedBitmap(IntPtr bits, int width, int height, float horizontalPixelsPerInch = 0.96f, float verticalPixelsPerInch = 0.96f)
        {
            _horizontalPixelsPerInch = horizontalPixelsPerInch;
            _verticalPixelsPerInch = verticalPixelsPerInch;
            var bytesPerPixel = Marshal.SizeOf<TPixelLayout>();
            Width = width;
            Height = height;
            _stride = bytesPerPixel * width;
            _bits = bits;
        }

        /// <summary>
        /// This returns a span with a the pixels for the specified line
        /// </summary>
        /// <param name="y">int with y position</param>
        /// <returns>Span of TPixelLayout for the line</returns>
        public Span<TPixelLayout> this[int y] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    var pixelRowPointer = _bits + (y * _stride);
                    return new Span<TPixelLayout>(pixelRowPointer.ToPointer(), Width);
                }
            }
        }

        /// <summary>
        /// This returns a span with all the pixels
        /// </summary>
        /// <returns>Span of TPixelLayout for the line</returns>
        public Span<TPixelLayout> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return new Span<TPixelLayout>(_bits.ToPointer(), Height * Width);
                }
            }
        }

        /// <inheritdoc />
        public PixelFormat PixelFormat
        {
            get
            {
                PixelFormat format;
                TPixelLayout empty = default;
                switch (empty)
                {
                    case Bgr24 _:
                        format = PixelFormat.Format24bppRgb;
                        break;
                    case Bgra32 _:
                        format = PixelFormat.Format32bppArgb;
                        break;
                    case Bgr32 _:
                        format = PixelFormat.Format32bppRgb;
                        break;
                    default:
                        throw new NotSupportedException("Unknown pixel format");
                }

                return format;
            }
        }
        
        /// <summary>
        /// Returns the pixel format for this Unmanaged bitmap
        /// </summary>
        public System.Windows.Media.PixelFormat WpfPixelFormat
        {
            get
            {
                TPixelLayout empty = default;
                switch (empty)
                {
                    case Bgr24 _:
                        return PixelFormats.Bgr24;
                    case Bgra32 _:
                        return PixelFormats.Bgra32;
                    case Bgr32 _:
                        return PixelFormats.Bgr32;
                    default:
                        throw new NotSupportedException("Unknown pixel format");
                }
            }
        }

        /// <inheritdoc />
        public float VerticalResolution => _verticalPixelsPerInch;

        /// <inheritdoc />
        public float HorizontalResolution => _horizontalPixelsPerInch;

        /// <summary>
        /// Convert this to a real bitmap
        /// </summary>
        /// <returns>Bitmap</returns>
        public Bitmap NativeBitmap
        {
            get
            {
                if (_nativeBitmap == null)
                {
                    _nativeBitmap = new Bitmap(Width, Height, _stride, PixelFormat, _bits);
                }
                return _nativeBitmap;
            }
        }
        
        /// <summary>
        /// Convert this to a real bitmap
        /// </summary>
        /// <returns>BitmapSource</returns>
        public BitmapSource NativeBitmapSource
        {
            get
            {
                return BitmapSource.Create(Width, Height, HorizontalResolution, VerticalResolution, WpfPixelFormat, null, _bits, _stride * Height, _stride);
            }
        }

        /// <inheritdoc/>
        public Size Size => new Size(Width, Height);

        /// <summary>
        /// The actual dispose
        /// </summary>
        public void Dispose()
        {
            _nativeBitmap?.Dispose();
            if (_hGlobal == IntPtr.Zero)
            {
                return;
            }
            Marshal.FreeHGlobal(_hGlobal);
            _hGlobal = IntPtr.Zero;
            GC.RemoveMemoryPressure(Height * _stride);
        }
    }
}
