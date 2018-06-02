using System.IO;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

namespace Greenshot.Addons.Extensions
{
    /// <summary>
    /// Extension methods to help with the ISurface
    /// </summary>
    public static class SurfaceExtensions
    {
        /// <summary>
        /// Choose what IFileConfiguration to use, global or the IDestinationFileConfiguration
        /// </summary>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <param name="destinationFileConfiguration">IDestinationFileConfiguration</param>
        /// <returns>IFileConfiguration</returns>
        public static IFileConfiguration Choose(this IFileConfiguration fileConfiguration, IDestinationFileConfiguration destinationFileConfiguration = null)
        {
            if (destinationFileConfiguration != null && !destinationFileConfiguration.UseOwnSettings)
            {
                return destinationFileConfiguration;
            }
            return fileConfiguration;
        }

        /// <summary>
        /// Create OutputSettings
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <param name="destinationFileConfiguration">IDestinationFileConfiguration</param>
        /// <returns>SurfaceOutputSettings</returns>
        public static SurfaceOutputSettings GenerateOutputSettings(this ISurface surface, IFileConfiguration fileConfiguration, IDestinationFileConfiguration destinationFileConfiguration = null)
        {
            return new SurfaceOutputSettings(fileConfiguration.Choose(destinationFileConfiguration));
        }

        /// <summary>
        /// Generate a filename
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <param name="destinationFileConfiguration">IDestinationFileConfiguration</param>
        /// <returns>Filename</returns>
        public static string GenerateFilename(this ISurface surface, IFileConfiguration fileConfiguration, IDestinationFileConfiguration destinationFileConfiguration = null)
        {
            var selectedFileConfiguration = fileConfiguration.Choose(destinationFileConfiguration);
            return Path.GetFileName(FilenameHelper.GetFilenameFromPattern(selectedFileConfiguration.OutputFileFilenamePattern, selectedFileConfiguration.OutputFileFormat, surface.CaptureDetails));
        }

        /// <summary>
        /// Generate a mimetype, used when uploading
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <param name="destinationFileConfiguration">IDestinationFileConfiguration</param>
        /// <returns>image with the format</returns>
        public static string GenerateMimeType(this ISurface surface, IFileConfiguration fileConfiguration, IDestinationFileConfiguration destinationFileConfiguration = null)
        {
            return "image/" + fileConfiguration.Choose(destinationFileConfiguration).OutputFileFormat;
        }

        /// <summary>
        /// Write the ISurface to the specified stream by using the supplied IFileConfiguration
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="stream">Stream</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <param name="destinationFileConfiguration">IDestinationFileConfiguration</param>
        public static void WriteToStream(this ISurface surface, Stream stream, IFileConfiguration fileConfiguration, IDestinationFileConfiguration destinationFileConfiguration = null)
        {
            ImageOutput.SaveToStream(surface, stream, surface.GenerateOutputSettings(fileConfiguration.Choose(destinationFileConfiguration)));
        }

        /// <summary>
        /// Write the ISurface to the specified stream by using the supplied IFileConfiguration
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <param name="fileConfiguration">IFileConfiguration</param>
        /// <param name="destinationFileConfiguration">IDestinationFileConfiguration</param>
        public static string SaveNamedTmpFile(this ISurface surface, IFileConfiguration fileConfiguration, IDestinationFileConfiguration destinationFileConfiguration = null)
        {
            var outputSettings = surface.GenerateOutputSettings(fileConfiguration.Choose(destinationFileConfiguration));
            return ImageOutput.SaveNamedTmpFile(surface, surface.CaptureDetails, outputSettings);
        }
    }
}
