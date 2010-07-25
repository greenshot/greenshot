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
using System.Drawing;

namespace Greenshot.Drawing.Fields {
	/// <summary>
	/// Defines all FieldTypes + their default value.
	/// (The additional value is why this is not an enum)
	/// </summary>
	[Serializable]
	public class FieldType {
		
		public static readonly FieldType ARROWHEADS = new FieldType("ARROWHEADS", Greenshot.Drawing.ArrowContainer.ArrowHeadCombination.END_POINT);
		public static readonly FieldType BLUR_RADIUS = new FieldType("BLUR_RADIUS", 3);
		public static readonly FieldType BRIGHTNESS = new FieldType("BRIGHTNESS", 0.9d);
		public static readonly FieldType FILL_COLOR = new FieldType("FILL_COLOR", Color.Transparent);
		public static readonly FieldType FONT_BOLD = new FieldType("FONT_BOLD", false);
		public static readonly FieldType FONT_FAMILY = new FieldType("FONT_FAMILY", FontFamily.GenericSansSerif.Name);
		public static readonly FieldType FONT_ITALIC = new FieldType("FONT_ITALIC", false);
		public static readonly FieldType FONT_SIZE = new FieldType("FONT_SIZE", 11f);
		public static readonly FieldType HIGHLIGHT_COLOR = new FieldType("HIGHLIGHT_COLOR", Color.Yellow);
		public static readonly FieldType LINE_COLOR = new FieldType("LINE_COLOR", Color.Red);
		public static readonly FieldType LINE_THICKNESS = new FieldType("LINE_THICKNESS", 1);
		public static readonly FieldType MAGNIFICATION_FACTOR = new FieldType("MAGNIFICATION_FACTOR", 2);
		public static readonly FieldType PIXEL_SIZE = new FieldType("PIXEL_SIZE", 5);
		public static readonly FieldType PREVIEW_QUALITY = new FieldType("PREVIEW_QUALITY", 1.0d);
		public static readonly FieldType SHADOW = new FieldType("SHADOW", false);
		public static readonly FieldType PREPARED_FILTER_OBFUSCATE = new FieldType("PREPARED_FILTER_OBFUSCATE", FilterContainer.PreparedFilter.PIXELIZE);
		public static readonly FieldType PREPARED_FILTER_HIGHLIGHT = new FieldType("PREPARED_FILTER_HIGHLIGHT", FilterContainer.PreparedFilter.TEXT_HIGHTLIGHT);
		public static readonly FieldType FLAGS = new FieldType("FLAGS", null);
		
		public static FieldType[] Values = new FieldType[]{
			ARROWHEADS,
			BLUR_RADIUS,
			BRIGHTNESS,
			FILL_COLOR,
			FONT_BOLD,
			FONT_FAMILY,
			FONT_ITALIC,
			FONT_SIZE,
			HIGHLIGHT_COLOR,
			LINE_COLOR,
			LINE_THICKNESS,
			MAGNIFICATION_FACTOR,
			PIXEL_SIZE,
			PREVIEW_QUALITY,
			SHADOW, 
			PREPARED_FILTER_OBFUSCATE,
			PREPARED_FILTER_HIGHLIGHT, 
			FLAGS
		};
		
		[Flags]
		public enum Flag {
			NONE = 0,
			CONFIRMABLE = 1
		}
		
		
		public object DefaultValue;
		public string Name;
		private FieldType(string name, object defaultValue) {
			Name = name;
			DefaultValue=defaultValue;
		}
		public override string ToString() {
			return this.Name;
		}
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (Name != null)
					hashCode += 1000000009 * Name.GetHashCode();
			}
			return hashCode;
		}
		
		public override bool Equals(object obj)
		{
			FieldType other = obj as FieldType;
			if (other == null)
				return false;
			return object.Equals(this.Name,other.Name);
		}
		
		public static bool operator ==(FieldType a, FieldType b) {
			return object.Equals(a,b);
		}
		
		public static bool operator !=(FieldType a, FieldType b) {
			return !object.Equals(a,b);
		}
		
	}
	
	
}
