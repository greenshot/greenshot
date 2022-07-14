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
using System.Diagnostics;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Configuration;

namespace Greenshot.Forms
{
    public partial class BugReportForm : BaseForm
    {
        private BugReportForm()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            ToFront = true;
        }

        public BugReportForm(string bugText) : this()
        {
            textBoxDescription.Text = bugText;
        }

        private void LinkLblBugsLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            openLink((LinkLabel)sender);
        }

        private void openLink(LinkLabel link)
        {
            try
            {
                link.LinkVisited = true;
                Process.Start(link.Text);
            }
            catch (Exception)
            {
                MessageBox.Show(Language.GetFormattedString(LangKey.error_openlink, link.Text), Language.GetString(LangKey.error));
            }
        }
    }
}