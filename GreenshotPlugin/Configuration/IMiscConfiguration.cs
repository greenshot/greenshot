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

using System.Collections.Generic;
using System.ComponentModel;

namespace GreenshotPlugin.Configuration
{
	public interface IMiscConfiguration
	{
		[Description("Optimize memory footprint, but with a performance penalty!"), DefaultValue(false)]
		bool MinimizeWorkingSetSize
		{
			get;
			set;
		}

		[Description("Also check for unstable version updates"), DefaultValue(false)]
		bool CheckForUnstable
		{
			get;
			set;
		}

		[Description("The fixes that are active.")]
		IList<string> ActiveTitleFixes
		{
			get;
			set;
		}

		[Description("The regular expressions to match the title with.")]
		IDictionary<string, string> TitleFixMatcher
		{
			get;
			set;
		}

		[Description("The replacements for the matchers.")]
		IDictionary<string, string> TitleFixReplacer
		{
			get;
			set;
		}

		[Description("Enable a special DIB clipboard reader"), DefaultValue(true)]
		bool EnableSpecialDIBClipboardReader
		{
			get;
			set;
		}

		[Description("The 'to' field for the email destination (settings for Outlook can be found under the Office section)"), DefaultValue("")]
		string MailApiTo
		{
			get;
			set;
		}

		[Description("The 'CC' field for the email destination (settings for Outlook can be found under the Office section)"), DefaultValue("")]
		string MailApiCC
		{
			get;
			set;
		}

		[Description("The 'BCC' field for the email destination (settings for Outlook can be found under the Office section)"), DefaultValue("")]
		string MailApiBCC
		{
			get;
			set;
		}

		[Description("Version of Greenshot which created this .ini")]
		string LastSaveWithVersion
		{
			get;
			set;
		}

		[Description("When reading images from files or clipboard, use the EXIF information to correct the orientation"), DefaultValue(true)]
		bool ProcessEXIFOrientation
		{
			get;
			set;
		}
	}
}