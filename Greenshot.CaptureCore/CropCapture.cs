using Dapplo.Config.Ini;
using Greenshot.CaptureCore.Forms;
using Greenshot.Core.Configuration;
using Greenshot.Core.Interfaces;

namespace Greenshot.CaptureCore
{
	public class CropCapture
	{
		public void Crop(ICapture capture)
		{
			var cropConfiguration = IniConfig.Current.GetSubSection<ICropConfiguration>();

			var cropFrom = new CropForm(capture, null, cropConfiguration);
			cropFrom.ShowDialog();

		}
	}
}
