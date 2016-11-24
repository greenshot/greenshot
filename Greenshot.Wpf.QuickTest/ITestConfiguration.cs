using Dapplo.Config.Ini;
using Greenshot.Core.Configuration;

namespace Greenshot.Wpf.QuickTest
{
	[IniSection("Test")]
	public interface ITestConfiguration : IIniSection, IIECaptureConfiguration, ICaptureConfiguration
	{
	}
}
