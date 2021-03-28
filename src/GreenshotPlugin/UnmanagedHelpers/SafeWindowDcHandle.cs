using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace GreenshotPlugin.UnmanagedHelpers
{
    /// <summary>
    /// A WindowDC SafeHandle implementation
    /// </summary>
    public class SafeWindowDcHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [DllImport("user32", SetLastError = true)]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32", SetLastError = true)]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private readonly IntPtr _hWnd;

        /// <summary>
        /// Needed for marshalling return values
        /// </summary>
        public SafeWindowDcHandle() : base(true)
        {
        }

        [SecurityCritical]
        public SafeWindowDcHandle(IntPtr hWnd, IntPtr preexistingHandle) : base(true)
        {
            _hWnd = hWnd;
            SetHandle(preexistingHandle);
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        protected override bool ReleaseHandle()
        {
            bool returnValue = ReleaseDC(_hWnd, handle);
            return returnValue;
        }

        /// <summary>
        /// Creates a DC as SafeWindowDcHandle for the whole of the specified hWnd
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>SafeWindowDcHandle</returns>
        public static SafeWindowDcHandle FromWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            var hDcDesktop = GetWindowDC(hWnd);
            return new SafeWindowDcHandle(hWnd, hDcDesktop);
        }

        public static SafeWindowDcHandle FromDesktop()
        {
            IntPtr hWndDesktop = User32.GetDesktopWindow();
            IntPtr hDCDesktop = GetWindowDC(hWndDesktop);
            return new SafeWindowDcHandle(hWndDesktop, hDCDesktop);
        }
    }
}