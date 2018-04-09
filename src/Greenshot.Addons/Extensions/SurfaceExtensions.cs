using System.IO;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

namespace Greenshot.Addons.Extensions
{
    public static class SurfaceExtensions
    {
        /// <summary>
        /// Create OutputSettings
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <returns>SurfaceOutputSettings</returns>
        public static SurfaceOutputSettings GenerateOutputSettings(this ISurface surface, IFileConfiguration fileConfiguration)
        {
            return new SurfaceOutputSettings(fileConfiguration);
        }

        /// <summary>
        /// Generate a filename
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <returns>Filename</returns>
        public static string GenerateFilename(this ISurface surface, IFileConfiguration fileConfiguration)
        {
            return Path.GetFileName(FilenameHelper.GetFilenameFromPattern(fileConfiguration.OutputFileFilenamePattern, fileConfiguration.OutputFileFormat, surface.CaptureDetails));
        }

        /// <summary>
        /// Generate a mimetype
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <returns>image with the format</returns>
        public static string GenerateMimeType(this ISurface surface, IFileConfiguration fileConfiguration)
        {
            return "image/" + fileConfiguration.OutputFileFormat;
        }

        /// <summary>
        /// Write the ISurface to the specified stream by using the supplied IFileConfiguration
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <param name="stream">Stream</param>
        public static void WriteToStream(this ISurface surface, IFileConfiguration fileConfiguration, Stream stream)
        {
            ImageOutput.SaveToStream(surface, stream, surface.GenerateOutputSettings(fileConfiguration));
        }
    }
}
