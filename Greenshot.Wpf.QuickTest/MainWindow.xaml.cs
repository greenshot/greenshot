using System.Windows;
using Greenshot.CaptureCore;
using Greenshot.Core.Enumerations;

namespace Greenshot.Wpf.QuickTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
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
