/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Greenshot.Addons.Controls;

namespace Greenshot.Forms
{
	partial class PrintOptionsDialog
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.checkbox_dontaskagain = new GreenshotCheckBox();
			this.checkboxAllowShrink = new GreenshotCheckBox();
			this.checkboxAllowEnlarge = new GreenshotCheckBox();
			this.checkboxAllowCenter = new GreenshotCheckBox();
			this.checkboxAllowRotate = new GreenshotCheckBox();
			this.button_ok = new GreenshotButton();
			this.checkboxDateTime = new GreenshotCheckBox();
			this.button_cancel = new GreenshotButton();
			this.checkboxPrintInverted = new GreenshotCheckBox();
			this.radioBtnGrayScale = new GreenshotRadioButton();
			this.radioBtnMonochrome = new GreenshotRadioButton();
			this.groupBoxPrintLayout = new GreenshotGroupBox();
			this.groupBoxColors = new GreenshotGroupBox();
			this.radioBtnColorPrint = new GreenshotRadioButton();
			this.groupBoxPrintLayout.SuspendLayout();
			this.groupBoxColors.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkbox_dontaskagain
			// 
			this.checkbox_dontaskagain.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_dontaskagain.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_dontaskagain.LanguageKey = "printoptions_dontaskagain";
			this.checkbox_dontaskagain.Location = new System.Drawing.Point(25, 299);
			this.checkbox_dontaskagain.Name = "checkbox_dontaskagain";
			this.checkbox_dontaskagain.Size = new System.Drawing.Size(240, 17);
			this.checkbox_dontaskagain.TabIndex = 15;
			this.checkbox_dontaskagain.Text = "Save options as default and do not ask again";
			this.checkbox_dontaskagain.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_dontaskagain.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowShrink
			// 
			this.checkboxAllowShrink.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowShrink.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowShrink.LanguageKey = "printoptions_allowshrink";
			this.checkboxAllowShrink.Location = new System.Drawing.Point(13, 23);
			this.checkboxAllowShrink.Name = "checkboxAllowShrink";
			this.checkboxAllowShrink.PropertyName = "OutputPrintAllowShrink";
			this.checkboxAllowShrink.Size = new System.Drawing.Size(168, 17);
			this.checkboxAllowShrink.TabIndex = 2;
			this.checkboxAllowShrink.Text = "Shrink printout to fit paper size";
			this.checkboxAllowShrink.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowShrink.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowEnlarge
			// 
			this.checkboxAllowEnlarge.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowEnlarge.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowEnlarge.LanguageKey = "printoptions_allowenlarge";
			this.checkboxAllowEnlarge.Location = new System.Drawing.Point(13, 46);
			this.checkboxAllowEnlarge.Name = "checkboxAllowEnlarge";
			this.checkboxAllowEnlarge.PropertyName = "OutputPrintAllowEnlarge";
			this.checkboxAllowEnlarge.Size = new System.Drawing.Size(174, 17);
			this.checkboxAllowEnlarge.TabIndex = 3;
			this.checkboxAllowEnlarge.Text = "Enlarge printout to fit paper size";
			this.checkboxAllowEnlarge.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowEnlarge.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowCenter
			// 
			this.checkboxAllowCenter.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowCenter.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowCenter.LanguageKey = "printoptions_allowcenter";
			this.checkboxAllowCenter.Location = new System.Drawing.Point(13, 92);
			this.checkboxAllowCenter.Name = "checkboxAllowCenter";
			this.checkboxAllowCenter.PropertyName = "OutputPrintCenter";
			this.checkboxAllowCenter.Size = new System.Drawing.Size(137, 17);
			this.checkboxAllowCenter.TabIndex = 5;
			this.checkboxAllowCenter.Text = "Center printout on page";
			this.checkboxAllowCenter.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowCenter.UseVisualStyleBackColor = true;
			// 
			// checkboxAllowRotate
			// 
			this.checkboxAllowRotate.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowRotate.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowRotate.LanguageKey = "printoptions_allowrotate";
			this.checkboxAllowRotate.Location = new System.Drawing.Point(13, 69);
			this.checkboxAllowRotate.Name = "checkboxAllowRotate";
			this.checkboxAllowRotate.PropertyName = "OutputPrintAllowRotate";
			this.checkboxAllowRotate.Size = new System.Drawing.Size(187, 17);
			this.checkboxAllowRotate.TabIndex = 4;
			this.checkboxAllowRotate.Text = "Rotate printout to page orientation";
			this.checkboxAllowRotate.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxAllowRotate.UseVisualStyleBackColor = true;
			// 
			// button_ok
			// 
			this.button_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button_ok.LanguageKey = "OK";
			this.button_ok.Location = new System.Drawing.Point(187, 355);
			this.button_ok.Name = "button_ok";
			this.button_ok.Size = new System.Drawing.Size(75, 23);
			this.button_ok.TabIndex = 20;
			this.button_ok.Text = "Ok";
			this.button_ok.UseVisualStyleBackColor = true;
			this.button_ok.Click += new System.EventHandler(this.Button_okClick);
			// 
			// checkboxDateTime
			// 
			this.checkboxDateTime.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxDateTime.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxDateTime.LanguageKey = "printoptions_timestamp";
			this.checkboxDateTime.Location = new System.Drawing.Point(13, 115);
			this.checkboxDateTime.Name = "checkboxDateTime";
			this.checkboxDateTime.PropertyName = "OutputPrintFooter";
			this.checkboxDateTime.Size = new System.Drawing.Size(187, 17);
			this.checkboxDateTime.TabIndex = 6;
			this.checkboxDateTime.Text = "Print date / time at bottom of page";
			this.checkboxDateTime.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxDateTime.UseVisualStyleBackColor = true;
			// 
			// button_cancel
			// 
			this.button_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button_cancel.LanguageKey = "CANCEL";
			this.button_cancel.Location = new System.Drawing.Point(268, 355);
			this.button_cancel.Name = "button_cancel";
			this.button_cancel.Size = new System.Drawing.Size(75, 23);
			this.button_cancel.TabIndex = 21;
			this.button_cancel.Text = "Cancel";
			this.button_cancel.UseVisualStyleBackColor = true;
			// 
			// checkboxPrintInverted
			// 
			this.checkboxPrintInverted.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxPrintInverted.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxPrintInverted.LanguageKey = "printoptions_inverted";
			this.checkboxPrintInverted.Location = new System.Drawing.Point(13, 88);
			this.checkboxPrintInverted.Name = "checkboxPrintInverted";
			this.checkboxPrintInverted.PropertyName = "OutputPrintInverted";
			this.checkboxPrintInverted.Size = new System.Drawing.Size(141, 17);
			this.checkboxPrintInverted.TabIndex = 14;
			this.checkboxPrintInverted.Text = "Print with inverted colors";
			this.checkboxPrintInverted.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkboxPrintInverted.UseVisualStyleBackColor = true;
			// 
			// radioBtnGrayScale
			// 
			this.radioBtnGrayScale.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnGrayScale.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnGrayScale.LanguageKey = "printoptions_printgrayscale";
			this.radioBtnGrayScale.Location = new System.Drawing.Point(13, 42);
			this.radioBtnGrayScale.Name = "radioBtnGrayScale";
			this.radioBtnGrayScale.PropertyName = "OutputPrintGrayscale";
			this.radioBtnGrayScale.Size = new System.Drawing.Size(137, 17);
			this.radioBtnGrayScale.TabIndex = 12;
			this.radioBtnGrayScale.Text = "Force grayscale printing";
			this.radioBtnGrayScale.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnGrayScale.UseVisualStyleBackColor = true;
			// 
			// radioBtnMonochrome
			// 
			this.radioBtnMonochrome.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnMonochrome.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnMonochrome.LanguageKey = "printoptions_printmonochrome";
			this.radioBtnMonochrome.Location = new System.Drawing.Point(13, 65);
			this.radioBtnMonochrome.Name = "radioBtnMonochrome";
			this.radioBtnMonochrome.PropertyName = "OutputPrintMonochrome";
			this.radioBtnMonochrome.Size = new System.Drawing.Size(148, 17);
			this.radioBtnMonochrome.TabIndex = 13;
			this.radioBtnMonochrome.Text = "Force black/white printing";
			this.radioBtnMonochrome.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnMonochrome.UseVisualStyleBackColor = true;
			// 
			// groupBoxPrintLayout
			// 
			this.groupBoxPrintLayout.Controls.Add(this.checkboxDateTime);
			this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowShrink);
			this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowEnlarge);
			this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowRotate);
			this.groupBoxPrintLayout.Controls.Add(this.checkboxAllowCenter);
			this.groupBoxPrintLayout.LanguageKey = "printoptions_layout";
			this.groupBoxPrintLayout.Location = new System.Drawing.Point(12, 12);
			this.groupBoxPrintLayout.Name = "groupBoxPrintLayout";
			this.groupBoxPrintLayout.Size = new System.Drawing.Size(331, 151);
			this.groupBoxPrintLayout.TabIndex = 31;
			this.groupBoxPrintLayout.TabStop = false;
			this.groupBoxPrintLayout.Text = "Page layout settings";
			// 
			// groupBoxColors
			// 
			this.groupBoxColors.Controls.Add(this.checkboxPrintInverted);
			this.groupBoxColors.Controls.Add(this.radioBtnColorPrint);
			this.groupBoxColors.Controls.Add(this.radioBtnGrayScale);
			this.groupBoxColors.Controls.Add(this.radioBtnMonochrome);
			this.groupBoxColors.LanguageKey = "printoptions_colors";
			this.groupBoxColors.Location = new System.Drawing.Point(12, 169);
			this.groupBoxColors.Name = "groupBoxColors";
			this.groupBoxColors.Size = new System.Drawing.Size(331, 127);
			this.groupBoxColors.TabIndex = 32;
			this.groupBoxColors.TabStop = false;
			this.groupBoxColors.Text = "Color settings";
			// 
			// radioBtnColorPrint
			// 
			this.radioBtnColorPrint.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnColorPrint.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnColorPrint.LanguageKey = "printoptions_printcolor";
			this.radioBtnColorPrint.Location = new System.Drawing.Point(13, 19);
			this.radioBtnColorPrint.Name = "radioBtnColorPrint";
			this.radioBtnColorPrint.PropertyName = "OutputPrintColor";
			this.radioBtnColorPrint.Size = new System.Drawing.Size(90, 17);
			this.radioBtnColorPrint.TabIndex = 11;
			this.radioBtnColorPrint.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.radioBtnColorPrint.UseVisualStyleBackColor = true;
            // 
            // PrintOptionsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(355, 390);
			this.Controls.Add(this.groupBoxColors);
			this.Controls.Add(this.groupBoxPrintLayout);
			this.Controls.Add(this.button_cancel);
			this.Controls.Add(this.button_ok);
			this.Controls.Add(this.checkbox_dontaskagain);
			this.LanguageKey = "printoptions_title";
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PrintOptionsDialog";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "Greenshot print options";
			this.groupBoxPrintLayout.ResumeLayout(false);
			this.groupBoxPrintLayout.PerformLayout();
			this.groupBoxColors.ResumeLayout(false);
			this.groupBoxColors.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		private GreenshotRadioButton radioBtnGrayScale;
		private GreenshotCheckBox checkboxPrintInverted;
		private GreenshotButton button_cancel;
		private GreenshotCheckBox checkboxDateTime;
		private GreenshotButton button_ok;
		private GreenshotCheckBox checkboxAllowRotate;
		private GreenshotCheckBox checkboxAllowCenter;
		private GreenshotCheckBox checkboxAllowEnlarge;
		private GreenshotCheckBox checkboxAllowShrink;
		private GreenshotCheckBox checkbox_dontaskagain;
        private GreenshotRadioButton radioBtnMonochrome;
        private GreenshotGroupBox groupBoxPrintLayout;
        private GreenshotGroupBox groupBoxColors;
        private GreenshotRadioButton radioBtnColorPrint;
	}
}
