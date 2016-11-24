using System.Windows;
using Dapplo.Config.Ini;
using Greenshot.CaptureCore;
using Greenshot.Core.Enumerations;

namespace Greenshot.Wpf.QuickTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly IniConfig _iniConfig = new IniConfig("GreenshotQuickTest", "greenshot-test");
		public MainWindow()
		{

			Loaded += async (sender, args) =>
			{
				await _iniConfig.RegisterAndGetAsync<ITestConfiguration>();

			};
			InitializeComponent();
		}

		private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			var captureWindow = new CaptureWindow
			{
				Mode = WindowCaptureMode.Auto,
				CaptureCursor = false,
				IeCapture = true
			};



			var capture = await captureWindow.CaptureActive();
			MessageBox.Show(capture.CaptureDetails.Title);
		}
	}
}
