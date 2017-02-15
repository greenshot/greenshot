#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Desktop;
using GreenshotPlugin.Core;
using log4net;

#endregion

namespace GreenshotPlugin.Controls
{
	/// <summary>
	///     The OAuthLoginForm is used to allow the user to authorize Greenshot with an "Oauth" application
	/// </summary>
	public sealed partial class OAuthLoginForm : Form
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(OAuthLoginForm));
		private readonly string _callbackUrl;

		public OAuthLoginForm(string browserTitle, Size size, string authorizationLink, string callbackUrl)
		{
			// Make sure Greenshot uses the correct browser version
			IEHelper.FixBrowserVersion(false);
			_callbackUrl = callbackUrl;
			// Fix for BUG-2071
			if (callbackUrl.EndsWith("/"))
			{
				_callbackUrl = callbackUrl.Substring(0, callbackUrl.Length - 1);
			}
			InitializeComponent();
			ClientSize = size;
			Icon = GreenshotResources.getGreenshotIcon();
			Text = browserTitle;
			_addressTextBox.Text = authorizationLink;

			// The script errors are suppressed by using the ExtendedWebBrowser
			_browser.ScriptErrorsSuppressed = false;
			_browser.DocumentCompleted += Browser_DocumentCompleted;
			_browser.Navigated += Browser_Navigated;
			_browser.Navigating += Browser_Navigating;
			_browser.Navigate(new Uri(authorizationLink));
		}

		public IDictionary<string, string> CallbackParameters { get; private set; }

		public bool IsOk => DialogResult == DialogResult.OK;

		/// <summary>
		///     Make sure the form is visible
		/// </summary>
		/// <param name="e">EventArgs</param>
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// TODO: Await?
			InteropWindowExtensions.ToForegroundAsync(Handle);
		}

		private void Browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			LOG.DebugFormat("document completed with url: {0}", _browser.Url);
			CheckUrl();
		}

		private void Browser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
		{
			LOG.DebugFormat("Navigating to url: {0}", _browser.Url);
			_addressTextBox.Text = e.Url.ToString();
		}

		private void Browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
		{
			LOG.DebugFormat("Navigated to url: {0}", _browser.Url);
			CheckUrl();
		}

		private void CheckUrl()
		{
			if (_browser.Url.ToString().StartsWith(_callbackUrl))
			{
				var queryParams = _browser.Url.Query;
				if (queryParams.Length > 0)
				{
					queryParams = NetworkHelper.UrlDecode(queryParams);
					//Store the Token and Token Secret
					CallbackParameters = NetworkHelper.ParseQueryString(queryParams);
				}
				DialogResult = DialogResult.OK;
			}
		}

		private void AddressTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			//Cancel the key press so the user can't enter a new url
			e.Handled = true;
		}
	}
}