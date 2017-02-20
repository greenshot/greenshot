using System;

namespace GreenshotPlugin.Core.Credentials
{
    /// <summary>
    ///     http://www.pinvoke.net/default.aspx/Enums.CREDUI_FLAGS
    ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnnetsec/html/dpapiusercredentials.asp
    ///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/creduipromptforcredentials.asp
    /// </summary>
    [Flags]
    public enum CredFlags
    {
        INCORRECT_PASSWORD = 0x1,
        DO_NOT_PERSIST = 0x2,
        REQUEST_ADMINISTRATOR = 0x4,
        EXCLUDE_CERTIFICATES = 0x8,
        REQUIRE_CERTIFICATE = 0x10,
        SHOW_SAVE_CHECK_BOX = 0x40,
        ALWAYS_SHOW_UI = 0x80,
        REQUIRE_SMARTCARD = 0x100,
        PASSWORD_ONLY_OK = 0x200,
        VALIDATE_USERNAME = 0x400,
        COMPLETE_USERNAME = 0x800,
        PERSIST = 0x1000,
        SERVER_CREDENTIAL = 0x4000,
        EXPECT_CONFIRMATION = 0x20000,
        GENERIC_CREDENTIALS = 0x40000,
        USERNAME_TARGET_CREDENTIALS = 0x80000,
        KEEP_USERNAME = 0x100000
    }
}