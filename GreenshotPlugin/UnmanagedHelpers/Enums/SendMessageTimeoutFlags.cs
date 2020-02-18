using System;
using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    /// <summary>
    /// See: http://www.pinvoke.net/default.aspx/Enums/SendMessageTimeoutFlags.html
    /// </summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum SendMessageTimeoutFlags : uint {
        SMTO_NORMAL				= 0x0,
        SMTO_BLOCK				= 0x1,
        SMTO_ABORTIFHUNG		= 0x2,
        SMTO_NOTIMEOUTIFNOTHUNG	= 0x8
    }
}