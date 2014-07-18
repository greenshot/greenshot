/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
namespace GreenshotDropboxPlugin {
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
		private void InitializeComponent() {
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
			this.buttonOK.Location = new System.Drawing.Point(267, 64);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 10;
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.LanguageKey = "CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(348, 64);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 11;
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// combobox_uploadimageformat
			// 
			this.combobox_uploadimageformat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.combobox_uploadimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_uploadimageformat.FormattingEnabled = true;
			this.combobox_uploadimageformat.Location = new System.Drawing.Point(116, 9);
			this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
			this.combobox_uploadimageformat.PropertyName = "UploadFormat";
			this.combobox_uploadimageformat.SectionName = "Dropbox";
			this.combobox_uploadimageformat.Size = new System.Drawing.Size(309, 21);
			this.combobox_uploadimageformat.TabIndex = 1;
			// 
			// label_upload_format
			// 
			this.label_upload_format.LanguageKey = "dropbox.label_upload_format";
			this.label_upload_format.Location = new System.Drawing.Point(11, 12);
			this.label_upload_format.Name = "label_upload_format";
			this.label_upload_format.Size = new System.Drawing.Size(84, 20);
			this.label_upload_format.TabIndex = 9;
			// 
			// label_AfterUpload
			// 
			this.label_AfterUpload.LanguageKey = "dropbox.label_AfterUpload";
			this.label_AfterUpload.Location = new System.Drawing.Point(10, 37);
			this.label_AfterUpload.Name = "label_AfterUpload";
			this.label_AfterUpload.Size = new System.Drawing.Size(84, 21);
			this.label_AfterUpload.TabIndex = 22;
			// 
			// checkboxAfterUploadLinkToClipBoard
			// 
			this.checkboxAfterUploadLinkToClipBoard.AutoSize = true;
			this.checkboxAfterUploadLinkToClipBoard.LanguageKey = "dropbox.label_AfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.Location = new System.Drawing.Point(116, 37);
			this.checkboxAfterUploadLinkToClipBoard.Name = "checkboxAfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.PropertyName = "AfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.SectionName = "Dropbox";
			this.checkboxAfterUploadLinkToClipBoard.Size = new System.Drawing.Size(104, 17);
			this.checkboxAfterUploadLinkToClipBoard.TabIndex = 2;
			this.checkboxAfterUploadLinkToClipBoard.UseVisualStyleBackColor = true;
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(432, 96);
			this.Controls.Add(this.checkboxAfterUploadLinkToClipBoard);
			this.Controls.Add(this.label_AfterUpload);
			this.Controls.Add(this.label_upload_format);
			this.Controls.Add(this.combobox_uploadimageformat);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.LanguageKey = "dropbox.settings_title";
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
