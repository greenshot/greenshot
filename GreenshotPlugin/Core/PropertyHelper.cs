/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// A Class to representate a simple "java" properties file
	/// </summary>
	public class Properties : Dictionary<string, string >{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Properties));

		public string GetProperty(string key) {
			try {
				return this[key];
			} catch (KeyNotFoundException) {
				return null;
			}
		}
		/// <summary>
		/// Split property with ',' and return the splitted string as a string[]
		/// </summary>
		public string[] GetPropertyAsArray(string key) {
			try {
				string array = this[key];
				return array.Split(new Char[] {','});
			} catch (KeyNotFoundException) {
				return null;
			}
		}
		public bool GetBoolProperty(string key) {
			if (this.ContainsKey(key)) {
				return bool.Parse(this[key]);
			}
			return false;
		}
		public uint GetUIntProperty(string key) {
			return uint.Parse(this[key]);
		}
		public int GetIntProperty(string key) {
			return int.Parse(this[key]);
		}
		public void AddProperty(string key, string value) {
			Add(key, value);
		}
		public void AddBoolProperty(string key, bool value) {
			AddProperty(key, value.ToString());
		}
		public void ChangeProperty(string key, string value) {
			if (this.ContainsKey(key)) {
				this[key] = value;
			} else {
				throw new KeyNotFoundException(key);
			}
		}
		public void ChangeBoolProperty(string key, bool value) {
			ChangeProperty(key, value.ToString());
		}
		
		public void write(string filename) {
			using ( TextWriter textWriter = new StreamWriter(filename)) {
				foreach(string key in Keys) {
					textWriter.WriteLine(key +"=" + this[key]);
				}
			}
		}

		public void write(string filename, string header) {
			using ( TextWriter textWriter = new StreamWriter(filename)) {
				if (header != null) {
					textWriter.WriteLine(header);
				}
				foreach(string key in Keys) {
					textWriter.WriteLine(key +"=" + this[key]);
				}
			}
		}

		// Read properties file
		public static Properties read(string filename) {
			LOG.Debug("Reading properties from file: " + filename);
			if (!File.Exists(filename)) {
				return null;
			}
			Properties properties = new Properties();
			foreach (string line in File.ReadAllLines(filename)) {
				if (line == null) {
					continue;
				}
				string currentLine = line.Trim();
				if (!currentLine.StartsWith("#") && currentLine.IndexOf('=') > 0) {
					string [] split = currentLine.Split(new Char[] {'='}, 2);
					if (split != null && split.Length == 2) {
						string name = split[0];
						if (name == null || name.Length < 1) {
							continue;
						}
						properties.Add(name.Trim(), split[1]);
					}
				}
			}
			return properties;
		}
	}
}
