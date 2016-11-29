using System;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using Greenshot.CaptureCore.Forms;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Legacy.Extensions;

namespace Greenshot.CaptureCore
{
	/// <summary>
	/// Crop the capture by showing a full screen overlay where the user can select the part he actually would like
	/// </summary>
	public class CropScreenCaptureProcessor : ICaptureProcessor
	{
		/// <summary>
		/// Show and make it possible to crop
		/// </summary>
		/// <param name="capture">ICapture to show and apply crop selection</param>
		/// <param name="getWindowsTask"></param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public async Task CropAsync(ICapture capture, Task<IList<WindowDetails>> getWindowsTask = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cropConfiguration = IniConfig.Current.GetSubSection<ICropConfiguration>();

			using (var cropFrom = new CropForm(capture, getWindowsTask, cropConfiguration))
			{
				cropFrom.Show();

				await cropFrom.WaitForClosedAsync(cancellationToken);

				// Show the screen, and if the result is "OK" crop the capture by applying it.
				if (cropFrom.DialogResult == DialogResult.OK)
				{
					capture.ApplyCrop(cropFrom.CaptureRectangle);
				}
				else
				{
					throw new OperationCanceledException("User cancelled crop");
				}
			}
		}

		/// <inheritdoc />
		public string Name { get; } = nameof(CropScreenCaptureProcessor);

		/// <inheritdoc />
		public async Task ProcessCaptureAsync(ICaptureFlow captureFlow, CancellationToken cancellationToken = new CancellationToken())
		{
			await CropAsync(captureFlow.Capture, null, cancellationToken);
		}
	}
}
