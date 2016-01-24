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

using GreenshotPlugin.Core;

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;

namespace GreenshotPlugin.Extensions
{
	public static class UriExtensions
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(UriExtensions));

		/// <summary>
		/// Download the uri to Bitmap
		/// </summary>
		/// <param name="uri">Of an image</param>
		/// <param name="token"></param>
		/// <returns>Bitmap</returns>
		public static async Task<Image> DownloadImageAsync(this Uri uri, CancellationToken token = default(CancellationToken))
		{
			try
			{
				Exception initialException;
				string content;
				using (var response = await uri.GetAsync(token: token))
				{
					using (var stream = await response.GetAsAsync<MemoryStream>(token: token))
					{
						try
						{
							using (Image image = Image.FromStream(stream))
							{
								return ImageHelper.Clone(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
							}
						}
						catch (Exception ex)
						{
							// This might be okay, maybe it was just a search result
							initialException = ex;
						}
						stream.Seek(0, SeekOrigin.Begin);
						// If we arrive here, the image loading didn't work, try to see if the response has a http(s) URL to an image and just take this instead.
						using (var streamReader = new StreamReader(stream, Encoding.UTF8, true))
						{
							content = await streamReader.ReadLineAsync();
						}
					}
				}
				var imageUrlRegex = new Regex(@"(http|https)://.*(\.png|\.gif|\.jpg|\.tiff|\.jpeg|\.bmp)");
				var match = imageUrlRegex.Match(content);
				if (match.Success)
				{
					var contentUri = new Uri(match.Value);
					using (var response = await contentUri.GetAsync(token: token))
					{
						using (var stream = await response.GetAsAsync<MemoryStream>(token: token))
						{
							using (var image = Image.FromStream(stream))
							{
								return ImageHelper.Clone(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
							}
						}
					}
				}
				throw initialException;
			}
			catch (Exception e)
			{
				LOG.Error(e, "Problem downloading the image from: {Uri}", uri);
			}
			return null;
		}
	}
}