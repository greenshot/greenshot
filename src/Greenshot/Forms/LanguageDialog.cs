#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addons.Resources;

#endregion

namespace Greenshot.Forms
{
	/// <summary>
	///     Description of LanguageDialog.
	/// </summary>
	public partial class LanguageDialog : Form
	{
		private static readonly LogSource Log = new LogSource();
		private bool _properOkPressed;

		public LanguageDialog()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			Icon = GreenshotResources.Instance.GetGreenshotIcon();
			Load += FormLoad;
			FormClosing += PreventFormClose;
		}

		public string SelectedLanguage => comboBoxLanguage?.SelectedValue?.ToString();

	    private void PreventFormClose(object sender, FormClosingEventArgs e)
		{
			if (!_properOkPressed)
			{
				e.Cancel = true;
			}
		}

	    private void FormLoad(object sender, EventArgs e)
		{
			// Initialize the Language ComboBox
			comboBoxLanguage.DisplayMember = "Value";
			comboBoxLanguage.ValueMember = "Key";

            // Set datasource last to prevent problems
            // See: http://www.codeproject.com/KB/database/scomlistcontrolbinding.aspx?fid=111644
            // TODO: Get languages
            //comboBoxLanguage.DataSource = LanguageLoader.Current.AvailableLanguages.ToList();

            var currentLanguage = "en-US"; // LanguageLoader.Current.CurrentLanguage;

            if (currentLanguage != null)
			{
				Log.Debug().WriteLine("Selecting {0}", currentLanguage);
				comboBoxLanguage.SelectedValue = currentLanguage;
			}
			else
			{
				comboBoxLanguage.SelectedValue = Thread.CurrentThread.CurrentUICulture.Name;
			}

			// Close again when there is only one language, this shows the form briefly!
			// But the use-case is not so interesting, only happens once, to invest a lot of time here.
		    if (false) //LanguageLoader.Current.AvailableLanguages.Count != 1)
		    {
		        return;
		    }

            comboBoxLanguage.SelectedValue = "en-US"; // LanguageLoader.Current.AvailableLanguages.Keys.FirstOrDefault();
            // TODO: Change language
		    //var ignoreTask = LanguageLoader.Current.ChangeLanguageAsync(SelectedLanguage);
		    _properOkPressed = true;
		    Close();
		}

		private void BtnOKClick(object sender, EventArgs e)
		{
			_properOkPressed = true;
			// Fix for Bug #3431100 
            // TODO: Change language
			//Language.CurrentLanguage = SelectedLanguage;
			Close();
		}
	}
}