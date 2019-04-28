// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Config.Language;
using Dapplo.Utils.Extensions;

namespace Greenshot.Addons.Extensions
{
    /// <summary>
    /// Extensions to support with ILanguage
    /// </summary>
    public static class LanguageExtensions
    {
        /// <summary>
        /// Returns possible translations for all enum values
        /// </summary>
        /// <typeparam name="TEnum">Generic for the enum type</typeparam>
        /// <param name="language">ILanguage</param>
        /// <returns>dictionary with possible translations</returns>
        public static IDictionary<TEnum, string> TranslationValuesForEnum<TEnum>(this ILanguage language)

        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToDictionary(
                enumValue => enumValue,
                language.Translate);
        }

        /// <summary>
        /// Returns the best translation for an enum
        /// </summary>
        /// <typeparam name="TEnum">Generic for the enum type</typeparam>
        /// <param name="language">ILanguage</param>
        /// <param name="enumValue">TEnum</param>
        /// <returns>translation</returns>
        public static string Translate<TEnum>(this ILanguage language, TEnum enumValue)
        {
            var enumType = typeof(TEnum);
            var result = enumType.ToString();

            var keys = language.Keys();
            var key = $"{enumType.Name}.{result}";
            if (keys.Contains(key))
            {
                return language[key];
            }

            if (keys.Contains(result))
            {
                return language[result];
            }

            if (enumValue is Enum unspecifiedEnumValue)
            {
                result = unspecifiedEnumValue.EnumValueOf();
            }
            key = $"{enumType.Name}.{result}";
            if (keys.Contains(key))
            {
                return language[key];
            }
            if (keys.Contains(result))
            {
                return language[result];
            }
            return result;
        }
    }
}
