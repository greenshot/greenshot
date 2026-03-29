/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;
using Greenshot.Base.IniFile;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;

namespace Greenshot.Forms
{
    /// <summary>
    /// Description of PrintOptionsDialog.
    /// </summary>
    public partial class PrintOptionsDialog : GreenshotForm
    {
        public PrintOptionsDialog()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();
            InitializeLanguage();
            checkbox_dontaskagain.Checked = false;
        }

        protected override void InitializeLanguage()
        {
            checkbox_dontaskagain.Text = Language.GetString("printoptions_dontaskagain");
            checkboxAllowShrink.Text = Language.GetString("printoptions_allowshrink");
            checkboxAllowShrink.PropertyName = nameof(coreConfiguration.OutputPrintAllowShrink);
            checkboxAllowEnlarge.Text = Language.GetString("printoptions_allowenlarge");
            checkboxAllowEnlarge.PropertyName = nameof(coreConfiguration.OutputPrintAllowEnlarge);
            checkboxAllowCenter.Text = Language.GetString("printoptions_allowcenter");
            checkboxAllowCenter.PropertyName = nameof(coreConfiguration.OutputPrintCenter);
            checkboxAllowRotate.Text = Language.GetString("printoptions_allowrotate");
            checkboxAllowRotate.PropertyName = nameof(coreConfiguration.OutputPrintAllowRotate);
            button_ok.Text = Language.GetString("OK");
            checkboxDateTime.Text = Language.GetString("printoptions_timestamp");
            checkboxDateTime.PropertyName = nameof(coreConfiguration.OutputPrintFooter);
            button_cancel.Text = Language.GetString("CANCEL");
            checkboxPrintInverted.Text = Language.GetString("printoptions_inverted");
            checkboxPrintInverted.PropertyName = nameof(coreConfiguration.OutputPrintInverted);
            radioBtnGrayScale.Text = Language.GetString("printoptions_printgrayscale");
            radioBtnGrayScale.PropertyName = nameof(coreConfiguration.OutputPrintGrayscale);
            radioBtnMonochrome.Text = Language.GetString("printoptions_printmonochrome");
            radioBtnMonochrome.PropertyName = nameof(coreConfiguration.OutputPrintMonochrome);
            groupBoxPrintLayout.Text = Language.GetString("printoptions_layout");
            groupBoxColors.Text = Language.GetString("printoptions_colors");
            radioBtnColorPrint.Text = Language.GetString("printoptions_printcolor");
            Text = Language.GetString("printoptions_title");
        }


        private void Button_okClick(object sender, EventArgs e)
        {
            // update config
            coreConfiguration.OutputPrintPromptOptions = !checkbox_dontaskagain.Checked;
            IniConfig.Save();
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
