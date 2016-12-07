//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Core.Configuration;

#endregion

namespace Greenshot.Forms
{
	/// <summary>
	///     Description of PrintOptionsDialog.
	/// </summary>
	public partial class PrintOptionsDialog : BaseForm
	{
		private static readonly IPrinterConfiguration PrinterConfiguration = IniConfig.Current.GetSubSection<IPrinterConfiguration>();
		public PrintOptionsDialog()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			checkbox_dontaskagain.Checked = false;
		}


		private void Button_okClick(object sender, EventArgs e)
		{
			// update config
			PrinterConfiguration.OutputPrintPromptOptions = !checkbox_dontaskagain.Checked;
			DialogResult = DialogResult.OK;
		}

		protected override void OnFieldsFilled()
		{
			// the color radio button is not actually bound to a setting, but checked when monochrome/grayscale are not checked
			if (!radioBtnGrayScale.Checked && !radioBtnMonochrome.Checked)
			{
				radioBtnColorPrint.Checked = true;
			}
		}
	}
}