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
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("JIRA", Description="Greenshot JIRA Plugin configuration")]
	public class JiraConfiguration : IniSection {
		[IniProperty("Url", Description="Url to Jira system, including wsdl.", DefaultValue="http://jira/rpc/soap/jirasoapservice-v2?wsdl")]
		public string Url;
		[IniProperty("Timeout", Description="Session timeout in minutes", DefaultValue="30")]
		public int Timeout;
		[IniProperty("User", Description="User for the JIRA System")]
		public string User;
		[IniProperty("Password", Description="Password for the JIRA System, belonging to user.")]
		public string Password;
		
		// This will not be stored
		public string TmpPassword;
		
		public bool HasPassword() {
        	return (Password != null && Password.Length > 0);
		}

		public bool HasTmpPassword() {
        	return (TmpPassword != null && TmpPassword.Length > 0);
		}

		/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
        public bool ShowConfigDialog() {
			LoginForm pwForm = new LoginForm();
			if (User == null || User.Length == 0) {
				User = Environment.UserName;
			}
        	pwForm.User = User;
        	pwForm.Url = Url;
        	DialogResult result = pwForm.ShowDialog();
        	if (result == DialogResult.OK) {
        		if (pwForm.DoNotStorePassword) {
        			TmpPassword = pwForm.Password;
        			Password = null;
        		} else {
        			Password = pwForm.Password;
        			TmpPassword = null;
        		}
            	
            	if (!pwForm.User.Equals(User) ||!pwForm.Url.Equals(Url)) {
            		User = pwForm.User;
            		Url = pwForm.Url;
            	}
           		IniConfig.Save();
        		return true;
        	}
        	return false;
        }
	}
}
