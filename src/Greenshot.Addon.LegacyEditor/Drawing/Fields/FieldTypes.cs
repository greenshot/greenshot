#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System.Drawing;
using Greenshot.Addons.Interfaces.Drawing;

#endregion

namespace Greenshot.Addon.LegacyEditor.Drawing.Fields
{
	/// <summary>
	///     Defines all FieldTypes + their default value.
	///     (The additional value is why this is not an enum)
	/// </summary>
	public static class FieldTypes
	{
	    public static readonly IFieldType ARROWHEADS = new FieldType<ArrowContainer.ArrowHeadCombination>("ARROWHEADS");
	    public static readonly IFieldType BLUR_RADIUS = new FieldType<int>("BLUR_RADIUS");
	    public static readonly IFieldType BRIGHTNESS = new FieldType<int>("BRIGHTNESS");
	    public static readonly IFieldType FILL_COLOR = new FieldType<Color>("FILL_COLOR");
	    public static readonly IFieldType FONT_BOLD = new FieldType<bool>("FONT_BOLD");
	    public static readonly IFieldType FONT_FAMILY = new FieldType<string>("FONT_FAMILY");
	    public static readonly IFieldType FONT_ITALIC = new FieldType<bool>("FONT_ITALIC");
	    public static readonly IFieldType FONT_SIZE = new FieldType<int>("FONT_SIZE");
	    public static readonly IFieldType TEXT_HORIZONTAL_ALIGNMENT = new FieldType<StringAlignment>("TEXT_HORIZONTAL_ALIGNMENT");
	    public static readonly IFieldType TEXT_VERTICAL_ALIGNMENT = new FieldType<StringAlignment>("TEXT_VERTICAL_ALIGNMENT");
	    public static readonly IFieldType HIGHLIGHT_COLOR = new FieldType<Color>("HIGHLIGHT_COLOR");
	    public static readonly IFieldType LINE_COLOR = new FieldType<Color>("LINE_COLOR");
	    public static readonly IFieldType LINE_THICKNESS = new FieldType<int>("LINE_THICKNESS");
	    public static readonly IFieldType MAGNIFICATION_FACTOR = new FieldType<int>("MAGNIFICATION_FACTOR");
	    public static readonly IFieldType PIXEL_SIZE = new FieldType<int>("PIXEL_SIZE");
	    public static readonly IFieldType PREVIEW_QUALITY = new FieldType<int>("PREVIEW_QUALITY");
	    public static readonly IFieldType SHADOW = new FieldType<bool>("SHADOW");
	    public static readonly IFieldType PREPARED_FILTER_OBFUSCATE = new FieldType<FilterContainer.PreparedFilter>("PREPARED_FILTER_OBFUSCATE");
	    public static readonly IFieldType PREPARED_FILTER_HIGHLIGHT = new FieldType<FilterContainer.PreparedFilter>("PREPARED_FILTER_HIGHLIGHT");
	    public static readonly IFieldType FLAGS = new FieldType<FieldFlag>("FLAGS");

	    public static IFieldType[] Values =
	    {
	        ARROWHEADS, BLUR_RADIUS, BRIGHTNESS, FILL_COLOR, FONT_BOLD, FONT_FAMILY, FONT_ITALIC, FONT_SIZE, TEXT_HORIZONTAL_ALIGNMENT, TEXT_VERTICAL_ALIGNMENT, HIGHLIGHT_COLOR, LINE_COLOR, LINE_THICKNESS, MAGNIFICATION_FACTOR, PIXEL_SIZE, PREVIEW_QUALITY, SHADOW, PREPARED_FILTER_OBFUSCATE, PREPARED_FILTER_HIGHLIGHT, FLAGS
	    };
	}
}