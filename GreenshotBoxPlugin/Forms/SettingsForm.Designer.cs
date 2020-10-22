/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
namespace GreenshotBoxPlugin {
	partial class SettingsForm {
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
			this.buttonOK = new GreenshotPlugin.Controls.GreenshotButton();
			this.buttonCancel = new GreenshotPlugin.Controls.GreenshotButton();
			this.combobox_uploadimageformat = new GreenshotPlugin.Controls.GreenshotComboBox();
			this.label_upload_format = new GreenshotPlugin.Controls.GreenshotLabel();
			this.label_AfterUpload = new GreenshotPlugin.Controls.GreenshotLabel();
			this.checkboxAfterUploadLinkToClipBoard = new GreenshotPlugin.Controls.GreenshotCheckBox();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.LanguageKey = "OK";
			this.buttonOK.Location = new System.Drawing.Point(267, 72);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 12;
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.LanguageKey = "CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(348, 72);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 13;
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// combobox_uploadimageformat
			// 
			this.combobox_uploadimageformat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.combobox_uploadimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_uploadimageformat.FormattingEnabled = true;
			this.combobox_uploadimageformat.Location = new System.Drawing.Point(208, 12);
			this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
			this.combobox_uploadimageformat.PropertyName = "UploadFormat";
			this.combobox_uploadimageformat.SectionName = "Box";
			this.combobox_uploadimageformat.Size = new System.Drawing.Size(215, 21);
			this.combobox_uploadimageformat.TabIndex = 5;
			// 
			// label_upload_format
			// 
			this.label_upload_format.LanguageKey = "box.label_upload_format";
			this.label_upload_format.Location = new System.Drawing.Point(10, 12);
			this.label_upload_format.Name = "label_upload_format";
			this.label_upload_format.Size = new System.Drawing.Size(192, 20);
			this.label_upload_format.TabIndex = 4;
			// 
			// label_AfterUpload
			// 
			this.label_AfterUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label_AfterUpload.LanguageKey = "box.label_AfterUpload";
			this.label_AfterUpload.Location = new System.Drawing.Point(10, 46);
			this.label_AfterUpload.Name = "label_AfterUpload";
			this.label_AfterUpload.Size = new System.Drawing.Size(84, 21);
			this.label_AfterUpload.TabIndex = 8;
			// 
			// checkboxAfterUploadLinkToClipBoard
			// 
			this.checkboxAfterUploadLinkToClipBoard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkboxAfterUploadLinkToClipBoard.LanguageKey = "box.label_AfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.Location = new System.Drawing.Point(208, 45);
			this.checkboxAfterUploadLinkToClipBoard.Name = "checkboxAfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.PropertyName = "AfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.SectionName = "Box";
			this.checkboxAfterUploadLinkToClipBoard.Size = new System.Drawing.Size(104, 17);
			this.checkboxAfterUploadLinkToClipBoard.TabIndex = 10;
			this.checkboxAfterUploadLinkToClipBoard.UseVisualStyleBackColor = true;
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(432, 104);
			this.Controls.Add(this.checkboxAfterUploadLinkToClipBoard);
			this.Controls.Add(this.label_AfterUpload);
			this.Controls.Add(this.label_upload_format);
			this.Controls.Add(this.combobox_uploadimageformat);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.LanguageKey = "box.settings_title";
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SettingsForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		private GreenshotPlugin.Controls.GreenshotComboBox combobox_uploadimageformat;
		private GreenshotPlugin.Controls.GreenshotLabel label_upload_format;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		private GreenshotPlugin.Controls.GreenshotButton buttonOK;
		private GreenshotPlugin.Controls.GreenshotLabel label_AfterUpload;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkboxAfterUploadLinkToClipBoard;
	}
}
