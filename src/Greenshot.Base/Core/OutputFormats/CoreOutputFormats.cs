namespace Greenshot.Base.Core.OutputFormats;

public static class CoreOutputFormats
{
    public static void RegisterCoreOutputFormats(IOutputFormatRegistry registry)
    {
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Bmp,
            WellKnownOutputFormats.Bmp,
            "Bitmap Image File (BMP)"));
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Gif,
            WellKnownOutputFormats.Gif,
            "Graphics Interchange Format (GIF)"));
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Jpg,
            WellKnownOutputFormats.Jpg,
            "JPEG (Joint Photographic Experts Group)"));
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Png,
            WellKnownOutputFormats.Png,
            "Portable Network Graphics (PNG)"));
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Tiff,
            WellKnownOutputFormats.Tiff,
            "Tagged Image File Format (TIFF)"));
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Jxr,
            WellKnownOutputFormats.Jxr,
            "JPEG XR"));
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Greenshot,
            WellKnownOutputFormats.Greenshot,
            "Greenshot Image"));
        registry.Register(new OutputFormatDefinition(
            WellKnownOutputFormats.Ico,
            WellKnownOutputFormats.Ico,
            "Windows Icon (ICO)"));
    }
}
