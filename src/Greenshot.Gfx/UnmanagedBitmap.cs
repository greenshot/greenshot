#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Greenshot.Gfx.Structs;

namespace Greenshot.Gfx
{
    /// <summary>
    /// A bitmap wraper with memory from Marshal.AllocHGlobal
    /// </summary>
    /// <typeparam name="TPixelLayout">struct for the pixel information</typeparam>
    public class UnmanagedBitmap<TPixelLayout> : IDisposable where TPixelLayout : unmanaged
    {
        private readonly int _byteLength;
        private readonly int _bytesPerPixel;
        private readonly int _pixels;
        private readonly int _stride;
        private bool _isDisposed;
        private readonly IntPtr _hGlobal;

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
        public UnmanagedBitmap(int width, int height)
        {
            _bytesPerPixel = Marshal.SizeOf<TPixelLayout>();
            Width = width;
            Height = height;
            _pixels = height * width;
            _stride = _bytesPerPixel * width;
            _byteLength = height * _stride;
            _hGlobal = Marshal.AllocHGlobal(_byteLength);
            GC.AddMemoryPressure(_byteLength);
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
                    var pixelRowPointer = _hGlobal + (y * _stride);
                    return new Span<TPixelLayout>(pixelRowPointer.ToPointer(), Width);
                }
            }
        }

        /// <inheritdoc />
        public Span<TPixelLayout> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return new Span<TPixelLayout>(_hGlobal.ToPointer(), _pixels);
                }
            }
        }

        /// <summary>
        /// Convert this to a real bitmap
        /// </summary>
        /// <returns>Bitmap</returns>
        public Bitmap AsBitmap()
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
            return new Bitmap(Width, Height, _stride, format, _hGlobal);
        }

        #region Implementation of IDisposable
        /// <summary>
        /// The actual dispose
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Marshal.FreeHGlobal(_hGlobal);
            GC.RemoveMemoryPressure(_byteLength);
        }

        #endregion
    }
}
