/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using GreenshotPlugin.Core;
using System.Diagnostics;
using System.Net;
using log4net;
using System;
using System.Threading.Tasks;

namespace Greenshot.Help
{
	/// <summary>
	/// Description of HelpFileLoader.
	/// </summary>
	public static class HelpFileLoader
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(HelpFileLoader));
		
		private static readonly Uri EXT_HELP_URL = new Uri(@"http://getgreenshot.org/help/");
		
		/// <summary>
		/// Start a process with the help, this can either be an online link, or a local file
		/// </summary>
		/// <returns>Task</returns>
		public static async Task LoadHelpAsync() {
			var uri = await FindOnlineHelpUrl(Language.CurrentLanguage);
			if (uri == null) {
				uri = Language.HelpFileUri;
			}
			Process.Start(uri.AbsoluteUri);			
		}
		
		/// <summary>
		/// </summary>
		/// <param name="currentIETF">Language.CurrentLanguage</param>
		/// <returns>URL of help file in selected ietf, or (if not present) default ietf, or null (if not present, too. probably indicating that there is no internet connection)</returns>
		private static async Task<Uri> FindOnlineHelpUrl(string currentIETF) {
			if (!currentIETF.Equals("en-US")) {
				var localizedContentUri = EXT_HELP_URL.AppendSegments(currentIETF.ToLower());

				// Check if the online localized content is available.
				try {
					// Although a "HeadAsync" should be enough, this give an OK when the SF-Database has problems.
					// TODO: change to HeadAsync after we moved to GitHub
					await localizedContentUri.GetAsStringAsync();
					return localizedContentUri;
				} catch {
					// NO internet or wrong URI
				}

				LOG.InfoFormat("Localized online help not found at {0}, will try {1} as fallback", localizedContentUri, EXT_HELP_URL);
			}

			// Check if the online content (en-US) is available.
			try {
				// Although a "HeadAsync" should be enough, this give an OK when the SF-Database has problems.
				// TODO: change to HeadAsync after we moved to GitHub
				await EXT_HELP_URL.GetAsStringAsync();
				return EXT_HELP_URL;
			} catch {
				// NO internet or wrong URI
			}
			return null;
		}
	}
}
