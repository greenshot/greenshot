using System;
using System.Runtime.InteropServices;

namespace GreenshotPlugin.Core.Credentials
{
    /// <summary>
    ///     http://www.pinvoke.net/default.aspx/Structures.CREDUI_INFO
    ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/credui_info.asp
    /// </summary>
    public struct CredUiInfo
    {
        public int cbSize;
        public IntPtr hwndParent;
        [MarshalAs(UnmanagedType.LPWStr)] public string pszMessageText;
        [MarshalAs(UnmanagedType.LPWStr)] public string pszCaptionText;
        public IntPtr hbmBanner;
    }
}