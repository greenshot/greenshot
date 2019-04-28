// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Addons.Resources;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	/// This form is used to show in the background
	/// </summary>
	public sealed partial class BackgroundForm : Form
	{
		private volatile bool _shouldClose;

        /// <summary>
        /// Constructor for the form
        /// </summary>
        /// <param name="title">string</param>
        /// <param name="text">string</param>
		public BackgroundForm(string title, string text)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			Icon = GreenshotResources.Instance.GetGreenshotIcon();
			_shouldClose = false;
			Text = title;
			label_pleasewait.Text = text;
			FormClosing += PreventFormClose;
			timer_checkforclose.Start();
		}

		private void BackgroundShowDialog()
		{
			ShowDialog();
		}

        /// <summary>
        /// Show this form an wait
        /// </summary>
        /// <param name="title">string</param>
        /// <param name="text">string</param>
        /// <returns>BackgroundForm</returns>
		public static BackgroundForm ShowAndWait(string title, string text)
		{
			var backgroundForm = new BackgroundForm(title, text);
			// Show form in background thread
			var backgroundTask = new Thread(backgroundForm.BackgroundShowDialog);
			backgroundForm.Name = "Background form";
			backgroundTask.IsBackground = true;
			backgroundTask.SetApartmentState(ApartmentState.STA);
			backgroundTask.Start();
			return backgroundForm;
		}

        /// <summary>
        /// Can be used instead of ShowDialog
        /// </summary>
        public new void Show()
		{
			base.Show();
			var positioned = false;
			foreach (var screen in Screen.AllScreens)
			{
				if (screen.Bounds.Contains(Cursor.Position))
				{
					positioned = true;
					Location = new Point(screen.Bounds.X + screen.Bounds.Width / 2 - Width / 2, screen.Bounds.Y + screen.Bounds.Height / 2 - Height / 2);
					break;
				}
			}
			if (!positioned)
			{
				Location = new Point(Cursor.Position.X - Width / 2, Cursor.Position.Y - Height / 2);
			}
		}

		private void PreventFormClose(object sender, FormClosingEventArgs e)
		{
			if (!_shouldClose)
			{
				e.Cancel = true;
			}
		}

		private void Timer_checkforcloseTick(object sender, EventArgs e)
		{
			if (_shouldClose)
			{
				timer_checkforclose.Stop();
				BeginInvoke(new EventHandler(delegate { Close(); }));
			}
		}

        /// <summary>
        /// Close the form
        /// </summary>
		public void CloseDialog()
		{
			_shouldClose = true;
			Application.DoEvents();
		}

		private void BackgroundFormFormClosing(object sender, FormClosingEventArgs e)
		{
			timer_checkforclose.Stop();
		}
	}
}