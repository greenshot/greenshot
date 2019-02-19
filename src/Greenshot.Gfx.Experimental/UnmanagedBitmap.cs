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
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace Greenshot.Gfx.Experimental
{
    /// <summary>
    /// A bitmap with memory from Marshal.AllocHGlobal
    /// </summary>
    /// <typeparam name="TPixelLayout">struct for the pixel information</typeparam>
    public class UnmanagedBitmap<TPixelLayout> : MemoryManager<TPixelLayout> where TPixelLayout : unmanaged
    {
        private readonly object _lock = new object();
        private readonly int _byteLength;
        private readonly int _bytesPerPixel;
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
        /// The format of the pixels
        /// </summary>
        public PixelFormat Format { get; }

        /// <summary>
        /// The constructor for the UnmanagedBitmap
        /// </summary>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        /// <param name="pixelFormat">PixelFormat</param>
        public UnmanagedBitmap(int width, int height, PixelFormat pixelFormat)
        {
            _bytesPerPixel = Marshal.SizeOf<TPixelLayout>();
            Width = width;
            Height = height;
            Format = pixelFormat;
            _stride = width * _bytesPerPixel;
            _byteLength = height * _stride;
            _hGlobal = Marshal.AllocHGlobal(_byteLength);
            GC.AddMemoryPressure(_byteLength);
        }

        /// <inheritdoc />
        public override unsafe Span<TPixelLayout> GetSpan() => new Span<TPixelLayout>(_hGlobal.ToPointer(), _byteLength);

        /// <summary>
        /// Retrusn
        /// </summary>
        public override Memory<TPixelLayout> Memory => CreateMemory(0, Width * Height);

        /// <summary>
        /// NotSupportedException
        /// </summary>
        /// <param name="elementIndex">int</param>
        /// <returns>MemoryHandle</returns>
        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotSupportedException("Pinning not needed");
        }

        /// <summary>
        /// NotSupportedException
        /// </summary>
        public override void Unpin()
        {
            throw new NotSupportedException("Pinning not needed");
        }

        /// <summary>
        /// Convert this to a real bitmap
        /// </summary>
        /// <returns></returns>
        public Bitmap AsBitmap()
        {
            return new Bitmap(Width, Height, _stride, Format, _hGlobal);
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Dispose implementation
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                Marshal.FreeHGlobal(_hGlobal);
                GC.RemoveMemoryPressure(_byteLength);
            }
        }

        /// <summary>
        /// The actual dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
