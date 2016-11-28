using System.Threading.Tasks;
using Dapplo.Config.Ini;
using Greenshot.CaptureCore.Forms;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Interfaces;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Greenshot.CaptureCore
{
	public class CropCapture
	{
		public void Crop(ICapture capture, Task<IList<WindowDetails>> getWindowsTask = null)
		{
			var cropConfiguration = IniConfig.Current.GetSubSection<ICropConfiguration>();

			using (var cropFrom = new CropForm(capture, getWindowsTask, cropConfiguration))
			{
				// Show the screen, and if the result is "OK" crop the capture by applying it.
				if (cropFrom.ShowDialog() == DialogResult.OK)
				{
					capture.ApplyCrop(cropFrom.CaptureRectangle);
				}
			}
		}
	}
}
