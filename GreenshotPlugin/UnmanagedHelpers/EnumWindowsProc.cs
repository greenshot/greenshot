using System;

namespace GreenshotPlugin.UnmanagedHelpers
{
    /// <summary>
    /// Used with EnumWindows or EnumChildWindows
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    public delegate int EnumWindowsProc(IntPtr hwnd, int lParam);
}