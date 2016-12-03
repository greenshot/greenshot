using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Implementations
{
	/// <summary>
	/// This ICaptureProcessor will apply all specified filters to the capture
	/// </summary>
	public class EffectCaptureProcessor : ComposableBase<IEffect>, ICaptureProcessor
	{
		public string Name { get; } = nameof(EffectCaptureProcessor);

		public Task ProcessCaptureAsync(ICaptureContext captureContext, CancellationToken cancellationToken = new CancellationToken())
		{
			return Task.Run( () =>
			{
				bool disposeImage = false;
				var currentImage = captureContext.Capture.Image;

				using (var matrix = new Matrix())
				{
					foreach (var effect in Items)
					{
						if (cancellationToken.IsCancellationRequested)
						{
							break;
						}
						var tmpImage = effect.Apply(currentImage, matrix);
						if (tmpImage != null)
						{
							if (disposeImage)
							{
								currentImage.Dispose();
							}
							currentImage = tmpImage;
							// Make sure the "new" image is disposed
							disposeImage = true;
						}
					}
				}
				captureContext.Capture.Image = currentImage;
			}, cancellationToken);
		}
	}
}
