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
using System.Collections.Generic;

namespace Greenshot.Core {
	public enum Destinations {Editor=1, FileDefault=2, FileWithDialog=4, Clipboard=8, Printer=16, EMail=32}
	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Core", Description="Greenshot core configuration")]
	public class CoreConfiguration : IniSection {
		[IniProperty("Language", Description="The language in IETF format (e.g. en-EN)", DefaultValue="en-EN")]
		public string Language;
		[IniProperty("RegisterHotkeys", Description="Register the hotkeys?", DefaultValue="true")]
		public bool RegisterHotkeys;
		[IniProperty("IsFirstLaunch", Description="Is this the first time launch?", DefaultValue="true")]
		public bool IsFirstLaunch;
		[IniProperty("Destinations", Description="Which destinations? Options are: Editor, FileDefault, FileWithDialog, Clipboard, Printer, EMail", DefaultValue="Editor")]
		public List<Destinations> OutputDestinations;
	}
}
