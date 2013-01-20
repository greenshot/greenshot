/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Description of ConfluenceConfiguration.
	/// </summary>
	[Serializable]
	[IniSection("Confluence", Description="Greenshot Confluence Plugin configuration")]
	public class ConfluenceConfiguration : IniSection {
		public const string DEFAULT_POSTFIX1 = "/rpc/soap-axis/confluenceservice-v1?wsdl";
		public const string DEFAULT_POSTFIX2 = "/rpc/soap-axis/confluenceservice-v2?wsdl";
		public const string DEFAULT_PREFIX = "http://";
		private const string DEFAULT_URL = DEFAULT_PREFIX + "confluence";

		[IniProperty("Url", Description="Url to Confluence system, including wsdl.", DefaultValue=DEFAULT_URL)]
		public string Url {
			get;
			set;
		}
		[IniProperty("Timeout", Description="Session timeout in minutes", DefaultValue="30")]
		public int Timeout {
			get;
			set;
		}

		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat {
			get;
			set;
		}
		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality {
			get;
			set;
		}
		[IniProperty("UploadReduceColors", Description="Reduce color amount of the uploaded image to 256", DefaultValue="False")]
		public bool UploadReduceColors {
			get;
			set;
		}
		[IniProperty("OpenPageAfterUpload", Description="Open the page where the picture is uploaded after upload", DefaultValue="True")]
		public bool OpenPageAfterUpload {
			get;
			set;
		}
		[IniProperty("CopyWikiMarkupForImageToClipboard", Description="Copy the Wikimarkup for the recently uploaded image to the Clipboard", DefaultValue="True")]
		public bool CopyWikiMarkupForImageToClipboard {
			get;
			set;
		}
		[IniProperty("SearchSpaceKey", Description="Key of last space that was searched for")]
		public string SearchSpaceKey {
			get;
			set;
		}
		[IniProperty("IncludePersonSpaces", Description = "Include personal spaces in the search & browse spaces list", DefaultValue = "False")]
		public bool IncludePersonSpaces {
			get;
			set;
		}
	}
}
