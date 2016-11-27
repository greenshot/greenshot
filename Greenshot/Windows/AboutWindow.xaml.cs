//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Log;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Core.Extensions;
using Greenshot.Helpers;
using Greenshot.Legacy;

#endregion

namespace Greenshot.Windows
{
	/// <summary>
	///     Logic for the About.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		private static readonly object LockObject = new object();
		private static AboutWindow _aboutWindow;

		public AboutWindow()
		{
			Icon = GreenshotResources.GetGreenshotIcon().ToBitmapSource();
			InitializeComponent();
			Closing += Window_Closing;
			Version v = Assembly.GetExecutingAssembly().GetName().Version;
			VersionLabel.Content = "Greenshot " + v.Major + "." + v.Minor + "." + v.Build + " Build " + v.Revision + (PortableHelper.IsPortable ? " Portable" : "") + (" (" + OSInfo.Bits) + " bit)";

			language.PropertyChanged += SetTranslations;
			SetTranslations();
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		public static void Create()
		{
			lock (LockObject)
			{
				if (_aboutWindow == null)
				{
					_aboutWindow = new AboutWindow();
					ElementHost.EnableModelessKeyboardInterop(_aboutWindow);
					_aboutWindow.Show();
				}
			}
		}

		/// <summary>
		///     Generic link click handler for the about links
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
					Log.Error().WriteLine(ex, "Error opening link: ");
				}
			}
		}

		private void SetTranslations(object sender = null, PropertyChangedEventArgs args = null)
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
		///     Make sure the reference is gone
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closing(object sender, CancelEventArgs e)
		{
			_aboutWindow = null;
			language.PropertyChanged -= SetTranslations;
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
						Log.Error().WriteLine(ex, "Can't open ini file:");
					}
					break;
				case Key.L:
					try
					{
						Process.Start(GreenshotStart.LogFileLocation);
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex, "Can't open log file:");
					}
					break;
				case Key.Escape:
					Close();
					break;
				case Key.E:
					MessageBox.Show(EnvironmentInfo.EnvironmentToString(true));
					break;
			}
		}
	}
}