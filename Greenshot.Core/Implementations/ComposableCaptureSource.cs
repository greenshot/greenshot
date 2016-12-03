using System.Threading;
using System.Threading.Tasks;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Implementations
{
	/// <summary>
	/// The composable capture source allows to combine multiple sources
	/// </summary>
	public class ComposableCaptureSource : ComposableBase<ICaptureSource>, ICaptureSource
	{
		public string Name { get; set; }

		/// <inheritdoc />
		public async Task TakeCaptureAsync(ICaptureContext captureContext, CancellationToken cancellationToken = new CancellationToken())
		{
			foreach (var captureSource in Items)
			{
				await captureSource.TakeCaptureAsync(captureContext, cancellationToken);
			}
		}
	}
}
