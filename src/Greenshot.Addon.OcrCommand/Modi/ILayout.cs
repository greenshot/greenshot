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

namespace Greenshot.Addon.OcrCommand.Modi
{
	/// <summary>
	///     Layout of the IImage
	/// </summary>
	public interface ILayout : ICommon
	{
		/// <summary>
		///     Returns the recognized text as a Unicode string.
		/// </summary>
		string Text { get; }

		/// <summary>
		///     An accessor property that returns the Words collection recognized in the text during an optical character
		///     recognition (OCR) operation.
		/// </summary>
		IWords Words { get; }

		/// <summary>
		///     Returns the number of characters in the recognized text.
		/// </summary>
		int NumChars { get; }

		/// <summary>
		///     Returns the number of words in the recognized text.
		/// </summary>
		int NumWords { get; }

		/// <summary>
		///     Returns the language identifier for the recognized text. Read-only Long.
		/// </summary>
		ModiLanguage Language { get; }
	}
}