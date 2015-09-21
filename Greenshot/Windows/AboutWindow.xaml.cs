/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Forms;
using GreenshotPlugin.Core;
using log4net;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;

namespace Greenshot.Windows
{
	/// <summary>
	/// Logic for the About.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(AboutWindow));
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		private static readonly object lockObject = new Object();
		private static AboutWindow _aboutWindow;

		public static void Create()
		{
			lock (lockObject)
			{
				if (_aboutWindow == null)
				{
					_aboutWindow = new AboutWindow();
					ElementHost.EnableModelessKeyboardInterop(_aboutWindow);
					_aboutWindow.Show();
				}
			}
		}

		public AboutWindow()
		{
			Icon = GreenshotResources.GetGreenshotIcon().ToBitmapSource();
			InitializeComponent();
			Closing += Window_Closing;
			Version v = Assembly.GetExecutingAssembly().GetName().Version;
			VersionLabel.Content = "Greenshot " + v.Major + "." + v.Minor + "." + v.Build + " Build " + v.Revision + (PortableHelper.IsPortable ? " Portable" : "") + (" (" + Helpers.OSInfo.Bits + " bit)");

			GreenshotPlugin.Core.Language.LanguageChanged += Language_LanguageChanged;

			SetTranslations();
		}

		private void Language_LanguageChanged(object sender, EventArgs e)
		{
			SetTranslations();
		}

		void SetTranslations()
		{
			Title = language.AboutTitle;
			AboutBugs.Text = language.AboutBugs;
			AboutDonations.Text = language.AboutDonations;
			AboutIcons.Text = language.AboutIcons;
			AboutLicense.Text = language.AboutLicense;
			if (string.IsNullOrEmpty(language.AboutTranslation))
			{
				AboutTranslation.Text = language.AboutTranslation;
			}

		}

		/// <summary>
		/// Make sure the reference is gone
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Window_Closing(object sender, CancelEventArgs e)
		{
			_aboutWindow = null;
			GreenshotPlugin.Core.Language.LanguageChanged -= Language_LanguageChanged;
		}

		/// <summary>
		/// Generic link click handler for the about links
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var hyperLink = sender as Hyperlink;
			if (hyperLink != null)
			{
				try
				{
					Process.Start(hyperLink.NavigateUri.AbsoluteUri);
				}
				catch (Exception ex)
				{
					LOG.Error("Error opening link: ", ex);
				}
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.I:
					try
					{
						Process.Start(IniConfig.Current.IniLocation);
					}
					catch (Exception ex)
					{
						LOG.Error("Can't open ini file:", ex);
					}
					break;
				case Key.L:
					try
					{
						Process.Start(MainForm.LogFileLocation);
					}
					catch (Exception ex)
					{
						LOG.Error("Can't open log file:", ex);
					}
					break;
				case Key.Escape:
					Close();
					break;
				case Key.E:
					MessageBox.Show(Helpers.EnvironmentInfo.EnvironmentToString(true));
					break;
			}
		}

		private void Close_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}
