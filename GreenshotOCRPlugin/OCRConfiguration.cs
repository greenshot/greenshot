﻿/*
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

using GreenshotPlugin.IniFile;

namespace GreenshotOCRPlugin {
	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("OCR", Description="Greenshot OCR Plugin configuration")]
	public class OCRConfiguration : IniSection {
		[IniProperty("Language", Description="Language for OCR", DefaultValue="miLANG_ENGLISH")]
		public string Language { get; set; }
		[IniProperty("orientimage", Description="Orient image?", DefaultValue="true")]
		public bool Orientimage { get; set; }
		[IniProperty("straightenImage", Description="Straighten image?", DefaultValue="true")]
		public bool StraightenImage { get; set; }
	}
}
