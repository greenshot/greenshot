using System;
using System.Windows.Forms;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Provides an implementation of the IWin32Window interface for wrapping a native window handle (HWND).
    /// </summary>
    /// <remarks>This class is typically used to supply a window handle to APIs that require an IWin32Window
    /// implementation, such as dialog owners in Windows Forms. The handle should represent a valid native window.</remarks>
    public class Win32Shim : IWin32Window
    {
        public Win32Shim(IntPtr handle) {
            Handle = handle;
        }

        public IntPtr Handle { get; internal set; }
    }
}
