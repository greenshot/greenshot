using System;
using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DWM_BB {
        Enable = 1,
        BlurRegion = 2,
        TransitionMaximized = 4
    }
}