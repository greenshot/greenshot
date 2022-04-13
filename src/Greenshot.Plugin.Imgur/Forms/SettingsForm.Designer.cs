﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Plugin.Imgur.Forms {
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
			this.historyButton = new GreenshotButton();
			this.checkbox_anonymous_access = new GreenshotCheckBox();
			this.checkbox_usepagelink = new GreenshotCheckBox();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.LanguageKey = "imgur.OK";
			this.buttonOK.Location = new System.Drawing.Point(222, 88);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 10;
			this.buttonOK.UseVisualStyleBackColor = true;
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.LanguageKey = "imgur.CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(303, 88);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 11;
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// combobox_uploadimageformat
			// 
			this.combobox_uploadimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_uploadimageformat.FormattingEnabled = true;
			this.combobox_uploadimageformat.Location = new System.Drawing.Point(168, 7);
			this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
			this.combobox_uploadimageformat.PropertyName = nameof(ImgurConfiguration.UploadFormat);
			this.combobox_uploadimageformat.SectionName = "Imgur";
			this.combobox_uploadimageformat.Size = new System.Drawing.Size(210, 21);
			this.combobox_uploadimageformat.TabIndex = 1;
			// 
			// label_upload_format
			// 
			this.label_upload_format.LanguageKey = "imgur.label_upload_format";
			this.label_upload_format.Location = new System.Drawing.Point(12, 10);
			this.label_upload_format.Name = "label_upload_format";
			this.label_upload_format.Size = new System.Drawing.Size(150, 20);
			this.label_upload_format.TabIndex = 9;
			// 
			// historyButton
			// 
			this.historyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.historyButton.LanguageKey = "imgur.history";
			this.historyButton.Location = new System.Drawing.Point(13, 88);
			this.historyButton.Name = "historyButton";
			this.historyButton.Size = new System.Drawing.Size(75, 23);
			this.historyButton.TabIndex = 20;
			this.historyButton.UseVisualStyleBackColor = true;
			this.historyButton.Click += new System.EventHandler(this.ButtonHistoryClick);
			// 
			// checkbox_anonymous_access
			// 
			this.checkbox_anonymous_access.LanguageKey = "imgur.anonymous_access";
			this.checkbox_anonymous_access.Location = new System.Drawing.Point(15, 38);
			this.checkbox_anonymous_access.Name = "checkbox_anonymous_access";
			this.checkbox_anonymous_access.PropertyName = nameof(ImgurConfiguration.AnonymousAccess);
			this.checkbox_anonymous_access.SectionName = "Imgur";
			this.checkbox_anonymous_access.Size = new System.Drawing.Size(139, 17);
			this.checkbox_anonymous_access.TabIndex = 2;
			this.checkbox_anonymous_access.UseVisualStyleBackColor = true;
			// 
			// checkbox_usepagelink
			// 
			this.checkbox_usepagelink.LanguageKey = "imgur.use_page_link";
			this.checkbox_usepagelink.Location = new System.Drawing.Point(15, 57);
			this.checkbox_usepagelink.Name = "checkbox_usepagelink";
			this.checkbox_usepagelink.PropertyName = nameof(ImgurConfiguration.UsePageLink);
			this.checkbox_usepagelink.SectionName = "Imgur";
			this.checkbox_usepagelink.Size = new System.Drawing.Size(251, 17);
			this.checkbox_usepagelink.TabIndex = 3;
			this.checkbox_usepagelink.UseVisualStyleBackColor = true;
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(387, 123);
			this.Controls.Add(this.checkbox_anonymous_access);
			this.Controls.Add(this.checkbox_usepagelink);
			this.Controls.Add(this.historyButton);
			this.Controls.Add(this.label_upload_format);
			this.Controls.Add(this.combobox_uploadimageformat);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.LanguageKey = "imgur.settings_title";
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SettingsForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		private GreenshotButton historyButton;
		private GreenshotComboBox combobox_uploadimageformat;
		private GreenshotLabel label_upload_format;
		private GreenshotButton buttonCancel;
		private GreenshotButton buttonOK;
		private GreenshotCheckBox checkbox_anonymous_access;
		private GreenshotCheckBox checkbox_usepagelink;
	}
}
