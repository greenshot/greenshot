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
using GreenshotPlugin.Core;
using System.ComponentModel;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Description of ConfluenceConfiguration.
	/// </summary>
	[IniSection("Confluence"), Description("Greenshot Confluence Plugin configuration")]
	public interface IConfluenceConfiguration : IIniSection<IConfluenceConfiguration>, INotifyPropertyChanged {
		[Description("Rest Url to Confluence system"), DefaultValue("https://confluence")]
		string RestUrl {
			get;
			set;
		}

		[Description("Session timeout in minutes"), DefaultValue("30")]
		int Timeout {
			get;
			set;
		}

		[Description("What file type to use for uploading"), DefaultValue(OutputFormat.png)]
		OutputFormat UploadFormat {
			get;
			set;
		}

		[Description("JPEG file save quality in %."), DefaultValue(80)]
		int UploadJpegQuality {
			get;
			set;
		}

		[Description("Reduce color amount of the uploaded image to 256"), DefaultValue(false)]
		bool UploadReduceColors {
			get;
			set;
		}

		[Description("Pattern for the filename that is used for uploading to Confluence"), DefaultValue("${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title:s0,10}")]
		string FilenamePattern {
			get;
			set;
		}

		[Description("Open the page where the picture is uploaded after upload"), DefaultValue(true)]
		bool OpenPageAfterUpload {
			get;
			set;
		}
		[Description("Copy the Wikimarkup for the recently uploaded image to the Clipboard"), DefaultValue(true)]
		bool CopyWikiMarkupForImageToClipboard {
			get;
			set;
		}
		[Description("Key of last space that was searched for")]
		string SearchSpaceKey {
			get;
			set;
		}
		[Description("Include personal spaces in the search & browse spaces list"), DefaultValue(false)]
		bool IncludePersonSpaces {
			get;
			set;
		}
	}
}
