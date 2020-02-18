using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    /// <summary>
    /// Used for User32.SetWinEventHook
    /// See: http://msdn.microsoft.com/en-us/library/windows/desktop/dd373606%28v=vs.85%29.aspx#OBJID_WINDOW
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum EventObjects
    {
        OBJID_ALERT = -10,
        OBJID_CARET = -8,
        OBJID_CLIENT = -4,
        OBJID_CURSOR = -9,
        OBJID_HSCROLL = -6,
        OBJID_MENU = -3,
        OBJID_SIZEGRIP = -7,
        OBJID_SOUND = -11,
        OBJID_SYSMENU = -1,
        OBJID_TITLEBAR = -2,
        OBJID_VSCROLL = -5,
        OBJID_WINDOW = 0
    }
}