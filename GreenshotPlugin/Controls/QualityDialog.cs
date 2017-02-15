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
using GreenshotPlugin.Core;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace GreenshotPlugin.Controls
{
	/// <summary>
	///     Description of JpegQualityDialog.
	/// </summary>
	public partial class QualityDialog : GreenshotForm
	{
		private static readonly CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		public QualityDialog(SurfaceOutputSettings outputSettings)
		{
			Settings = outputSettings;
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();

			checkBox_reduceColors.Checked = Settings.ReduceColors;
			trackBarJpegQuality.Enabled = OutputFormats.jpg.Equals(outputSettings.Format);
			trackBarJpegQuality.Value = Settings.JPGQuality;
			textBoxJpegQuality.Enabled = OutputFormats.jpg.Equals(outputSettings.Format);
			textBoxJpegQuality.Text = Settings.JPGQuality.ToString();
			ToFront = true;
		}

		public SurfaceOutputSettings Settings { get; set; }

		private void Button_okClick(object sender, EventArgs e)
		{
			Settings.JPGQuality = trackBarJpegQuality.Value;
			Settings.ReduceColors = checkBox_reduceColors.Checked;
			if (checkbox_dontaskagain.Checked)
			{
				conf.OutputFileJpegQuality = Settings.JPGQuality;
				conf.OutputFilePromptQuality = false;
				conf.OutputFileReduceColors = Settings.ReduceColors;
				IniConfig.Save();
			}
		}

		private void TrackBarJpegQualityScroll(object sender, EventArgs e)
		{
			textBoxJpegQuality.Text = trackBarJpegQuality.Value.ToString();
		}
	}
}