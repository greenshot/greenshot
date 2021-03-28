using System;
using System.Diagnostics.CodeAnalysis;

namespace Greenshot.Base.UnmanagedHelpers.Enums
{
    /// <summary>
    /// Used for User32.SetWinEventHook
    /// See: http://msdn.microsoft.com/en-us/library/windows/desktop/dd373640%28v=vs.85%29.aspx
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming"), Flags]
    public enum WinEventHookFlags
    {
        WINEVENT_SKIPOWNTHREAD = 1,
        WINEVENT_SKIPOWNPROCESS = 2,
        WINEVENT_OUTOFCONTEXT = 0,
        WINEVENT_INCONTEXT = 4
    }
}