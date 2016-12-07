//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.ComponentModel;
using Dapplo.Ini.Converters;
using Dapplo.Ini;
using Greenshot.Core.Configuration;

#endregion

namespace Greenshot.Addon.Jira
{
	/// <summary>
	///     Description of JiraConfiguration.
	/// </summary>
	[IniSection("Jira")]
	[Description("Greenshot Jira Plugin configuration")]
	public interface IJiraConfiguration : IIniSection<IJiraConfiguration>
	{
		[Description("Pattern for the filename that is used for uploading to JIRA")]
		[DefaultValue("${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title:s0,10}")]
		string FilenamePattern { get; set; }

		[Description("Password for Jira system")]
		[TypeConverter(typeof(StringEncryptionTypeConverter))]
		string Password { get; set; }

		[Description("Rest Url to Jira system")]
		[DefaultValue("https://jira")]
		string RestUrl { get; set; }

		[Description("What file type to use for uploading")]
		[DefaultValue(OutputFormat.png)]
		OutputFormat UploadFormat { get; set; }

		[Description("JPEG file save quality in %.")]
		[DefaultValue(80)]
		int UploadJpegQuality { get; set; }

		[Description("Reduce color amount of the uploaded image to 256")]
		[DefaultValue(false)]
		bool UploadReduceColors { get; set; }

		[Description("Username for Jira system")]
		string Username { get; set; }
	}
}