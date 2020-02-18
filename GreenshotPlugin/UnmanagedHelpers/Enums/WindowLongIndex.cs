using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum WindowLongIndex
    {
        GWL_EXSTYLE = -20,	// Sets a new extended window style.
        GWL_HINSTANCE = -6,	// Sets a new application instance handle.
        GWL_ID = -12,	// Sets a new identifier of the child window. The window cannot be a top-level window.
        GWL_STYLE = -16,	// Sets a new window style.
        GWL_USERDATA = -21,	// Sets the user data associated with the window. This data is intended for use by the application that created the window. Its value is initially zero.
        GWL_WNDPROC = -4 // Sets a new address for the window procedure. You cannot change this attribute if the window does not belong to the same process as the calling thread.
    }
}