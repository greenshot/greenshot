/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.ContentConverter;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.HttpExtensions.Support;
using Dapplo.Log.Facade;
using System.Net.Http;
using System.Net.Http.Headers;

namespace GreenshotJiraPlugin
{
	/// <summary>
	/// This adds SVG image support for the Jira Plugin
	/// </summary>
	public class SvgBitmapHttpContentConverter : IHttpContentConverter
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IList<string> SupportedContentTypes = new List<string>();

		static SvgBitmapHttpContentConverter()
		{
			SupportedContentTypes.Add(MediaTypes.Svg.EnumValueOf());
		}

		/// <inheritdoc />
		public int Order => 0;

		public int Width { get; set; }

		public int Height { get; set; }

		/// <summary>
		///     This checks if the HttpContent can be converted to a Bitmap and is assignable to the specified Type
		/// </summary>
		/// <param name="typeToConvertTo">This should be something we can assign Bitmap to</param>
		/// <param name="httpContent">HttpContent to process</param>
		/// <returns>true if it can convert</returns>
		public bool CanConvertFromHttpContent(Type typeToConvertTo, HttpContent httpContent)
		{
			if (typeToConvertTo == typeof(object) || !typeToConvertTo.IsAssignableFrom(typeof(Bitmap)))
			{
				return false;
			}
			var httpBehaviour = HttpBehaviour.Current;
			return !httpBehaviour.ValidateResponseContentType || SupportedContentTypes.Contains(httpContent.GetContentType());
		}

		/// <inheritdoc />
		public async Task<object> ConvertFromHttpContentAsync(Type resultType, HttpContent httpContent, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!CanConvertFromHttpContent(resultType, httpContent))
			{
				var exMessage = "CanConvertFromHttpContent resulted in false, ConvertFromHttpContentAsync is not supposed to be called.";
				Log.Error().WriteLine(exMessage);
				throw new NotSupportedException(exMessage);
			}
			using (var memoryStream = (MemoryStream) await StreamHttpContentConverter.Instance.ConvertFromHttpContentAsync(typeof(MemoryStream), httpContent, cancellationToken).ConfigureAwait(false))
			{
				Log.Debug().WriteLine("Creating a Bitmap from the SVG.");
				var svgImage = new SvgImage(memoryStream)
				{
					Height = Height,
					Width = Width
				};
				return svgImage.Image;
			}
		}

		/// <inheritdoc />
		public bool CanConvertToHttpContent(Type typeToConvert, object content)
		{
			return false;
		}

		/// <inheritdoc />
		public HttpContent ConvertToHttpContent(Type typeToConvert, object content)
		{
			return null;
		}

		/// <inheritdoc />
		public void AddAcceptHeadersForType(Type resultType, HttpRequestMessage httpRequestMessage)
		{
			if (resultType == null)
			{
				throw new ArgumentNullException(nameof(resultType));
			}
			if (httpRequestMessage == null)
			{
				throw new ArgumentNullException(nameof(httpRequestMessage));
			}
			if (resultType == typeof(object) || !resultType.IsAssignableFrom(typeof(Bitmap)))
			{
				return;
			}
			httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypes.Svg.EnumValueOf()));
			Log.Debug().WriteLine("Modified the header(s) of the HttpRequestMessage: Accept: {0}", httpRequestMessage.Headers.Accept);
		}
	}
}
