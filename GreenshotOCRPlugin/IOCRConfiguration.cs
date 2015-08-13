/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Config.Ini;
using System.ComponentModel;

namespace GreenshotOCR {
	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("OCR"), Description("Greenshot OCR Plugin configuration")]
	public interface IOCRConfiguration : IIniSection<IOCRConfiguration> {
		[Description("Language for OCR"), DefaultValue("ENGLISH")]
		string Language
		{
			get;
			set;
		}

		[Description("Orient image?"), DefaultValue(true)]
		bool Orientimage
		{
			get;
			set;
		}

		[Description("Straighten image?"), DefaultValue(true)]
		bool StraightenImage
		{
			get;
			set;
		}
	}
}
