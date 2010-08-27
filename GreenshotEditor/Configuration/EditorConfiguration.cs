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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

using Greenshot.Core;
using Greenshot.Drawing;
using Greenshot.Drawing.Fields;

namespace Greenshot.Editor {
	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Editor", Description="Greenshot editor configuration")]
	public class EditorConfiguration : IniSection {
		[IniProperty("EditorWindowSize", Description="Size of the editor.", DefaultValue="540, 380")]
		public Size EditorWindowSize;
		[IniProperty("EditorWindowLocation", Description="Location of the editor.", DefaultValue="100, 100")]
		public Point EditorWindowLocation;
		[IniProperty("EditorWindowState", Description="The window state of the editor. (Normal or Maximized)", DefaultValue="Normal")]
		public FormWindowState EditorWindowState;
		[IniProperty("EditorPreviousScreenbounds", Description="What screenbound did we have last time? Is used to check screen configuration changes, preventing windows to appear nowhere.", DefaultValue="0,0,0,0")]
		public Rectangle EditorPreviousScreenbounds;

		public Color[] Editor_RecentColors = new Color[12];
		public Font Editor_Font = null;

		[IniProperty("LastFieldValue", Description="Field values, make sure the last used settings are re-used")]
		public Dictionary<string, object> LastUsedFieldValues;

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "LastFieldValue":
					return new Dictionary<string, object>();
			}
			return null;
		}

		public void UpdateLastUsedFieldValue(IField f) {
			string key = GetKeyForField(f);
			if(f.Value != null) {
				if (LastUsedFieldValues.ContainsKey(key)) {
					LastUsedFieldValues[key] = f.Value;
				} else {
					LastUsedFieldValues.Add(key, f.Value);
				}
			}
		}
		
		public IField GetLastUsedValueForField(IField f) {
			string key = GetKeyForField(f);
			if(LastUsedFieldValues.ContainsKey(key)) {
				f.Value = LastUsedFieldValues[key];
			}
			return f;
		}

		private string GetKeyForField(IField f) {
            if(f.Scope == null) {
                return f.FieldType.ToString();
            } else {
                return f.FieldType.ToString() + "-" + f.Scope;
            }
        }
	}
}
