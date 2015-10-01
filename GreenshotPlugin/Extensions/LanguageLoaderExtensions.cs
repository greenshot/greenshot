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

using Dapplo.Config.Language;

namespace GreenshotPlugin.Extensions
{
	public static class LanguageLoaderExtensions
	{
		public static string Translate(this LanguageLoader loader, string languageKey, string languageModule)
		{
			if (string.IsNullOrEmpty(languageModule))
			{
				var keyParts = languageKey.Split('.');

				if (keyParts.Length > 1)
				{
					languageModule = keyParts[0];
					languageKey = keyParts[1];
				}
				else
				{
					languageModule = "Core";

				}
			}
			return LanguageLoader.Current[languageModule][languageKey];
		}

		/// <summary>
		/// Search for a translation of the languageKeyObject string-value in the languageModule
		/// </summary>
		/// <param name="loader"></param>
		/// <param name="languageModule"></param>
		/// <param name="languageKeyObject"></param>
		/// <returns>Returns the translation or languageKeyObject.ToString() when none is found</returns>
		public static string Translate(this LanguageLoader loader, object languageKeyObject, string languageModule = "Core")
		{
			string typename = languageKeyObject.GetType().Name;
			string languageKey = typename + "." + languageKeyObject.ToString();
			// Even if there is a default, null can be supplied!
			if (string.IsNullOrEmpty(languageModule))
			{
				languageModule = "Core";
            }
			var translation = LanguageLoader.Current[languageModule][languageKey];
			if (string.IsNullOrEmpty(translation))
			{
				return languageKeyObject.ToString();
            }
            return translation;
		}
	}
}
