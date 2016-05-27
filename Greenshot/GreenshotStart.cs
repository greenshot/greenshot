/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Addons.Bootstrapper;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Config.Support;
using Dapplo.LogFacade;
using Dapplo.Windows.Native;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Forms;
using Greenshot.Helpers;
using Serilog;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Greenshot
{
	/// <summary>
	/// This takes care or starting Greenshot
	/// </summary>
	public class GreenshotStart
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(GreenshotStart));
		private const string MutexId = "F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08";
		public static string LogFileLocation = null;
		public const string ApplicationName = "Greenshot";
		private static readonly ApplicationBootstrapper GreenshotBootstrapper = new ApplicationBootstrapper(ApplicationName, MutexId);
		protected static IniConfig iniConfig;
		protected static IGreenshotLanguage language;
		protected static ICoreConfiguration coreConfiguration;


		public static ApplicationBootstrapper Bootstrapper => GreenshotBootstrapper;

		public static void Start(string[] args)
		{
			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = ApplicationName;
			// Handle exceptions
			TaskScheduler.UnobservedTaskException += (s, e) => ShowException(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowException(e.ExceptionObject as Exception);

			// Read arguments
			var arguments = new Arguments(args);
			// Don't continue if the Help was requested
			if (arguments.IsHelp)
			{
				return;
			}

			Serilog.Log.Logger = new LoggerConfiguration().ReadFrom.AppSettings()
#if DEBUG
				.MinimumLevel.Debug()
				.WriteTo.Trace(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {SourceContext} - {Message}{NewLine}{Exception}")
#endif
			.CreateLogger();
			// Map Dapplo.HttpExtensions logs to seri-log
			LogSettings.Logger = new DapploSeriLogLogger();
#if DEBUG
			LogSettings.Logger.Level = LogLevel.Verbose;
#endif

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

			// Initialize the string encryption, TODO: Move "credentials" to build server / yaml
			Dapplo.Config.Converters.StringEncryptionTypeConverter.RgbIv = "dlgjowejgogkklwj";
			Dapplo.Config.Converters.StringEncryptionTypeConverter.RgbKey = "lsjvkwhvwujkagfauguwcsjgu2wueuff";

			Task.Run(async () =>
			{
				iniConfig = new IniConfig(ApplicationName, ApplicationName, iniDirectory);
				// Register method to fix some values
				iniConfig.AfterLoad<ICoreConfiguration>(CoreConfigurationChecker.AfterLoad);
				coreConfiguration = await iniConfig.RegisterAndGetAsync<ICoreConfiguration>();
				var languageLoader = new LanguageLoader(ApplicationName, coreConfiguration.Language ?? "en-US");

				// Read configuration & languages
				languageLoader.CorrectMissingTranslations();
				language = await LanguageLoader.Current.RegisterAndGetAsync<IGreenshotLanguage>();
				await iniConfig.RegisterAndGetAsync<INetworkConfiguration>();
			}).Wait();

			// Log the startup
			Log.Information("Starting: " + EnvironmentInfo.EnvironmentToString(false));

			try
			{
				// Fix for Bug 2495900, Multi-user Environment
				// check whether there's an local instance running already
				if (!GreenshotBootstrapper.IsMutexLocked)
				{
					// Other instance is running, call a Greenshot client or exit etc

					if (arguments.FilesToOpen.Count > 0)
					{
						GreenshotClient.OpenFiles(arguments.FilesToOpen);
					}

					if (arguments.IsExit)
					{
						GreenshotClient.Exit();
					}

					ShowOtherInstances();
					GreenshotBootstrapper.Dispose();
					return;
				}

				if (!string.IsNullOrWhiteSpace(arguments.Language))
				{
					// Set language
					coreConfiguration.Language = arguments.Language;
				}

				MainForm.Start(arguments);
			}
			catch (Exception ex)
			{
				Log.Error("Exception in startup.", ex);
				ShowException(ex);
			}
		}

		/// <summary>
		/// Log and display any unhandled exceptions
		/// </summary>
		/// <param name="exception">Exception</param>
		public static void ShowException(Exception exception)
		{
			if (exception != null)
			{
				var exceptionText = EnvironmentInfo.BuildReport(exception);
				Log.Error(EnvironmentInfo.ExceptionToString(exception));
				new BugReportForm(exceptionText).ShowDialog();
			}
		}

		/// <summary>
		/// Helper method to show the other running instances
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
					Log.Debug(ex, "Problem retrieving process path of a running Greenshot instance");
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
				MessageBox.Show(dummyWindow, language.TranslationOrDefault(t => t.ErrorMultipleinstances) + "\r\n" + instanceInfo, language.TranslationOrDefault(t => t.Error), MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			finally
			{
				dummyWindow.Close();
			}
		}
	}
}
