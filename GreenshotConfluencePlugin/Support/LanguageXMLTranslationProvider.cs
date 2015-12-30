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

using Dapplo.Config.Language;

namespace GreenshotConfluencePlugin.Support
{
	/// <summary>
	/// 
	/// </summary>
	public class LanguageXMLTranslationProvider : ITranslationProvider
	{
		private readonly IConfluenceLanguage _language = LanguageLoader.Current.Get<IConfluenceLanguage>();

		#region Construction

		/// <summary>
		/// Initializes a new instance of the <see cref="ResxTranslationProvider"/> class.
		/// </summary>
		public LanguageXMLTranslationProvider()
		{
		}

		#endregion

		#region ITranslationProvider Members

		/// <summary>
		/// See <see cref="ITranslationProvider.Translate" />
		/// </summary>
		public object Translate(string key)
		{
			return _language[key];
		}

		#endregion

		#region ITranslationProvider Members

		/// <summary>
		/// See <see cref="ITranslationProvider.AvailableLanguages" />
		/// </summary>
		/*public IEnumerable<CultureInfo> Languages {
            get {
				foreach (LanguageFile supportedLanguage in Language.SupportedLanguages) {
            		yield return new CultureInfo(supportedLanguage.Ietf);
            	}
            }
        }*/

		#endregion
	}
}