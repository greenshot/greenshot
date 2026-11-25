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
using System.Runtime.InteropServices;

namespace Greenshot.Plugin.RapidOcr.Recognition;

/// <summary>
/// Memory outside the managed heap for the large buffers of the OCR (the pixels of a capture, the masks of the detection): on the
/// managed heap anything from 85000 bytes goes to the large object heap, which is only collected with a full garbage collection.
/// Its content is zero when it is created.
/// </summary>
internal sealed unsafe class NativeBuffer<T> : IDisposable where T : unmanaged
{
    private IntPtr _memory;

    public NativeBuffer(int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        _memory = Marshal.AllocHGlobal(new IntPtr(Math.Max(1L, (long)length * sizeof(T))));
        Length = length;
        Span.Clear();
    }

    public int Length { get; private set; }

    public Span<T> Span => _memory == IntPtr.Zero ? throw new ObjectDisposedException(nameof(NativeBuffer<T>)) : new Span<T>((void*)_memory, Length);

    /// <summary>
    /// More room, the content is kept (the added part isn't cleared)
    /// </summary>
    public void Grow(int length)
    {
        if (length <= Length) return;
        _memory = Marshal.ReAllocHGlobal(_memory, new IntPtr((long)length * sizeof(T)));
        Length = length;
    }

    public void Dispose()
    {
        Free();
        GC.SuppressFinalize(this);
    }

    ~NativeBuffer() => Free();

    private void Free()
    {
        if (_memory == IntPtr.Zero) return;
        Marshal.FreeHGlobal(_memory);
        _memory = IntPtr.Zero;
        Length = 0;
    }
}
