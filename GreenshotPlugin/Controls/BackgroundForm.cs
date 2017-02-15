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
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using GreenshotPlugin.Core;

#endregion

namespace GreenshotPlugin.Controls
{
	/// <summary>
	///     Description of PleaseWaitForm.
	/// </summary>
	public sealed partial class BackgroundForm : Form
	{
		private volatile bool _shouldClose;

		public BackgroundForm(string title, string text)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			Icon = GreenshotResources.getGreenshotIcon();
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

		// Can be used instead of ShowDialog
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