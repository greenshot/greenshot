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
using Greenshot.Configuration;
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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Greenshot.Windows
{
	/// <summary>
	/// Logic for the About.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		private static ILog LOG = LogManager.GetLogger(typeof(AboutWindow));
		private static object lockObject = new Object();
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
			Title = GreenshotPlugin.Core.Language.GetString(LangKey.about_title);
			AboutBugs.Text = GreenshotPlugin.Core.Language.GetString(LangKey.about_bugs);
			AboutDonations.Text = GreenshotPlugin.Core.Language.GetString(LangKey.about_donations);
			AboutIcons.Text = GreenshotPlugin.Core.Language.GetString(LangKey.about_icons);
			AboutLicense.Text = GreenshotPlugin.Core.Language.GetString(LangKey.about_license);
			if (GreenshotPlugin.Core.Language.HasKey(LangKey.about_translation)) {
				AboutTranslation.Text = GreenshotPlugin.Core.Language.GetString(LangKey.about_translation);
			}
			Version v = Assembly.GetExecutingAssembly().GetName().Version;
			VersionLabel.Content = "Greenshot " + v.Major + "." + v.Minor + "." + v.Build + " Build " + v.Revision + (PortableHelper.IsPortable ? " Portable" : "") + (" (" + Helpers.OSInfo.Bits + " bit)");

			// Create glimmer "Color-Cycle" animation

			var dots = new Ellipse[]{
				L1C1,L1C2, L1C3, L1C4, L1C5,
				L2C1, L2C2,
				L3C1,L3C2,
				L4C1, L4C2,
				L5C1,
				L6C1, L6C2, L6C3, L6C4,
				L7C1, L7C2,
				L8C1, L8C2, L8C3};

			int delayOffset = 15;
			int initialDelay = 5000;
			foreach (var ellipse in dots)
			{
				var storyBoard = new Storyboard();
				storyBoard.BeginTime = TimeSpan.FromMilliseconds(initialDelay);
				storyBoard.RepeatBehavior = RepeatBehavior.Forever;
				storyBoard.Duration = TimeSpan.FromSeconds(10);

				var goBright = new ColorAnimation();
				goBright.From = Color.FromArgb(0xff, 0x8a, 0xff, 0x00);
				goBright.To = Colors.White;
				goBright.Duration = TimeSpan.FromMilliseconds(400);
				storyBoard.Children.Add(goBright);
				var goNormal = new ColorAnimation();
				goNormal.To = Color.FromArgb(0xff, 0x8a, 0xff, 0x00);
				goNormal.From = Colors.White;
				goNormal.Duration = TimeSpan.FromMilliseconds(400);
				storyBoard.Children.Add(goNormal);
				Storyboard.SetTarget(goBright, ellipse);
				Storyboard.SetTargetProperty(goBright, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));
				Storyboard.SetTarget(goNormal, ellipse);
				Storyboard.SetTargetProperty(goNormal, new PropertyPath("(Shape.Fill).(SolidColorBrush.Color)"));
				ellipse.BeginStoryboard(storyBoard);
				initialDelay += delayOffset;
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
		}

		/// <summary>
		/// Generic link click handler for the about links
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			var hyperLink = sender as Hyperlink;
			try
			{
				Process.Start(hyperLink.NavigateUri.AbsoluteUri);
			}
			catch (Exception ex)
			{
				LOG.Error("Error opening link: ", ex);
			}
		}

		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.I:
					try
					{
						Process.Start(IniConfig.Get("Greenshot", "greenshot").IniLocation);
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
					DialogResult = true;
					break;
				case Key.E:
					MessageBox.Show(Helpers.EnvironmentInfo.EnvironmentToString(true));
					break;
			}
		}
	}
}
