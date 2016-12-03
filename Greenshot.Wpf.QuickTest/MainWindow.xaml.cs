using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Log;
using Dapplo.Log.Loggers;
using Greenshot.Addon.Configuration;
using Greenshot.Core.Enumerations;
using Greenshot.Core.Extensions;
using Greenshot.Core.Implementations;
using Greenshot.Core.Interfaces;
using Greenshot.Addon.Editor;
using Greenshot.Addon.Editor.Configuration;
using Greenshot.CaptureCore.CaptureProcessors;
using Greenshot.CaptureCore.CaptureSources;
using Greenshot.Core.Gfx;

namespace Greenshot.Wpf.QuickTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, ICaptureDestination
	{
		// Make sure an Ini-Config is created
		private readonly IniConfig _iniConfig = new IniConfig("GreenshotQuickTest", "greenshot-test");
		// Make sure the language configuration can be loaded
		private readonly LanguageLoader _languageLoader = new LanguageLoader("GreenshotQuickTest");

		public MainWindow()
		{
			LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);

			// Initialize some async stuff
			Loaded += async (sender, args) =>
			{
				// Manually initialize the configuration for now
				await _iniConfig.RegisterAndGetAsync<ITestConfiguration>();
				await _iniConfig.RegisterAndGetAsync<IEditorConfiguration>();
				await _languageLoader.RegisterAndGetAsync<ITestTranslations>();
				await _languageLoader.RegisterAndGetAsync<IGreenshotLanguage>();
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
			using (var image = capture.Flatten())
			{
				CapturedImage.Source = image.ToBitmapSource();
			}
		}

		/// <summary>
		/// Capture a window, and show the result
		/// </summary>
		private async void WindowButton_OnClick(object sender, RoutedEventArgs e)
		{
			WindowButton.IsEnabled = false;

			var effectCaptureProcessor = new EffectCaptureProcessor();
			effectCaptureProcessor.Add(new DropShadowEffect());
			var flow = new SimpleCaptureFlow
			{
				CaptureSource = new WindowCaptureSource
				{
					Mode = WindowCaptureMode.Auto,
					CaptureCursor = false,
					IeCapture = true
				},
				// Apply drop shadow
				CaptureProcessor = effectCaptureProcessor,
				// Show the (cropped) capture, by getting the image and placing it into the UI
				CaptureDestination = this
			};
			await flow.ExecuteAsync();

			WindowButton.IsEnabled = true;
		}

		/// <summary>
		/// Capture the screen, and show the cropped result
		/// </summary>
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
				CaptureProcessor = new CropScreenCaptureProcessor(),
				// Show the (cropped) capture, by getting the image and placing it into the UI
				CaptureDestination = this
			};
			await flow.ExecuteAsync();
			ScreenButton.IsEnabled = true;

		}

		/// <summary>
		/// Capture the screen, and show it in the editor
		/// </summary>
		private async void ScreenEditButton_OnClick(object sender, RoutedEventArgs e)
		{
			ScreenEditButton.IsEnabled = false;

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
				CaptureProcessor = new CropScreenCaptureProcessor(),
				// Show the capture in the editor (currently the editor is a destination, which actually when you think about it doesn't fit.. it's a processor)
				CaptureDestination = new EditorCaptureDestination
				{
					EditorConfiguration = IniConfig.Current.Get<IEditorConfiguration>()
				}
			};
			await flow.ExecuteAsync();

			// Now take the capture manually
			ShowCapture(flow.Capture);

			ScreenEditButton.IsEnabled = true;
		}

		/// <summary>
		/// Example implementation of the CaptureDestination
		/// </summary>
		/// <param name="captureContext">ICaptureContext which needs to be send to it's destinationt</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Task</returns>
		public Task ExportCaptureAsync(ICaptureContext captureContext, CancellationToken cancellationToken = new CancellationToken())
		{
			ShowCapture(captureContext.Capture);
			return Task.FromResult(true);
		}
	}
}
