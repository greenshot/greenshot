#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using log4net;

#endregion

namespace GreenshotPlugin.Core
{
	public static class StringExtensions
	{
		private const string RGBIV = "dlgjowejgogkklwj";
		private const string KEY = "lsjvkwhvwujkagfauguwcsjgu2wueuff";
		private static readonly ILog LOG = LogManager.GetLogger(typeof(StringExtensions));

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
				if (propertyInfo.CanRead && propertyInfo.CanWrite)
				{
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
			}

			var r = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

			var values = new List<object>();
			var rewrittenFormat = r.Replace(format, delegate(Match m)
			{
				var startGroup = m.Groups["start"];
				var propertyGroup = m.Groups["property"];
				var formatGroup = m.Groups["format"];
				var endGroup = m.Groups["end"];

				object value;
				values.Add(properties.TryGetValue(propertyGroup.Value, out value) ? value : source);
				return new string('{', startGroup.Captures.Count) + (values.Count - 1) + formatGroup.Value + new string('}', endGroup.Captures.Count);
			});

			return string.Format(provider, rewrittenFormat, values.ToArray());
		}

		/// <summary>
		///     A simply rijndael aes encryption, can be used to store passwords
		/// </summary>
		/// <param name="clearText">the string to call upon</param>
		/// <returns>an encryped string in base64 form</returns>
		public static string Encrypt(this string clearText)
		{
			var returnValue = clearText;
			try
			{
				var clearTextBytes = Encoding.ASCII.GetBytes(clearText);
				var rijn = SymmetricAlgorithm.Create();

				using (var ms = new MemoryStream())
				{
					var rgbIV = Encoding.ASCII.GetBytes(RGBIV);
					var key = Encoding.ASCII.GetBytes(KEY);
					using (var cs = new CryptoStream(ms, rijn.CreateEncryptor(key, rgbIV), CryptoStreamMode.Write))
					{
						cs.Write(clearTextBytes, 0, clearTextBytes.Length);
						cs.FlushFinalBlock();

						returnValue = Convert.ToBase64String(ms.ToArray());
					}
				}
			}
			catch (Exception ex)
			{
				LOG.ErrorFormat("Error encrypting, error: {0}", ex.Message);
			}
			return returnValue;
		}

		/// <summary>
		///     A simply rijndael aes decryption, can be used to store passwords
		/// </summary>
		/// <param name="encryptedText">a base64 encoded rijndael encrypted string</param>
		/// <returns>Decrypeted text</returns>
		public static string Decrypt(this string encryptedText)
		{
			var returnValue = encryptedText;
			try
			{
				var encryptedTextBytes = Convert.FromBase64String(encryptedText);
				using (var ms = new MemoryStream())
				{
					var rijn = SymmetricAlgorithm.Create();


					var rgbIV = Encoding.ASCII.GetBytes(RGBIV);
					var key = Encoding.ASCII.GetBytes(KEY);

					using (var cs = new CryptoStream(ms, rijn.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write))
					{
						cs.Write(encryptedTextBytes, 0, encryptedTextBytes.Length);
						cs.FlushFinalBlock();
						returnValue = Encoding.ASCII.GetString(ms.ToArray());
					}
				}
			}
			catch (Exception ex)
			{
				LOG.ErrorFormat("Error decrypting {0}, error: {1}", encryptedText, ex.Message);
			}

			return returnValue;
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