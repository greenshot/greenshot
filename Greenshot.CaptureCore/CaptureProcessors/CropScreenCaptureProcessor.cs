//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Greenshot.CaptureCore.Forms;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Extensions;

namespace Greenshot.CaptureCore.CaptureProcessors
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
		public async Task ProcessCaptureAsync(ICaptureContext captureContext, CancellationToken cancellationToken = new CancellationToken())
		{
			await CropAsync(captureContext.Capture, null, cancellationToken);
		}
	}
}
