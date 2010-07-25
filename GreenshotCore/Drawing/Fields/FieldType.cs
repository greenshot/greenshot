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
	public enum FieldType {
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
	}
	
	[Flags]
	public enum FieldFlag {
		NONE = 0,
		CONFIRMABLE = 1
	}
	
}
