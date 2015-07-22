/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Threading.Tasks;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of PasswordRequestForm.
	/// </summary>
	public partial class SettingsForm : ImgurForm {
		private readonly ImgurConfiguration _config;
		public SettingsForm(ImgurConfiguration config) : base() {
			_config = config;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			CancelButton = buttonCancel;
			AcceptButton = buttonOK;

			Load += SettingsForm_Load;
		}

		private async void SettingsForm_Load(object sender, EventArgs e) {
			await ImgurUtils.LoadHistory().ConfigureAwait(false);

			if (_config.runtimeImgurHistory.Count > 0) {
				historyButton.Enabled = true;
			} else {
				historyButton.Enabled = false;
			}
		}
		
		async void ButtonHistoryClick(object sender, EventArgs e) {
			await ImgurHistory.ShowHistoryAsync().ConfigureAwait(false);
		}
	}
}
