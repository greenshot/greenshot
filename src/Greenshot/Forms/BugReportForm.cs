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
using System.Diagnostics;
using System.Windows.Forms;
using Greenshot.Addons;
using Greenshot.Addons.Controls;

namespace Greenshot.Forms
{
    /// <summary>
    /// 
    /// </summary>
	public partial class BugReportForm : GreenshotForm
	{
	    private readonly IGreenshotLanguage _greenshotLanguage;

	    private BugReportForm(IGreenshotLanguage greenshotLanguage) : base(greenshotLanguage)
		{
		    _greenshotLanguage = greenshotLanguage;
		    //
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			ToFront = true;
		}

		public BugReportForm(string bugText, IGreenshotLanguage greenshotLanguage) : this(greenshotLanguage)
		{
			textBoxDescription.Text = bugText;
		}

		private void LinkLblBugsLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			OpenLink((LinkLabel) sender);
		}

		private void OpenLink(LinkLabel link)
		{
			try
			{
				link.LinkVisited = true;
                var processStartInfo = new ProcessStartInfo(link.Text)
                {
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
			}
			catch (Exception)
			{
				MessageBox.Show(string.Format(_greenshotLanguage.ErrorOpenlink, link.Text), _greenshotLanguage.Error);
			}
		}
	}
}