using Dapplo.Config.Ini;
using Greenshot.Core.Configuration;

namespace Greenshot.Wpf.QuickTest
{
	/// <summary>
	/// This actually only makes sure tsome of the sub-ini configuration modules can be found
	/// </summary>
	[IniSection("Test")]
	public interface ITestConfiguration : IIniSection, IIECaptureConfiguration, ICaptureConfiguration, ICropConfiguration, IUiConfiguration
	{
	}
}
