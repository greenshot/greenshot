using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ClassLongIndex
    {
        GCL_CBCLSEXTRA = -20, // the size, in bytes, of the extra memory associated with the class. Setting this value does not change the number of extra bytes already allocated.
        GCL_CBWNDEXTRA = -18, // the size, in bytes, of the extra window memory associated with each window in the class. Setting this value does not change the number of extra bytes already allocated. For information on how to access this memory, see SetWindowLong.
        GCL_HBRBACKGROUND = -10, // a handle to the background brush associated with the class.
        GCL_HCURSOR = -12, // a handle to the cursor associated with the class.
        GCL_HICON = -14, // a handle to the icon associated with the class.
        GCL_HICONSM = -34, // a handle to the small icon associated with the class.
        GCL_HMODULE = -16, // a handle to the module that registered the class.
        GCL_MENUNAME = -8, // the address of the menu name string. The string identifies the menu resource associated with the class.
        GCL_STYLE = -26, // the window-class style bits.
        GCL_WNDPROC = -24, // the address of the window procedure, or a handle representing the address of the window procedure. You must use the CallWindowProc function to call the window procedure. 
    }
}