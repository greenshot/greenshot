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
using System.Windows.Forms;

using Greenshot.Core;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Description of JiraConfiguration.
	/// </summary>
	[IniSection("Jira", Description="Greenshot Jira Plugin configuration")]
	public class JiraConfiguration : IniSection {
		public const string DEFAULT_POSTFIX = "/rpc/soap/jirasoapservice-v2?wsdl";
		public const string DEFAULT_PREFIX = "http://";
		private const string DEFAULT_URL = DEFAULT_PREFIX + "jira" + DEFAULT_POSTFIX;

		[IniProperty("Url", Description="Url to Jira system, including wsdl.", DefaultValue=DEFAULT_URL)]
		public string Url;
		[IniProperty("Timeout", Description="Session timeout in minutes", DefaultValue="30")]
		public int Timeout;
		
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="Png")]
		public OutputFormat UploadFormat;
		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality;

		/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
        public bool ShowConfigDialog() {
			SettingsForm pwForm = new SettingsForm();
        	pwForm.Url = Url;
        	DialogResult result = pwForm.ShowDialog();
        	if (result == DialogResult.OK) {
            	if (!pwForm.Url.Equals(Url)) {
            		Url = pwForm.Url;
            	}
           		IniConfig.Save();
        		return true;
        	}
        	return false;
        }
	}
}
