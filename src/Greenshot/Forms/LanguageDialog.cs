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
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Dapplo.Config.Language;
using Dapplo.Log;
using Greenshot.Addons.Resources;

namespace Greenshot.Forms
{
	/// <summary>
	///     The LanguageDialog askes the user for the language to use when none is selected.
	/// </summary>
	public partial class LanguageDialog : Form
	{
		private static readonly LogSource Log = new LogSource();
        private readonly LanguageContainer _languageContainer;
        private bool _properOkPressed;

		public LanguageDialog(GreenshotResources greenshotResources, LanguageContainer languageContainer)
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			Icon = greenshotResources.GetGreenshotIcon();
			Load += FormLoad;
			FormClosing += PreventFormClose;
            _languageContainer = languageContainer;
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
            comboBoxLanguage.DataSource = _languageContainer.AvailableLanguages.ToList();

            var preselectedLanguage = _languageContainer.CurrentLanguage ?? Thread.CurrentThread.CurrentUICulture.Name;
            if (_languageContainer.AvailableLanguages.ContainsKey(preselectedLanguage))
            {
                Log.Debug().WriteLine("Selecting {0}", preselectedLanguage);
                comboBoxLanguage.SelectedValue = preselectedLanguage;
            }
            
			// Close again when there is only one language, this shows the form briefly!
			// But the use-case is not so interesting, only happens once, to invest a lot of time here.
		    if (_languageContainer.AvailableLanguages.Count != 1)
		    {
		        return;
		    }

            comboBoxLanguage.SelectedValue = _languageContainer.AvailableLanguages.Keys.First();
		    _ = _languageContainer.ChangeLanguageAsync(SelectedLanguage);
		    _properOkPressed = true;
		    Close();
		}

		private void BtnOkClick(object sender, EventArgs e)
		{
			_properOkPressed = true;
            _ = _languageContainer.ChangeLanguageAsync(SelectedLanguage);
			Close();
		}
	}
}