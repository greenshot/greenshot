//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;

#endregion

namespace Greenshot.Addon.Confluence.Support
{
	public class TranslationManager
	{
		private static TranslationManager _translationManager;

		public static TranslationManager Instance
		{
			get
			{
				if (_translationManager == null)
				{
					_translationManager = new TranslationManager();
				}
				return _translationManager;
			}
		}

		public ITranslationProvider TranslationProvider { get; set; }

		public event EventHandler LanguageChanged;

		private void OnLanguageChanged()
		{
			if (LanguageChanged != null)
			{
				LanguageChanged(this, EventArgs.Empty);
			}
		}

		public object Translate(string key)
		{
			if (TranslationProvider != null)
			{
				object translatedValue = TranslationProvider.Translate(key);
				if (translatedValue != null)
				{
					return translatedValue;
				}
			}
			return string.Format("!{0}!", key);
		}
	}
}