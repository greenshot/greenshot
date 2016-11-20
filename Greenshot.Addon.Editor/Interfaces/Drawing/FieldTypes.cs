using System;

namespace Greenshot.Addon.Interfaces.Drawing
{
	/// <summary>
	///     This is used to mark the fields that are important for the editor in the container
	/// </summary>
	[Serializable]
	public enum FieldTypes
	{
		ARROWHEADS,
		BLUR_RADIUS,
		BRIGHTNESS,
		FILL_COLOR,
		FONT_BOLD,
		FONT_FAMILY,
		FONT_ITALIC,
		FONT_SIZE,
		TEXT_HORIZONTAL_ALIGNMENT,
		TEXT_VERTICAL_ALIGNMENT,
		HIGHLIGHT_COLOR,
		LINE_COLOR,
		LINE_THICKNESS,
		MAGNIFICATION_FACTOR,
		PIXEL_SIZE,
		SHADOW,
		PREPARED_FILTER_OBFUSCATE,
		PREPARED_FILTER_HIGHLIGHT,
		FLAGS,
		COUNTER_START
	}
}