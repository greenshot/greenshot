using System.Threading;
using System.Threading.Tasks;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Implementations
{
	/// <summary>
	/// The composable capture processor allows to combine multiple processors
	/// </summary>
	public class ComposableCaptureProcessor : ComposableBase<ICaptureProcessor>, ICaptureProcessor
	{
		public string Name { get; set; }

		/// <inheritdoc />
		public async Task ProcessCaptureAsync(ICaptureContext captureContext, CancellationToken cancellationToken = new CancellationToken())
		{
			foreach (var captureProcessor in Items)
			{
				await captureProcessor.ProcessCaptureAsync(captureContext, cancellationToken);
			}
		}
	}
}
