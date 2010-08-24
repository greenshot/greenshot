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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

using Greenshot.Core;
using Greenshot.Drawing;
using Greenshot.Drawing.Fields;

namespace Greenshot.Configuration {
	/// <summary>
	/// AppConfig is used for loading and saving the configuration. All public fields
	/// in this class are serialized with the BinaryFormatter and then saved to the
	/// config file. After loading the values from file, SetDefaults iterates over
	/// all public fields an sets fields set to null to the default value.
	/// </summary>
	[Serializable]
	public class AppConfig {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(AppConfig));

		private static AppConfig instance = null;
		
		public Dictionary<string, object> LastUsedFieldValues = new Dictionary<string, object>();
		
		/// <summary>
		/// a private constructor because this is a singleton
		/// </summary>
		private AppConfig()	{
		}
		
		/// <summary>
		/// get an instance of AppConfig
		/// </summary>
		/// <returns></returns>
		public static AppConfig GetInstance() {
			if (instance == null) {
				instance = new AppConfig();
			}
			return instance;
		}
		
		public void UpdateLastUsedFieldValue(IField f) {
			if(f.Value != null) {
				string key = GetKeyForField(f);
				LastUsedFieldValues[key] = f.Value;
			}
		}
		
		public IField GetLastUsedValueForField(IField f) {
			string key = GetKeyForField(f);
			if(LastUsedFieldValues.ContainsKey(key)) {
				f.Value = LastUsedFieldValues[key];
			} 
			return f;
		}
		
		/// <param name="f"></param>
		/// <returns></returns>
		/// <param name="f"></param>
		/// <returns>the key under which last used value for the Field can be stored/retrieved</returns>
		private string GetKeyForField(IField f) {
			if(f.Scope == null) {
				return f.FieldType.ToString();
			} else {
				return f.FieldType.ToString() + "-" + f.Scope;
			}
		}
	}
}
