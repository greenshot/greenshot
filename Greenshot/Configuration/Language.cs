/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

using GreenshotPlugin.Core;

namespace Greenshot.Configuration {
	/// <summary>
	/// Wrapper for the language container for the Greenshot base.
	/// </summary>
	public class Language : LanguageContainer, ILanguage  {
		private static ILanguage uniqueInstance;
		private const string LANGUAGE_FILENAME_PATTERN = @"language-*.xml";
		
		public static ILanguage GetInstance() {
			if(uniqueInstance == null) {
				uniqueInstance = new LanguageContainer();
				uniqueInstance.LanguageFilePattern = LANGUAGE_FILENAME_PATTERN;
				uniqueInstance.Load();
				uniqueInstance.SetLanguage(AppConfig.GetInstance().Ui_Language);
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(uniqueInstance.CurrentLanguage);
			}
			return uniqueInstance;
		}
	}
}
