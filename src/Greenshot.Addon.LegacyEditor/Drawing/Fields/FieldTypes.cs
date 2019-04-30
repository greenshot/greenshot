// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Drawing.Fields
{
	/// <summary>
	///     Defines all FieldTypes + their default value.
	///     (The additional value is why this is not an enum)
	/// </summary>
	public static class FieldTypes
	{
	    /// <summary>
	    /// This field specifies which arrow heads are used
	    /// </summary>
	    public static readonly IFieldType ARROWHEADS = new FieldType<ArrowContainer.ArrowHeadCombination>("ARROWHEADS");
        /// <summary>
        /// This field specifies the blur radius
        /// </summary>
	    public static readonly IFieldType BLUR_RADIUS = new FieldType<int>("BLUR_RADIUS");
        /// <summary>
        /// This field specifies the brightness of a filter
        /// </summary>
	    public static readonly IFieldType BRIGHTNESS = new FieldType<double>("BRIGHTNESS");
        /// <summary>
        /// This field specifies the fill color
        /// </summary>
	    public static readonly IFieldType FILL_COLOR = new FieldType<Color>("FILL_COLOR");
        /// <summary>
        /// This field specifies if the font is bold
        /// </summary>
	    public static readonly IFieldType FONT_BOLD = new FieldType<bool>("FONT_BOLD");
        /// <summary>
        /// This field specifies the font family
        /// </summary>
	    public static readonly IFieldType FONT_FAMILY = new FieldType<string>("FONT_FAMILY");
        /// <summary>
        /// This field specifies if the font is italic
        /// </summary>
	    public static readonly IFieldType FONT_ITALIC = new FieldType<bool>("FONT_ITALIC");
        /// <summary>
        /// This field specifies the font size
        /// </summary>
	    public static readonly IFieldType FONT_SIZE = new FieldType<int>("FONT_SIZE");
        /// <summary>
        /// This field specifies the horizontal text alignment
        /// </summary>
	    public static readonly IFieldType TEXT_HORIZONTAL_ALIGNMENT = new FieldType<StringAlignment>("TEXT_HORIZONTAL_ALIGNMENT");
        /// <summary>
        /// This field specifies the vertical text alignment
        /// </summary>
	    public static readonly IFieldType TEXT_VERTICAL_ALIGNMENT = new FieldType<StringAlignment>("TEXT_VERTICAL_ALIGNMENT");
        /// <summary>
        /// This field specifies the highlight color
        /// </summary>
	    public static readonly IFieldType HIGHLIGHT_COLOR = new FieldType<Color>("HIGHLIGHT_COLOR");
        /// <summary>
        /// This field specifies the line color
        /// </summary>
	    public static readonly IFieldType LINE_COLOR = new FieldType<Color>("LINE_COLOR");
        /// <summary>
        /// This field specifies the line thickness
        /// </summary>
	    public static readonly IFieldType LINE_THICKNESS = new FieldType<int>("LINE_THICKNESS");
        /// <summary>
        /// This field specifies the magnification factor for the magnification filter
        /// </summary>
	    public static readonly IFieldType MAGNIFICATION_FACTOR = new FieldType<int>("MAGNIFICATION_FACTOR");
        /// <summary>
        /// This field specifies the pixel size for the pixelate filter
        /// </summary>
	    public static readonly IFieldType PIXEL_SIZE = new FieldType<int>("PIXEL_SIZE");
        /// <summary>
        /// This field specifies if a shadow should be rendered
        /// </summary>
	    public static readonly IFieldType SHADOW = new FieldType<bool>("SHADOW");
        /// <summary>
        /// This field specifies if this is a obfuscate filter
        /// </summary>
	    public static readonly IFieldType PREPARED_FILTER_OBFUSCATE = new FieldType<PreparedFilter>("PREPARED_FILTER_OBFUSCATE");
        /// <summary>
        /// This field specifies if this is a highlight filter
        /// </summary>
	    public static readonly IFieldType PREPARED_FILTER_HIGHLIGHT = new FieldType<PreparedFilter>("PREPARED_FILTER_HIGHLIGHT");
        /// <summary>
        /// This field specifies some flags
        /// </summary>
	    public static readonly IFieldType FLAGS = new FieldType<FieldFlag>("FLAGS");

        /// <summary>
        /// All values
        /// </summary>
	    public static readonly IFieldType[] Values =
	    {
	        ARROWHEADS, BLUR_RADIUS, BRIGHTNESS, FILL_COLOR, FONT_BOLD, FONT_FAMILY, FONT_ITALIC, FONT_SIZE, TEXT_HORIZONTAL_ALIGNMENT, TEXT_VERTICAL_ALIGNMENT, HIGHLIGHT_COLOR, LINE_COLOR, LINE_THICKNESS, MAGNIFICATION_FACTOR, PIXEL_SIZE, SHADOW, PREPARED_FILTER_OBFUSCATE, PREPARED_FILTER_HIGHLIGHT, FLAGS
	    };
	}
}