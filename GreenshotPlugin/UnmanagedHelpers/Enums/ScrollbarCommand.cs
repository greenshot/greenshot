using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ScrollbarCommand
    {
        SB_LINEUP = 0, // Scrolls one line up.
        SB_LINEDOWN = 1, // Scrolls one line down.
        SB_PAGEUP = 2, // Scrolls one page up.
        SB_PAGEDOWN = 3, // Scrolls one page down.
        SB_THUMBPOSITION = 4, // The user has dragged the scroll box (thumb) and released the mouse button. The high-order word indicates the position of the scroll box at the end of the drag operation.
        SB_THUMBTRACK = 5, // The user is dragging the scroll box. This message is sent repeatedly until the user releases the mouse button. The high-order word indicates the position that the scroll box has been dragged to.
        SB_TOP = 6, // Scrolls to the upper left.
        SB_BOTTOM = 7, // Scrolls to the lower right.
        SB_ENDSCROLL = 8 // Ends scroll.
    }
}