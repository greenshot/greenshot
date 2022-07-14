﻿/*
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
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Core;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// Description of PleaseWaitForm.
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
            Icon = GreenshotResources.GetGreenshotIcon();
            _shouldClose = false;
            Text = title;
            label_pleasewait.Text = text;
            FormClosing += PreventFormClose;
            timer_checkforclose.Start();
        }

        // Can be used instead of ShowDialog
        public new void Show()
        {
            base.Show();
            bool positioned = false;
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.Bounds.Contains(Cursor.Position))
                {
                    positioned = true;
                    Location = new Point(screen.Bounds.X + (screen.Bounds.Width / 2) - (Width / 2), screen.Bounds.Y + (screen.Bounds.Height / 2) - (Height / 2));
                    break;
                }
            }

            if (!positioned)
            {
                Location = new Point(Cursor.Position.X - (Width / 2), Cursor.Position.Y - (Height / 2));
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