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
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Reflection;

namespace Greenshot.Helpers {
    public class WebsiteImageGenerator {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WebsiteImageGenerator));

		private const int MAX_HEIGHT = 2048;
		private const int MAX_WIDTH = 2048;
        public static Bitmap GetImageFromURL(string Url) {
            WebsiteImage imageGenerator = new WebsiteImage();
            imageGenerator.Url = Url;
            return imageGenerator.GenerateWebSiteImage();
        }

        public static Bitmap GetImageFromHTML(string html) {
            WebsiteImage imageGenerator = new WebsiteImage();
            imageGenerator.Text = html;
            return imageGenerator.GenerateWebSiteImage();
        }

        private class WebsiteImage {
            public WebsiteImage() {
            }

            private string m_Text = null;
            public string Text {
                get {
                    return m_Text;
                }
                set {
                    m_Text = value;
                }
            }

            private string m_Url = null;
            public string Url {
                get {
                    return m_Url;
                }
                set {
                    m_Url = value;
                }
            }
        
            private Bitmap m_Bitmap = null;
            public Bitmap ThumbnailImage {
                get {
                    return m_Bitmap;
                }
            }

            public Bitmap GenerateWebSiteImage() {
                Thread m_thread = new Thread(new ThreadStart(_GenerateWebSiteImage));
                m_thread.SetApartmentState(ApartmentState.STA);
                m_thread.Start();
                m_thread.Join();
                return m_Bitmap;
            }

            private void _GenerateWebSiteImage() {
                WebBrowser m_WebBrowser = new WebBrowser();
                m_WebBrowser.ScrollBarsEnabled = false;
                if (m_Text != null) {
	                m_WebBrowser.DocumentText = m_Text;
                } else {
	                m_WebBrowser.Navigate(m_Url);
                }
                m_WebBrowser.ScriptErrorsSuppressed = true;
                m_WebBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(WebBrowser_DocumentCompleted);
                while (m_WebBrowser.ReadyState != WebBrowserReadyState.Complete) {
                    Application.DoEvents();
                }
                m_WebBrowser.Dispose();
            }

            private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {
            	LOG.Debug("Completed loading of document, rendering to Bitmap!");
                WebBrowser m_WebBrowser = (WebBrowser)sender;
                int browserWidth = m_WebBrowser.Document.Body.ScrollRectangle.Width;
            	int browserHeight = m_WebBrowser.Document.Body.ScrollRectangle.Height;
            	LOG.Debug("Suggested size:" + browserWidth + "x" + browserHeight);
                m_WebBrowser.Height = Math.Min(MAX_HEIGHT, browserHeight);
                m_WebBrowser.Width = Math.Min(MAX_WIDTH, browserWidth);
                m_WebBrowser.ScrollBarsEnabled = false;
                m_Bitmap = new Bitmap(m_WebBrowser.Bounds.Width, m_WebBrowser.Bounds.Height);
                m_WebBrowser.BringToFront();
                m_WebBrowser.DrawToBitmap(m_Bitmap, m_WebBrowser.Bounds);
            }
        }
    }
}