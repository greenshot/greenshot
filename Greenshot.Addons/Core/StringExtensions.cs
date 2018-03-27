#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Dapplo.Log;

#endregion

namespace Greenshot.Addons.Core
{
	public static class StringExtensions
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     Format a string with the specified object
		/// </summary>
		/// <param name="format">String with formatting, like {name}</param>
		/// <param name="source">Object used for the formatting</param>
		/// <returns>Formatted string</returns>
		public static string FormatWith(this string format, object source)
		{
			return FormatWith(format, null, source);
		}

		/// <summary>
		///     Format the string "format" with the source
		/// </summary>
		/// <param name="format"></param>
		/// <param name="provider"></param>
		/// <param name="source">
		///     object with properties, if a property has the type IDictionary string,string it can used these
		///     parameters too
		/// </param>
		/// <returns>Formatted string</returns>
		public static string FormatWith(this string format, IFormatProvider provider, object source)
		{
			if (format == null)
			{
				throw new ArgumentNullException(nameof(format));
			}

			IDictionary<string, object> properties = new Dictionary<string, object>();
			foreach (var propertyInfo in  source.GetType().GetProperties())
			{
			    if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
			    {
			        continue;
			    }

			    var value = propertyInfo.GetValue(source, null);
			    if (propertyInfo.PropertyType != typeof(IDictionary<string, string>))
			    {
			        properties.Add(propertyInfo.Name, value);
			    }
			    else
			    {
			        var dictionary = (IDictionary<string, string>) value;
			        foreach (var propertyKey in dictionary.Keys)
			        {
			            properties.Add(propertyKey, dictionary[propertyKey]);
			        }
			    }
			}

			var r = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

			var values = new List<object>();
			var rewrittenFormat = r.Replace(format, m =>
			{
			    var startGroup = m.Groups["start"];
			    var propertyGroup = m.Groups["property"];
			    var formatGroup = m.Groups["format"];
			    var endGroup = m.Groups["end"];

			    values.Add(properties.TryGetValue(propertyGroup.Value, out var value) ? value : source);
			    return new string('{', startGroup.Captures.Count) + (values.Count - 1) + formatGroup.Value + new string('}', endGroup.Captures.Count);
			});

			return string.Format(provider, rewrittenFormat, values.ToArray());
		}

		/// <summary>
		///     Read "streamextensions" :)
		/// </summary>
		/// <param name="input">Stream</param>
		/// <param name="output">Stream</param>
		public static void CopyTo(this Stream input, Stream output)
		{
			var buffer = new byte[16 * 1024]; // Fairly arbitrary size
			int bytesRead;

			while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
			{
				output.Write(buffer, 0, bytesRead);
			}
		}
	}
}