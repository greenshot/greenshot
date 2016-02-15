/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Language;
using Dapplo.Config.Support;
using Dapplo.HttpExtensions;

namespace Greenshot.Addon.Core
{
	/// <summary>
	/// Description of HelpFileLoader.
	/// </summary>
	public static class HelpFileLoader
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(HelpFileLoader));

		private static readonly Uri ExtHelpUrl = new Uri(@"http://getgreenshot.org/help/");
		private static readonly string HelpfilePattern = "help-{0}.html";

		/// <summary>
		/// Start a process with the help, this can either be an online link, or a local file
		/// </summary>
		/// <returns>Task</returns>
		public static async Task LoadHelpAsync(CancellationToken token = default(CancellationToken))
		{
			var uri = await FindOnlineHelpUrlAsync(LanguageLoader.Current.CurrentLanguage, token);
			if (uri == null)
			{
				var currentLanguage = LanguageLoader.Current.CurrentLanguage;
				var startupDirectory = FileLocations.StartupDirectory;
				var directories = new List<string>();
				// Portable
				directories.Add(Path.Combine(startupDirectory, "App", "Greenshot", "Languages"));

				// Application data path
				directories.Add(Path.Combine(FileLocations.RoamingAppDataDirectory("Greenshot"), "Greenshot", "Languages"));

				// Startup path
				directories.Add(Path.Combine(Path.Combine(startupDirectory, @"Languages")));
				var helpFilePath = FileLocations.Scan(directories, string.Format(HelpfilePattern, currentLanguage)).FirstOrDefault();
				if (helpFilePath != null)
				{
					uri = new Uri(string.Format(@"file://{0}", helpFilePath));
				}
			}
			if (uri != null)
			{
				Process.Start(uri.AbsoluteUri);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="currentIetf">Language.CurrentLanguage</param>
		/// <returns>URL of help file in selected ietf, or (if not present) default ietf, or null (if not present, too. probably indicating that there is no internet connection)</returns>
		private static async Task<Uri> FindOnlineHelpUrlAsync(string currentIetf, CancellationToken token = default(CancellationToken))
		{
			if (!currentIetf.Equals("en-US"))
			{
				var localizedContentUri = ExtHelpUrl.AppendSegments(currentIetf.ToLower());

				// Check if the online localized content is available.
				try
				{
					// Although a "HeadAsync" should be enough, this gives an OK when the SF-Database has problems.
					await localizedContentUri.HeadAsync(null, token);
					return localizedContentUri;
				}
				catch
				{
					// NO internet or wrong URI
				}

				Log.Information("Localized online help not found at {0}, will try {1} as fallback", localizedContentUri, ExtHelpUrl);
			}

			// Check if the online content (en-US) is available.
			try
			{
				// Although a "HeadAsync" should be enough, this give an OK when the SF-Database has problems.
				await ExtHelpUrl.HeadAsync(null, token);
				return ExtHelpUrl;
			}
			catch
			{
				// NO internet or wrong URI
			}
			return null;
		}
	}
}