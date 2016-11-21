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

#region Usings

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using Dapplo.CaliburnMicro;
using Dapplo.Config.Converters;
using Dapplo.Config.Language;
using Dapplo.Config.Support;
using Dapplo.Log;
using Dapplo.Log.Loggers;
using Dapplo.Utils.Resolving;
using Dapplo.Windows.Native;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Forms;
using Greenshot.Helpers;
using Greenshot.Legacy;

#if !DEBUG
using Dapplo.Log.LogFile;
#endif

#endregion

namespace Greenshot
{
	/// <summary>
	///     This class contains the code to start Greenshot
	/// </summary>
	public static class Start
	{
		private const string MutexId = "F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08";
		private const string ApplicationName = "Greenshot";

		public static Dapplication Dapplication { get; set; }

		/// <summary>
		///     Start the application
		/// </summary>
		[STAThread]
		public static void Main(string[] args)
		{
#if DEBUG
			LogSettings.RegisterDefaultLogger<DebugLogger>(LogLevels.Verbose);
#else
			LogSettings.RegisterDefaultLogger<ForwardingLogger>(LogLevels.Verbose);
#endif
			// Read arguments
			var arguments = new Arguments(args);
			// Don't continue if the Help was requested
			if (arguments.IsHelp)
			{
				return;
			}

			// Setting the INI-directory
			string iniDirectory = null;
			// Specified the ini directory directly
			if (!string.IsNullOrWhiteSpace(arguments.IniDirectory))
			{
				iniDirectory = arguments.IniDirectory;
			}
			// Portable mode
			if (PortableHelper.IsPortable)
			{
				iniDirectory = PortableHelper.PortableIniFileLocation;
			}
			// TODO: Use the correct directory

			// Initialize the string encryption, TODO: Move "credentials" to build server / yaml
			StringEncryptionTypeConverter.RgbIv = "dlgjowejgogkklwj";
			StringEncryptionTypeConverter.RgbKey = "lsjvkwhvwujkagfauguwcsjgu2wueuff";

			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = ApplicationName;

			Dapplication = new Dapplication(ApplicationName, MutexId)
			{
				ShutdownMode = ShutdownMode.OnExplicitShutdown,
				// Don't allow the application to run multiple times
				OnAlreadyRunning = () =>
				{
					// Other instance is running, call a Greenshot client or exit etc
					if (arguments.FilesToOpen.Count > 0)
					{
						GreenshotClient.OpenFiles(arguments.FilesToOpen);
						return;
					}
					if (arguments.IsExit)
					{
						GreenshotClient.Exit();
						return;
					}

					ShowOtherInstances();
				},
				// Handle AppDomain exception
				OnUnhandledAppDomainException = (exception, isTerminating) => { ShowException(exception); },
				// Handle the unhandled dispatcher exceptions
				OnUnhandledDispatcherException = exception => { ShowException(exception); },
				// Handle the unhandled exceptions
				OnUnhandledException = exception => { ShowException(exception); },
				// Handle the unhandled task exceptions
				OnUnhandledTaskException = exception => { ShowException(exception); }
			};

			// Set .gsp as the Greenshot plugin extension
			AssemblyResolver.Extensions.Add("gsp");

			// Set the plugins directory
			Dapplication.Bootstrapper.AddScanDirectory("Plugins");

			// Load the assemblies Dapplo.CaliburnMicro, Dapplo.CaliburnMicro.NotifyIconWpf, Dapplo.CaliburnMicro.Metro
			Dapplication.Bootstrapper.FindAndLoadAssemblies("Dapplo.Caliburn*");
			Dapplication.Bootstrapper.FindAndLoadAssemblies("Greenshot.CaptureCore");
			Dapplication.Bootstrapper.FindAndLoadAssemblies("Greenshot.Core");
			Dapplication.Bootstrapper.FindAndLoadAssemblies("Greenshot.Addon*");


			if (!string.IsNullOrWhiteSpace(arguments.Language))
			{
				// TODO: Set language
				//coreConfiguration.Language = arguments.Language;
			}

			new LogSource().Info().WriteLine("Starting: {0}", EnvironmentInfo.EnvironmentToString(false));

			Dapplication.Run();
		}


		/// <summary>
		///     Log and display any unhandled exceptions
		/// </summary>
		/// <param name="exception">Exception</param>
		public static void ShowException(Exception exception)
		{
			if (exception != null)
			{
				var exceptionText = EnvironmentInfo.BuildReport(exception);
				new LogSource().Error().WriteLine(EnvironmentInfo.ExceptionToString(exception));
				new BugReportForm(exceptionText).ShowDialog();
			}
		}


		/// <summary>
		///     Helper method to show the other running instances
		/// </summary>
		private static void ShowOtherInstances()
		{
			var instanceInfo = new StringBuilder();
			var matchedThisProcess = false;
			var index = 1;
			int currentProcessId;
			using (var currentProcess = Process.GetCurrentProcess())
			{
				currentProcessId = currentProcess.Id;
			}
			foreach (var greenshotProcess in Process.GetProcessesByName(ApplicationName))
			{
				try
				{
					instanceInfo.Append(index++ + ": ").AppendLine(Kernel32.GetProcessPath(greenshotProcess.Id));
					if (currentProcessId == greenshotProcess.Id)
					{
						matchedThisProcess = true;
					}
				}
				catch (Exception ex)
				{
					new LogSource().Debug().WriteLine(ex, "Problem retrieving process path of a running Greenshot instance");
				}
				greenshotProcess.Dispose();
			}
			if (!matchedThisProcess)
			{
				using (var currentProcess = Process.GetCurrentProcess())
				{
					instanceInfo.Append(index + ": ").AppendLine(Kernel32.GetProcessPath(currentProcess.Id));
				}
			}

			// A dirty fix to make sure the messagebox is visible as a Greenshot window on the taskbar
			var dummyWindow = new Window
			{
				ShowInTaskbar = true,
				WindowStartupLocation = WindowStartupLocation.Manual,
				Left = int.MinValue,
				Top = int.MinValue,
				SizeToContent = SizeToContent.Manual,
				ResizeMode = ResizeMode.NoResize,
				Width = 0,
				Height = 0,
				Icon = GreenshotResources.GetGreenshotImage().ToBitmapSource()
			};
			try
			{
				dummyWindow.Show();
				// Make sure the language files are loaded, so we can show the error message "Greenshot is already running" in the right language.

				var greenshotLanguage = LanguageLoader.Current.Get<IGreenshotLanguage>();
				MessageBox.Show(greenshotLanguage.TranslationOrDefault(x => x.ErrorMultipleinstances), "Greenshot", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				Application.Current.Shutdown();
			}
			finally
			{
				dummyWindow.Close();
			}
		}
	}
}