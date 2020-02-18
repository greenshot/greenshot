using System.Diagnostics.CodeAnalysis;

namespace GreenshotPlugin.UnmanagedHelpers.Enums
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum RegionResult {
        REGION_ERROR = 0,
        REGION_NULLREGION = 1,
        REGION_SIMPLEREGION = 2,
        REGION_COMPLEXREGION = 3
    }
}