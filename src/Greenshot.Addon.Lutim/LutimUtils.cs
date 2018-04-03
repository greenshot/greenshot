/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using Dapplo.Ini;
using Dapplo.Log;
using Greenshot.Addon.Lutim.Entities;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

namespace Greenshot.Addon.Lutim
{
	/// <summary>
	/// A collection of Lutim helper methods
	/// </summary>
	public static class LutimUtils
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ILutimConfiguration Config = IniConfig.Current.Get<ILutimConfiguration>();

		/// <summary>
		/// Check if we need to load the history
		/// </summary>
		/// <returns></returns>
		public static bool IsHistoryLoadingNeeded()
		{
			Log.Info().WriteLine("Checking if lutim cache loading needed, configuration has {0} lutim hashes, loaded are {1} hashes.", Config.LutimUploadHistory.Count, Config.RuntimeLutimHistory.Count);
			return Config.RuntimeLutimHistory.Count != Config.LutimUploadHistory.Count;
		}

		/// <summary>
		/// Load the complete history of the lutim uploads, with the corresponding information
		/// </summary>
		public static void LoadHistory()
		{
			if (!IsHistoryLoadingNeeded())
			{
				return;
			}

			// Load the ImUr history
			foreach (string key in Config.LutimUploadHistory.Keys.ToList())
			{
				if (Config.RuntimeLutimHistory.ContainsKey(key))
				{
					// Already loaded
					continue;
				}

				try
				{
					var value = Config.LutimUploadHistory[key];
                    // TODO: Read from something
					//LutimInfo lutimInfo = LutimInfo.FromIniString(key, value);
					// Config.RuntimeLutimHistory[key] = lutimInfo;
				}
				catch (ArgumentException)
				{
					Log.Info().WriteLine("Bad format of lutim history item for short {0}", key);
					Config.LutimUploadHistory.Remove(key);
					Config.RuntimeLutimHistory.Remove(key);
				}
				catch (Exception e)
				{
					Log.Error().WriteLine(e, "Problem loading lutim history for short " + key);
				}
			}
		}

		/// <summary>
		/// Do the actual upload to Lutim
		/// For more details on the available parameters, see: http://api.lutim.com/resources_anon
		/// </summary>
		/// <param name="surfaceToUpload">ISurface to upload</param>
		/// <param name="outputSettings">OutputSettings for the image file format</param>
		/// <param name="filename">Filename</param>
		/// <returns>LutimInfo with details</returns>
		public static async Task<LutimInfo> UploadToLutim(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string filename)
		{
		    var baseUrl = new Uri(Config.LutimUrl);

            // TODO: Upload
		    var result = await baseUrl.PostAsync<AddResult>("");
		    return result.LutimInfo;
		}

		/// <summary>
		/// Delete an lutim image, this is done by specifying the delete hash
		/// </summary>
		/// <param name="lutimInfo"></param>
		public static async Task DeleteLutimImage(AddResult lutimInfo)
		{
			Log.Info().WriteLine("Deleting Lutim image for {0}", lutimInfo.LutimInfo.Short);

			var lutimBaseUri = new Uri(Config.LutimUrl);
			var deleteUri = lutimBaseUri.AppendSegments("d", lutimInfo.LutimInfo.RealShort, lutimInfo.LutimInfo.Token).ExtendQuery("format", "json");

			var httpResponse = await deleteUri.GetAsAsync<HttpResponse<string, string>>();
                
			if (httpResponse.StatusCode != HttpStatusCode.BadRequest)
			{
			    throw new Exception(httpResponse.ErrorResponse);
			}
			
			Log.Info().WriteLine("Delete result: {0}", httpResponse.Response);
			// Make sure we remove it from the history, if no error occured
			Config.RuntimeLutimHistory.Remove(lutimInfo.LutimInfo.Short);
			Config.LutimUploadHistory.Remove(lutimInfo.LutimInfo.Short);
		}

		/// <summary>
		/// Helper for logging
		/// </summary>
		/// <param name="nameValues"></param>
		/// <param name="key"></param>
		private static void LogHeader(IDictionary<string, string> nameValues, string key)
		{
			if (nameValues.ContainsKey(key))
			{
				Log.Info().WriteLine("{0}={1}", key, nameValues[key]);
			}
		}

	    public static Task<LutimInfo> RetrieveLutimInfoAsync(string hash, string s, CancellationToken cancellationToken)
	    {
	        throw new NotImplementedException();
	    }

	    public static Task RetrieveLutimThumbnailAsync(LutimInfo lutimInfo, CancellationToken cancellationToken)
	    {
	        throw new NotImplementedException();
	    }
	}
}
