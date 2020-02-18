using System;
using System.Runtime.InteropServices;

namespace GreenshotPlugin.UnmanagedHelpers.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CursorInfo {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }
}