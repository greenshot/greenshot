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
		public Dictionary<FieldType, object> LastUsedFieldValues;
		
		public void UpdateLastUsedFieldValue(IField f) {
			if(f.Value != null) {
				LastUsedFieldValues[f.FieldType] = f.Value;
			}
		}
		
		public IField GetLastUsedValueForField(IField f) {
			if(LastUsedFieldValues.ContainsKey(f.FieldType)) {
				f.Value = LastUsedFieldValues[f.FieldType];
			} 
			return f;
		}

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "LastFieldValue":
					Dictionary<FieldType, object> fieldDefaults = new Dictionary<FieldType, object>();
					fieldDefaults.Add(FieldType.ARROWHEADS, ArrowContainer.ArrowHeadCombination.END_POINT);
					fieldDefaults.Add(FieldType.BLUR_RADIUS, 3);
					fieldDefaults.Add(FieldType.BRIGHTNESS, 0.9d);
					fieldDefaults.Add(FieldType.FILL_COLOR, Color.Transparent);
					fieldDefaults.Add(FieldType.FLAGS, null);
					fieldDefaults.Add(FieldType.FONT_BOLD, false);
					fieldDefaults.Add(FieldType.FONT_FAMILY, FontFamily.GenericSansSerif.Name);
					fieldDefaults.Add(FieldType.FONT_ITALIC, false);
					fieldDefaults.Add(FieldType.FONT_SIZE, 11f);
					fieldDefaults.Add(FieldType.HIGHLIGHT_COLOR, Color.Yellow);
					fieldDefaults.Add(FieldType.LINE_COLOR, Color.Red);
					fieldDefaults.Add(FieldType.LINE_THICKNESS, 1);
					fieldDefaults.Add(FieldType.MAGNIFICATION_FACTOR, 2);
					fieldDefaults.Add(FieldType.PIXEL_SIZE, 5);
					fieldDefaults.Add(FieldType.PREVIEW_QUALITY, 1.0d);
					fieldDefaults.Add(FieldType.SHADOW, false);
					fieldDefaults.Add(FieldType.PREPARED_FILTER_OBFUSCATE, FilterContainer.PreparedFilter.PIXELIZE);
					fieldDefaults.Add(FieldType.PREPARED_FILTER_HIGHLIGHT, FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT);
					return fieldDefaults; 
			}
			return null;
		}

	}
}
