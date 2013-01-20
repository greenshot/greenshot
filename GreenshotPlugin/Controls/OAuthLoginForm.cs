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
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OAuthLoginForm));
		private string callbackUrl = null;
		private IDictionary<string, string> callbackParameters = null;
		
		public IDictionary<string, string> CallbackParameters {
			get { return callbackParameters; }
		}
		
		public bool isOk {
			get {
				return DialogResult == DialogResult.OK;
			}
		}
		
		public OAuthLoginForm(string browserTitle, Size size, string authorizationLink, string callbackUrl) {
			this.callbackUrl = callbackUrl;
			InitializeComponent();
			this.ClientSize = size;
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			this.Text = browserTitle;
			this.addressTextBox.Text = authorizationLink;

			// The script errors are suppressed by using the ExtendedWebBrowser
			browser.ScriptErrorsSuppressed = false;
			browser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(browser_DocumentCompleted);
			browser.Navigate(new Uri(authorizationLink));

			WindowDetails.ToForeground(this.Handle);
		}

		private void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {
			LOG.DebugFormat("document completed with url: {0}", browser.Url);
			checkUrl();
		}
		private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e) {
			LOG.DebugFormat("Navigating to url: {0}", browser.Url);
			this.addressTextBox.Text = e.Url.ToString();
		}

		private void browser_Navigated(object sender, WebBrowserNavigatedEventArgs e) {
			LOG.DebugFormat("Navigated to url: {0}", browser.Url);
			checkUrl();
		}

		private void checkUrl() {
			if (browser.Url.ToString().StartsWith(callbackUrl)) {
				string queryParams = browser.Url.Query;
				if (queryParams.Length > 0) {
					queryParams = NetworkHelper.UrlDecode(queryParams);
					//Store the Token and Token Secret
					callbackParameters = NetworkHelper.ParseQueryString(queryParams);
				}
				DialogResult = DialogResult.OK;
			}
		}

		private void addressTextBox_KeyPress(object sender, KeyPressEventArgs e) {
			//Cancel the key press so the user can't enter a new url
			e.Handled = true; 
		}
	}
}
