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
using System.Linq;
using Dapplo.Language;

#endregion

namespace Greenshot.Addons.Core
{
    public delegate void LanguageChangedHandler(object sender, EventArgs e);

    /// <summary>
    ///     This class supplies the GUI with translations, based upon keys.
    ///     The language resources are loaded from the language files found on fixed or supplied paths
    /// </summary>
    public class Language
    {
        private static LanguageLoader languageLoader = LanguageLoader.Current;
        /// <summary>
        ///     Get or set the current language
        /// </summary>
        public static string CurrentLanguage
        {
            get => languageLoader.CurrentLanguage;
            set { languageLoader.ChangeLanguageAsync(value); }
        }

        /// <summary>
        ///     Return a list of all the supported languages
        /// </summary>
        public static IDictionary<string, string> SupportedLanguages => languageLoader.AvailableLanguages;


        /// <summary>
        ///     Check if a resource with prefix.key exists
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>true if available</returns>
        public static bool HasKey(string prefix, string key)
        {
            return languageLoader[prefix].Keys().Contains(key);
        }

        /// <summary>
        ///     Check if a resource with prefix.key exists
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>true if available</returns>
        public static bool HasKey(string prefix, Enum key)
        {
            return languageLoader[prefix].Keys().Contains(key.ToString());
        }

        public static string Translate(object key)
        {
            var typename = key.GetType().Name;
            var enumKey = typename + "." + key;
            if (HasKey("Core", enumKey))
            {
                return GetString("Core", enumKey);
            }
            return key.ToString();
        }

        /// <summary>
        ///     TryGet method which combines HasKey & GetString
        /// </summary>
        /// <param name="prefix">string with prefix</param>
        /// <param name="key">string with key</param>
        /// <param name="languageString">out string</param>
        /// <returns>bool</returns>
        public static bool TryGetString(string prefix, string key, out string languageString)
        {
            if (languageLoader.Any(l => l.PrefixName() == prefix) && languageLoader[prefix].Keys().Contains(key))
            {
                languageString = languageLoader[prefix][key];
                return true;
            }

            languageString = null;
            return false;
        }

        /// <summary>
        ///     TryGet method which combines HasKey & GetString
        /// </summary>
        /// <param name="key">string with key</param>
        /// <param name="languageString">out string</param>
        /// <returns>bool</returns>
        public static bool TryGetString(string key, out string languageString)
        {
            return TryGetString("Core", key, out languageString);
        }

        /// <summary>
        ///     Get the resource for prefix.key
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>resource or a "string ###prefix.key### not found"</returns>
        public static string GetString(string prefix, string key)
        {
            if (key == null)
            {
                return null;
            }
            return languageLoader[prefix][key];
        }

        /// <summary>
        ///     Get the resource for key
        /// </summary>
        /// <param name="key"></param>
        /// <returns>resource or a "string ###prefix.key### not found"</returns>
        public static string GetString(string key)
        {
            if (key == null)
            {
                return null;
            }
            return languageLoader["Core"][key];
        }

        /// <summary>
        ///     Get the resource for key
        /// </summary>
        /// <param name="key">Enum</param>
        /// <returns>resource or a "string ###prefix.key### not found"</returns>
        public static string GetString(Enum key)
        {
            if (key == null)
            {
                return null;
            }
            return languageLoader["Core"][key.ToString()];
        }

        /// <summary>
        ///     Get the resource for prefix.key
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <returns>resource or a "string ###prefix.key### not found"</returns>
        public static string GetString(string prefix, Enum key)
        {
            if (key == null)
            {
                return null;
            }
            return GetString(prefix, key.ToString());
        }

        /// <summary>
        ///     Get the resource for key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###key### not found"</returns>
        public static string GetFormattedString(Enum key, object param)
        {
            return GetFormattedString(key.ToString(), param);
        }

        /// <summary>
        ///     Get the resource for prefix.key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###prefix.key### not found"</returns>
        public static string GetFormattedString(string prefix, Enum key, object param)
        {
            return GetFormattedString(prefix, key.ToString(), param);
        }

        /// <summary>
        ///     Get the resource for prefix.key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###prefix.key### not found"</returns>
        public static string GetFormattedString(string prefix, string key, object param)
        {
            if (TryGetString(prefix, key, out var value))
            {
                return string.Format(value, param);
            }

            return $"string ###{prefix}.{key}### not found";
        }

        /// <summary>
        ///     Get the resource for key, format with with string.format an supply the parameters
        /// </summary>
        /// <param name="key"></param>
        /// <param name="param"></param>
        /// <returns>formatted resource or a "string ###key### not found"</returns>
        public static string GetFormattedString(string key, object param)
        {
            return GetFormattedString("Core", key, param);
        }
    }
}