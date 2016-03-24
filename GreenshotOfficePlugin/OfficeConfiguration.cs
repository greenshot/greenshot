/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.IniFile;
using Greenshot.Interop.Office;

namespace GreenshotOfficePlugin {

	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Office", Description="Greenshot Office configuration")]
	public class OfficeConfiguration : IniSection {
		[IniProperty("OutlookEmailFormat", Description = "Default type for emails. (Text, HTML)", DefaultValue="HTML")]
		public EmailFormat OutlookEmailFormat;
		[IniProperty("EmailSubjectPattern", Description = "Email subject pattern, works like the OutputFileFilenamePattern", DefaultValue = "${title}")]
		public string EmailSubjectPattern;
		[IniProperty("EmailTo", Description = "Default value for the to in emails that are created", DefaultValue = "")]
		public string EmailTo;
		[IniProperty("EmailCC", Description = "Default value for the CC in emails that are created", DefaultValue = "")]
		public string EmailCC;
		[IniProperty("EmailBCC", Description = "Default value for the BCC in emails that are created", DefaultValue = "")]
		public string EmailBCC;
		[IniProperty("OutlookAllowExportInMeetings", Description = "For Outlook: Allow export in meeting items", DefaultValue = "False")]
		public bool OutlookAllowExportInMeetings;
		[IniProperty("WordLockAspectRatio", Description = "For Word: Lock the aspect ratio of the image", DefaultValue = "True")]
		public bool WordLockAspectRatio;
		[IniProperty("PowerpointLockAspectRatio", Description = "For Powerpoint: Lock the aspect ratio of the image", DefaultValue = "True")]
		public bool PowerpointLockAspectRatio;
		[IniProperty("PowerpointSlideLayout", Description = "For Powerpoint: Slide layout, changing this to a wrong value will fallback on ppLayoutBlank!!", DefaultValue = "ppLayoutPictureWithCaption")]
		public PPSlideLayout PowerpointSlideLayout;

	}
}