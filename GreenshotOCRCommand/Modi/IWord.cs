/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

namespace GreenshotOCRCommand.Modi
{
	/// <summary>
	/// Represents a word recognized in the text during an optical character recognition (OCR) operation.
	/// </summary>
	public interface IWord : ICommon
	{
		/// <summary>
		/// Returns the index of the specified word in the Words collection of the Layout or IMiSelectableItem object.
		/// </summary>
		long Id { get; }

		/// <summary>
		/// Returns the number of the region in the optical character recognition (OCR) layout where the word occurs.
		/// </summary>
		long RegionId { get; }

		/// <summary>
		/// Returns the number of the line in the optical character recognition (OCR) layout where the word occurs.
		/// </summary>
		long LineId { get; }

		/// <summary>
		/// Returns the recognized text as a Unicode string.
		/// </summary>
		string Text { get; }

		/// <summary>
		/// Returns the relative confidence factor reported by the optical character recognition (OCR) engine (on a scale of 0 to 999) after recognizing the specified word.
		/// </summary>
		short RecognitionConfidence { get; }

		/// <summary>
		/// Returns the index of the font used by the specified wordthis is the font that was recognized in the text during an optical character recognition (OCR) operation.
		/// </summary>
		long FontId { get; }

		/// <summary>
		/// Rectangles 
		/// </summary>
		IMiRects Rects { get; }

	}
}
