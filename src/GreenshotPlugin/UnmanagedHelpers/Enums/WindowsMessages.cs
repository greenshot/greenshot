using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    /// <summary>
    ///     All possible windows messages
    /// See also <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644927(v=vs.85).aspx#system_defined">here</a>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowsMessages : uint
    {
        WM_MOUSEACTIVATE = 0x0021,
        WM_INPUTLANGCHANGEREQUEST = 0x0050,
        WM_INPUTLANGCHANGE = 0x0051,


        /// <summary>
        /// Sent to a window to retrieve a handle to the large or small icon associated with a window. The system displays the large icon in the ALT+TAB dialog, and the small icon in the window caption.
        /// A window receives this message through its WindowProc function.
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632625.aspx">WM_GETICON message</a>
        /// </summary>
        WM_GETICON = 0x007F,
        WM_CHAR = 0x0102,
        WM_SYSCOMMAND = 0x0112
    }
}