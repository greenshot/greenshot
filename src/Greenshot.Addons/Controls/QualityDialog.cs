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
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Core.Enums;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	/// This is a dialog where the user can specify the quality of the written bitmap
	/// </summary>
	public partial class QualityDialog : GreenshotForm
	{
		private readonly ICoreConfiguration _coreConfiguration;

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="outputSettings">SurfaceOutputSettings</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        public QualityDialog(
		    SurfaceOutputSettings outputSettings,
            ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage) : base(greenshotLanguage)
		{
		    _coreConfiguration = coreConfiguration;
            Settings = outputSettings;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			checkBox_reduceColors.Checked = Settings.ReduceColors;
			trackBarJpegQuality.Enabled = OutputFormats.jpg.Equals(outputSettings.Format);
			trackBarJpegQuality.Value = Settings.JpgQuality;
			textBoxJpegQuality.Enabled = OutputFormats.jpg.Equals(outputSettings.Format);
			textBoxJpegQuality.Text = Settings.JpgQuality.ToString();
			ToFront = true;
		}

        /// <summary>
        /// This SurfaceOutputSettings which result from this form
        /// </summary>
        public SurfaceOutputSettings Settings { get; set; }

		private void Button_okClick(object sender, EventArgs e)
		{
			Settings.JpgQuality = trackBarJpegQuality.Value;
			Settings.ReduceColors = checkBox_reduceColors.Checked;
		    if (!checkbox_dontaskagain.Checked)
		    {
		        return;
		    }

		    _coreConfiguration.OutputFileJpegQuality = Settings.JpgQuality;
		    _coreConfiguration.OutputFilePromptQuality = false;
		    _coreConfiguration.OutputFileReduceColors = Settings.ReduceColors;
		}

		private void TrackBarJpegQualityScroll(object sender, EventArgs e)
		{
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString();
		}
	}
}