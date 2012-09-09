/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Web;
using System.Collections.Specialized;
using GreenshotPlugin.Core;

namespace GreenshotPlugin.Controls {
	/// <summary>
	/// The OAuthLoginForm is used to allow the user to authorize Greenshot with an "Oauth" application
	/// </summary>
	public partial class OAuthLoginForm : Form {
		private OAuthHelper _oauth;
		private String _token;
		private String _verifier;
		private String _tokenSecret;

		public String Token {
			get {
				return _token;
			}
		}

		public String Verifier {
			get {
				return _verifier;
			}
		}

		public String TokenSecret {
			get {
				return _tokenSecret;
			}
		}

		public OAuthLoginForm(OAuthHelper o, string browserTitle) {
			_oauth = o;
			_token = null;
			InitializeComponent();
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			this.Text = browserTitle;
			this.addressTextBox.Text = o.AuthorizationLink;
			_token = _oauth.Token;
			_tokenSecret = _oauth.TokenSecret;
			browser.Navigate(new Uri(_oauth.AuthorizationLink));
		}

		private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e) {
			this.addressTextBox.Text = e.Url.ToString();
		}

		private void browser_Navigated(object sender, WebBrowserNavigatedEventArgs e) {
			if (browser.Url.ToString().Contains(_oauth.CallbackUrl))  {
				string queryParams = e.Url.Query;
				if (queryParams.Length > 0) {
					//Store the Token and Token Secret
					NameValueCollection qs = NetworkHelper.ParseQueryString(queryParams);
					if (qs["oauth_token"] != null) {
						_token = qs["oauth_token"];
					}
					if (qs["oauth_verifier"] != null) {
						_verifier = qs["oauth_verifier"];
					}
				}
				this.Close();
			}
		}

		private void addressTextBox_KeyPress(object sender, KeyPressEventArgs e) {
			//Cancel the key press so the user can't enter a new url
			e.Handled = true; 
		}
	}
}
