using System;

namespace Greenshot.Gfx.Experimental.Structs
{
    public struct SpanInfo
    {
        public IntPtr Pointer;
        public int PixelStride;
        public int Height;
        public int Width;
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
        public int BitmapSize;

    }
}
