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

namespace GreenshotOfficePlugin.OfficeInterop
{
	/// <summary>
	///     Units: http://msdn.microsoft.com/en-us/library/office/bb214015(v=office.12).aspx
	/// </summary>
	public enum WdUnits
	{
		wdCell = 12,
		wdCharacter = 1,
		wdCharacterFormatting = 13,
		wdColumn = 9,
		wdItem = 16,
		wdLine = 5,
		wdParagraph = 4,
		wdParagraphFormatting = 14,
		wdRow = 10,
		wdScreen = 7,
		wdSection = 8,
		wdSentence = 3,
		wdStory = 6,
		wdTable = 15,
		wdWindow = 11,
		wdWord = 2
	}
}