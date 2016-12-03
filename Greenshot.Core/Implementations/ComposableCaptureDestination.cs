using System.Threading;
using System.Threading.Tasks;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Implementations
{
	/// <summary>
	/// The composable capture destination allows to combine multiple destination
	/// </summary>
	public class ComposableCaptureDestination : ComposableBase<ICaptureDestination>, ICaptureDestination
	{
		public string Name { get; set; }

		public async Task ExportCaptureAsync(ICaptureContext captureContext, CancellationToken cancellationToken = new CancellationToken())
		{
			foreach (var captureDestination in Items)
			{
				await captureDestination.ExportCaptureAsync(captureContext, cancellationToken);
			}
		}
	}
}
