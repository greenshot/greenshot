using System;
using Greenshot.Base.UnmanagedHelpers.Enums;

namespace Greenshot.Base.UnmanagedHelpers
{
    /// <summary>
    /// Used with SetWinEventHook
    /// </summary>
    /// <param name="hWinEventHook"></param>
    /// <param name="eventType"></param>
    /// <param name="hWnd"></param>
    /// <param name="idObject"></param>
    /// <param name="idChild"></param>
    /// <param name="dwEventThread"></param>
    /// <param name="dwmsEventTime"></param>
    public delegate void WinEventDelegate(IntPtr hWinEventHook, WinEvent eventType, IntPtr hWnd, EventObjects idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
}