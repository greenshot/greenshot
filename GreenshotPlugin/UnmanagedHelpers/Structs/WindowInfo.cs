using System;
using System.Runtime.InteropServices;

namespace GreenshotPlugin.UnmanagedHelpers.Structs
{
    /// <summary>
    /// The structure for the WindowInfo
    /// See: http://msdn.microsoft.com/en-us/library/windows/desktop/ms632610%28v=vs.85%29.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct WindowInfo {
        public uint cbSize;
        public RECT rcWindow;
        public RECT rcClient;
        public uint dwStyle;
        public uint dwExStyle;
        public uint dwWindowStatus;
        public uint cxWindowBorders;
        public uint cyWindowBorders;
        public ushort atomWindowType;
        public ushort wCreatorVersion;
        // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
        public WindowInfo(bool? filler) : this() {
            cbSize = (uint)(Marshal.SizeOf(typeof(WindowInfo)));
        }
    }
}