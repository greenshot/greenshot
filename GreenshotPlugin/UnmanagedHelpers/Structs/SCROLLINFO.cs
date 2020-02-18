using System;
using System.Runtime.InteropServices;

namespace GreenshotPlugin.UnmanagedHelpers.Structs
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct SCROLLINFO {
        public int cbSize;
        public int fMask;
        public int nMin;
        public int nMax;
        public int nPage;
        public int nPos;
        public int nTrackPos;
    }
}