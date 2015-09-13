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
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotPlugin.Extensions
{
	public static class HttpResponseMessageExtensions
	{
		/// <summary>
		/// ReadAsStringAsync
		/// </summary>
		/// <param name="response"></param>
		/// <param name="throwErrorOnNonSuccess"></param>
		/// <returns></returns>
		public static async Task<string> GetAsStringAsync(this HttpResponseMessage response, bool throwErrorOnNonSuccess = true)
		{
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			}
			if (throwErrorOnNonSuccess)
			{
				response.EnsureSuccessStatusCode();
			}
			return null;
		}

		/// <summary>
		/// GetAsJsonAsync
		/// </summary>
		/// <param name="response"></param>
		/// <param name="throwErrorOnNonSuccess"></param>
		/// <returns>dynamic (DyanmicJson)</returns>
		public static async Task<dynamic> GetAsJsonAsync(this HttpResponseMessage response, bool throwErrorOnNonSuccess = true)
		{
			if (response.IsSuccessStatusCode)
			{
				var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				return DynamicJson.Parse(jsonString);
			}
			if (throwErrorOnNonSuccess)
			{
				response.EnsureSuccessStatusCode();
			}
			return null;
		}

		/// <summary>
		/// Get the content as a MemoryStream
		/// </summary>
		/// <param name="response"></param>
		/// <param name="throwErrorOnNonSuccess"></param>
		/// <returns>MemoryStream</returns>
		public static async Task<MemoryStream> GetAsMemoryStreamAsync(this HttpResponseMessage response, bool throwErrorOnNonSuccess = true, CancellationToken token = default(CancellationToken))
		{
			if (response.IsSuccessStatusCode)
			{
				using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
				{
					var memoryStream = new MemoryStream();
					await contentStream.CopyToAsync(memoryStream, 4096, token).ConfigureAwait(false);
					memoryStream.Position = 0;
					return memoryStream;
				}
			}
			try
			{
				response.EnsureSuccessStatusCode();
			}
			catch (Exception)
			{
				if (throwErrorOnNonSuccess)
				{
					throw;
				}
			}
			return null;
		}

		/// <summary>
		/// Simplified error handling, this makes sure the uri & response are logged
		/// </summary>
		/// <param name="responseMessage"></param>
		public static async Task HandleErrorAsync(this HttpResponseMessage responseMessage, CancellationToken token = default(CancellationToken))
		{
			Exception throwException = null;
			string errorContent = null;
			Uri requestUri = null;
			try
			{
				if (!responseMessage.IsSuccessStatusCode)
				{
					requestUri = responseMessage.RequestMessage.RequestUri;
					try
					{
						// try reading the content, so this is not lost
						errorContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
					}
					catch
					{
						// Ignore
					}
					responseMessage.EnsureSuccessStatusCode();
				}
			}
			catch (Exception ex)
			{
				throwException = ex;
				throwException.Data.Add("uri", requestUri);
			}
			if (throwException != null)
			{
				if (errorContent != null)
				{
					throwException.Data.Add("response", errorContent);
				}
				throw throwException;
			}
			return;
		}
	}
}
