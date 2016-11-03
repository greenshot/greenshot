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
using Dapplo.Log.Facade;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Helpers;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Log.Loggers;

#if !DEBUG
using Dapplo.Log.LogFile;
using Greenshot.Configuration;
#endif

namespace Greenshot
{
	/// <summary>
	/// This takes care or starting Greenshot
	/// </summary>
	public class GreenshotStart
	{
		private static readonly LogSource Log = new LogSource();
		private const string MutexId = "F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08";
		public static string LogFileLocation = null;
		public const string ApplicationName = "Greenshot";
		protected static IniConfig iniConfig;
		protected static IGreenshotLanguage language;
		protected static ICoreConfiguration coreConfiguration;


		public static void Start(string[] args)
		{
			// Set the Thread name, is better than "1"
			Thread.CurrentThread.Name = ApplicationName;

			// Read arguments
			var arguments = new Arguments(args);
			// Don't continue if the Help was requested
			if (arguments.IsHelp)
			{
				return;
			}

#if DEBUG
			LogSettings.RegisterDefaultLogger<TraceLogger>(LogLevels.Verbose);
#else
			LogSettings.RegisterDefaultLogger<ForwardingLogger>(LogLevels.Verbose);
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

			}).Wait();

		}
	}
}
