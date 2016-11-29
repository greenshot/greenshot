using System.Windows;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Log;
using Dapplo.Log.Loggers;
using Greenshot.CaptureCore;
using Greenshot.Core.Enumerations;
using Greenshot.Core.Extensions;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;

namespace Greenshot.Wpf.QuickTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		// Make sure an Ini-Config is created
		private readonly IniConfig _iniConfig = new IniConfig("GreenshotQuickTest", "greenshot-test");
		// Make sure the language configuration can be loaded
		private readonly LanguageLoader _languageLoader = new LanguageLoader("GreenshotQuickTest");

		public MainWindow()
		{
			LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);
			Loaded += async (sender, args) =>
			{
				await _iniConfig.RegisterAndGetAsync<ITestConfiguration>();
				await _languageLoader.RegisterAndGetAsync<ITestTranslations>();


			};
			InitializeComponent();
		}

		/// <summary>
		/// Helper method to show the capture
		/// </summary>
		/// <param name="capture">ICapture</param>
		private void ShowCapture(ICapture capture)
		{
			// Show the (cropped) capture, by getting the image and placing it into the UI
			using (var image = capture.GetImageForExport())
			{
				CapturedImage.Source = image.ToBitmapSource();
			}
		}
		private async void WindowButton_OnClick(object sender, RoutedEventArgs e)
		{
			WindowButton.IsEnabled = false;
			var flow = new SimpleCaptureFlow
			{
				CaptureSource = new WindowCaptureSource
				{
					Mode = WindowCaptureMode.Auto,
					CaptureCursor = false,
					IeCapture = true
				}
			};
			await flow.ExecuteAsync();

			ShowCapture(flow.Capture);
			WindowButton.IsEnabled = true;
		}

		private async void ScreenButton_OnClick(object sender, RoutedEventArgs e)
		{
			ScreenButton.IsEnabled = false;

			var flow = new SimpleCaptureFlow
			{
				// Get a capture of the "active" screen, that is the one with the mouse cursor.
				// The capture contains all the information, like the bitmap/mouse cursor/location of the mouse and some meta data.
				CaptureSource = new ScreenCaptureSource
				{
					Mode = ScreenCaptureMode.Auto,
					CaptureCursor = true,
				},
				// Have the user crop the screen
				CaptureProcessor = new CropScreenCaptureProcessor()
			};
			await flow.ExecuteAsync();

			// Show the (cropped) capture, by getting the image and placing it into the UI
			ShowCapture(flow.Capture);
			ScreenButton.IsEnabled = true;

		}
	}
}
