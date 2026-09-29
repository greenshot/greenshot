using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Interfaces.Plugin
{
    /// <summary>
    /// This interface is the GreenshotPluginHost, that which "Hosts" the plugin.
    /// For Greenshot this is implemented in the PluginHelper
    /// </summary>
    public interface IGreenshotHost
    {
        /// <summary>
        /// Create a Thumbnail
        /// </summary>
        /// <param name="image">Image of which we need a Thumbnail</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>Image with Thumbnail</returns>
        Image GetThumbnail(Image image, int width, int height);

        /// <summary>
        /// Export a surface to the destination with has the supplied designation
        /// </summary>
        /// <param name="manuallyInitiated"></param>
        /// <param name="designation"></param>
        /// <param name="surface"></param>
        /// <param name="captureDetails"></param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>the result of the export, the surface is updated with it</returns>
        Task<ExportResult> ExportCaptureAsync(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails, CancellationToken cancellationToken = default);

        /// <summary>
        /// Make region capture with specified Handler
        /// </summary>
        /// <param name="captureMouseCursor">bool false if the mouse should not be captured, true if the configuration should be checked</param>
        /// <param name="destination">IDestination destination</param>
        void CaptureRegion(bool captureMouseCursor, IDestination destination);

        /// <summary>
        /// Use the supplied capture, and handle it as if it's captured.
        /// </summary>
        /// <param name="captureToImport">ICapture to import</param>
        void ImportCapture(ICapture captureToImport);

        /// <summary>
        /// Use the supplied image, and ICapture a capture object for it
        /// </summary>
        /// <param name="imageToCapture">Image to create capture for</param>
        /// <returns>ICapture</returns>
        ICapture GetCapture(Image imageToCapture);
    }
}