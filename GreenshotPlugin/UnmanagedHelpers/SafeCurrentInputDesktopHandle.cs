using System;
using System.Security.Permissions;
using GreenshotPlugin.UnmanagedHelpers.Enums;
using log4net;
using Microsoft.Win32.SafeHandles;

namespace GreenshotPlugin.UnmanagedHelpers
{
    /// <summary>
    /// A SafeHandle class implementation for the current input desktop
    /// </summary>
    public class SafeCurrentInputDesktopHandle : SafeHandleZeroOrMinusOneIsInvalid {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(SafeCurrentInputDesktopHandle));

        public SafeCurrentInputDesktopHandle() : base(true) {
            IntPtr hDesktop = User32.OpenInputDesktop(0, true, DesktopAccessRight.GENERIC_ALL);
            if (hDesktop != IntPtr.Zero) {
                SetHandle(hDesktop);
                if (User32.SetThreadDesktop(hDesktop)) {
                    LOG.DebugFormat("Switched to desktop {0}", hDesktop);
                } else {
                    LOG.WarnFormat("Couldn't switch to desktop {0}", hDesktop);
                    LOG.Error(User32.CreateWin32Exception("SetThreadDesktop"));
                }
            } else {
                LOG.Warn("Couldn't get current desktop.");
                LOG.Error(User32.CreateWin32Exception("OpenInputDesktop"));
            }
        }

        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        protected override bool ReleaseHandle() {
            return User32.CloseDesktop(handle);
        }
    }
}