using System;
using System.Windows;
using Dapplo.CaliburnMicro;
using Dapplo.Config.Language;
using Dapplo.Log.Facade;
using Dapplo.Utils.Resolving;
using Greenshot.Addon.Configuration;
using Dapplo.Config.Support;
using Dapplo.Log.LogFile;

namespace Greenshot
{
	/// <summary>
	/// This class contains the code to start Greenshot
	/// </summary>
	public static class Start
	{
		private const string MutexId = "F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08";

		/// <summary>
		///     Start the application
		/// </summary>
		[STAThread]
		public static void Main()
		{
#if DEBUG
			LogSettings.RegisterDefaultLogger<TraceLogger>(LogLevels.Verbose);
#else
			LogSettings.RegisterDefaultLogger<ForwardingLogger>(LogLevels.Verbose);
#endif

			// Initialize the string encryption, TODO: Move "credentials" to build server / yaml
			Dapplo.Config.Converters.StringEncryptionTypeConverter.RgbIv = "dlgjowejgogkklwj";
			Dapplo.Config.Converters.StringEncryptionTypeConverter.RgbKey = "lsjvkwhvwujkagfauguwcsjgu2wueuff";

			var application = new Dapplication("Greenshot", MutexId)
			{
				ShutdownMode = ShutdownMode.OnExplicitShutdown,
				// Don't allow the application to run multiple times
				OnAlreadyRunning = () =>
				{
					var greenshotLanguage = LanguageLoader.Current.Get<IGreenshotLanguage>();
					MessageBox.Show(greenshotLanguage.TranslationOrDefault(x => x.ErrorMultipleinstances), "Greenshot", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					Application.Current.Shutdown();
				}
			};

			// Set .gsp as the Greenshot plugin extension
			AssemblyResolver.Extensions.Add("gsp");

			// Set the plugins directory
			application.Bootstrapper.AddScanDirectory("Plugins");

			// Load the assemblies Dapplo.CaliburnMicro, Dapplo.CaliburnMicro.NotifyIconWpf, Dapplo.CaliburnMicro.Metro
			application.Bootstrapper.FindAndLoadAssemblies("Dapplo.Caliburn*");

			application.Bootstrapper.FindAndLoadAssemblies("Greenshot.Addon*");

			application.Run();
		}

	}
}
