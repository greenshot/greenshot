/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// The OAuthLoginForm is used to allow the user to authorize Greenshot with an "Oauth" application
    /// </summary>
    public sealed partial class OAuthLoginForm : Form
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(OAuthLoginForm));
        private readonly string _callbackUrl;

        public IDictionary<string, string> CallbackParameters { get; private set; }

        public bool IsOk => DialogResult == DialogResult.OK;

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
            Icon = GreenshotResources.GetGreenshotIcon();
            Text = browserTitle;
            _addressTextBox.Text = authorizationLink;

            // The script errors are suppressed by using the ExtendedWebBrowser
            _browser.ScriptErrorsSuppressed = false;
            _browser.DocumentCompleted += Browser_DocumentCompleted;
            _browser.Navigated += Browser_Navigated;
            _browser.Navigating += Browser_Navigating;
            _browser.Navigate(new Uri(authorizationLink));
        }

        /// <summary>
        /// Make sure the form is visible
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            WindowDetails.ToForeground(Handle);
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
                var correctedUri = new Uri(_browser.Url.AbsoluteUri.Replace("#", "&"));

                string queryParams = correctedUri.Query;
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