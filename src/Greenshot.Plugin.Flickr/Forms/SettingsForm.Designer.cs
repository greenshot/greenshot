/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using Greenshot.Base.Controls;

namespace Greenshot.Plugin.Flickr.Forms {
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
			this.buttonOK = new GreenshotButton();
			this.buttonCancel = new GreenshotButton();
			this.combobox_uploadimageformat = new GreenshotComboBox();
			this.label_upload_format = new GreenshotLabel();
			this.checkBoxPublic = new GreenshotCheckBox();
			this.checkBoxFamily = new GreenshotCheckBox();
			this.checkBoxFriend = new GreenshotCheckBox();
			this.label_SafetyLevel = new GreenshotLabel();
			this.combobox_safetyLevel = new GreenshotComboBox();
			this.label_AfterUpload = new GreenshotLabel();
			this.checkboxAfterUploadLinkToClipBoard = new GreenshotCheckBox();
			this.checkBox_hiddenfromsearch = new GreenshotCheckBox();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.LanguageKey = "OK";
			this.buttonOK.Location = new System.Drawing.Point(270, 151);
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
			this.buttonCancel.Location = new System.Drawing.Point(351, 151);
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
			this.combobox_uploadimageformat.Location = new System.Drawing.Point(174, 6);
			this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
			this.combobox_uploadimageformat.PropertyName = "UploadFormat";
			this.combobox_uploadimageformat.SectionName = "Flickr";
			this.combobox_uploadimageformat.Size = new System.Drawing.Size(251, 21);
			this.combobox_uploadimageformat.TabIndex = 1;
			// 
			// label_upload_format
			// 
			this.label_upload_format.LanguageKey = "flickr.label_upload_format";
			this.label_upload_format.Location = new System.Drawing.Point(11, 9);
			this.label_upload_format.Name = "label_upload_format";
			this.label_upload_format.Size = new System.Drawing.Size(157, 20);
			this.label_upload_format.TabIndex = 3;
			// 
			// checkBoxPublic
			// 
			this.checkBoxPublic.LanguageKey = "flickr.public";
			this.checkBoxPublic.Location = new System.Drawing.Point(174, 88);
			this.checkBoxPublic.Name = "checkBoxPublic";
			this.checkBoxPublic.PropertyName = "flickrIsPublic";
			this.checkBoxPublic.SectionName = "Flickr";
			this.checkBoxPublic.Size = new System.Drawing.Size(55, 17);
			this.checkBoxPublic.TabIndex = 4;
			this.checkBoxPublic.UseVisualStyleBackColor = true;
			// 
			// checkBoxFamily
			// 
			this.checkBoxFamily.LanguageKey = "flickr.family";
			this.checkBoxFamily.Location = new System.Drawing.Point(265, 88);
			this.checkBoxFamily.Name = "checkBoxFamily";
			this.checkBoxFamily.PropertyName = "flickrIsFamily";
			this.checkBoxFamily.SectionName = "Flickr";
			this.checkBoxFamily.Size = new System.Drawing.Size(55, 17);
			this.checkBoxFamily.TabIndex = 5;
			this.checkBoxFamily.UseVisualStyleBackColor = true;
			// 
			// checkBoxFriend
			// 
			this.checkBoxFriend.LanguageKey = "flickr.friend";
			this.checkBoxFriend.Location = new System.Drawing.Point(350, 88);
			this.checkBoxFriend.Name = "checkBoxFriend";
			this.checkBoxFriend.PropertyName = "flickrIsFriend";
			this.checkBoxFriend.SectionName = "Flickr";
			this.checkBoxFriend.Size = new System.Drawing.Size(55, 17);
			this.checkBoxFriend.TabIndex = 6;
			this.checkBoxFriend.UseVisualStyleBackColor = true;
			// 
			// label_SafetyLevel
			// 
			this.label_SafetyLevel.LanguageKey = "flickr.label_SafetyLevel";
			this.label_SafetyLevel.Location = new System.Drawing.Point(11, 36);
			this.label_SafetyLevel.Name = "label_SafetyLevel";
			this.label_SafetyLevel.Size = new System.Drawing.Size(157, 21);
			this.label_SafetyLevel.TabIndex = 7;
			// 
			// combobox_safetyLevel
			// 
			this.combobox_safetyLevel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.combobox_safetyLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_safetyLevel.FormattingEnabled = true;
			this.combobox_safetyLevel.Location = new System.Drawing.Point(174, 33);
			this.combobox_safetyLevel.Name = "combobox_safetyLevel";
			this.combobox_safetyLevel.PropertyName = "SafetyLevel";
			this.combobox_safetyLevel.SectionName = "Flickr";
			this.combobox_safetyLevel.Size = new System.Drawing.Size(251, 21);
			this.combobox_safetyLevel.TabIndex = 2;
			// 
			// label_AfterUpload
			// 
			this.label_AfterUpload.LanguageKey = "flickr.label_AfterUpload";
			this.label_AfterUpload.Location = new System.Drawing.Point(12, 117);
			this.label_AfterUpload.Name = "label_AfterUpload";
			this.label_AfterUpload.Size = new System.Drawing.Size(155, 21);
			this.label_AfterUpload.TabIndex = 14;
			// 
			// checkboxAfterUploadLinkToClipBoard
			// 
			this.checkboxAfterUploadLinkToClipBoard.LanguageKey = "flickr.label_AfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.Location = new System.Drawing.Point(173, 116);
			this.checkboxAfterUploadLinkToClipBoard.Name = "checkboxAfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.PropertyName = "AfterUploadLinkToClipBoard";
			this.checkboxAfterUploadLinkToClipBoard.SectionName = "Flickr";
			this.checkboxAfterUploadLinkToClipBoard.Size = new System.Drawing.Size(104, 17);
			this.checkboxAfterUploadLinkToClipBoard.TabIndex = 7;
			this.checkboxAfterUploadLinkToClipBoard.UseVisualStyleBackColor = true;
			// 
			// checkBox_hiddenfromsearch
			// 
			this.checkBox_hiddenfromsearch.LanguageKey = "flickr.label_HiddenFromSearch";
			this.checkBox_hiddenfromsearch.Location = new System.Drawing.Point(174, 60);
			this.checkBox_hiddenfromsearch.Name = "checkBox_hiddenfromsearch";
			this.checkBox_hiddenfromsearch.PropertyName = "HiddenFromSearch";
			this.checkBox_hiddenfromsearch.SectionName = "Flickr";
			this.checkBox_hiddenfromsearch.Size = new System.Drawing.Size(118, 17);
			this.checkBox_hiddenfromsearch.TabIndex = 3;
			this.checkBox_hiddenfromsearch.UseVisualStyleBackColor = true;
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(432, 186);
			this.Controls.Add(this.checkBox_hiddenfromsearch);
			this.Controls.Add(this.checkboxAfterUploadLinkToClipBoard);
			this.Controls.Add(this.label_AfterUpload);
			this.Controls.Add(this.label_SafetyLevel);
			this.Controls.Add(this.combobox_safetyLevel);
			this.Controls.Add(this.checkBoxFriend);
			this.Controls.Add(this.checkBoxFamily);
			this.Controls.Add(this.checkBoxPublic);
			this.Controls.Add(this.label_upload_format);
			this.Controls.Add(this.combobox_uploadimageformat);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.LanguageKey = "flickr.settings_title";
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SettingsForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		private GreenshotCheckBox checkBox_hiddenfromsearch;
		private GreenshotComboBox combobox_uploadimageformat;
		private GreenshotLabel label_upload_format;
		private GreenshotButton buttonCancel;
		private GreenshotButton buttonOK;
        private GreenshotCheckBox checkBoxPublic;
        private GreenshotCheckBox checkBoxFamily;
        private GreenshotCheckBox checkBoxFriend;
        private GreenshotLabel label_SafetyLevel;
		private GreenshotComboBox combobox_safetyLevel;
        private GreenshotLabel label_AfterUpload;
        private GreenshotCheckBox checkboxAfterUploadLinkToClipBoard;
	}
}
