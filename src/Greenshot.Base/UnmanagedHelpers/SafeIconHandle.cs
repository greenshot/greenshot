using System;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Greenshot.Base.UnmanagedHelpers
{
    /// <summary>
    /// A SafeHandle class implementation for the hIcon
    /// </summary>
    public class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Needed for marshalling return values
        /// </summary>
        [SecurityCritical]
        public SafeIconHandle() : base(true)
        {
        }


        public SafeIconHandle(IntPtr hIcon) : base(true)
        {
            SetHandle(hIcon);
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        protected override bool ReleaseHandle()
        {
            return User32.DestroyIcon(handle);
        }
    }
}