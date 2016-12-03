namespace Greenshot.Core.Interfaces
{
	/// <summary>
	/// The ICaptureContext is what is containing the capture, and is passed to ICaptureSource, ICaptureProcessor and ICaptureDestination
	/// </summary>
	public interface ICaptureContext
	{
		/// <summary>
		/// Contains the capture of this flow
		/// </summary>
		ICapture Capture { get; set; }
	}
}
