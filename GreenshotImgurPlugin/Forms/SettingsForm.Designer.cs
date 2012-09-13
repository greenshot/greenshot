/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
namespace GreenshotImgurPlugin {
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
			this.historyButton = new GreenshotPlugin.Controls.GreenshotButton();
			this.checkbox_anonymous_access = new GreenshotPlugin.Controls.GreenshotCheckBox();
			this.checkbox_usepagelink = new GreenshotPlugin.Controls.GreenshotCheckBox();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.LanguageKey = "imgur.OK";
			this.buttonOK.Location = new System.Drawing.Point(222, 88);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.ButtonOKClick);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.LanguageKey = "imgur.CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(303, 88);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.ButtonCancelClick);
			// 
			// combobox_uploadimageformat
			// 
			this.combobox_uploadimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_uploadimageformat.FormattingEnabled = true;
			this.combobox_uploadimageformat.Location = new System.Drawing.Point(102, 7);
			this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
			this.combobox_uploadimageformat.PropertyName = "UploadFormat";
			this.combobox_uploadimageformat.SectionName = "Imgur";
			this.combobox_uploadimageformat.Size = new System.Drawing.Size(276, 21);
			this.combobox_uploadimageformat.TabIndex = 8;
			// 
			// label_upload_format
			// 
			this.label_upload_format.LanguageKey = "imgur.label_upload_format";
			this.label_upload_format.Location = new System.Drawing.Point(12, 10);
			this.label_upload_format.Name = "label_upload_format";
			this.label_upload_format.Size = new System.Drawing.Size(84, 20);
			this.label_upload_format.TabIndex = 9;
			// 
			// historyButton
			// 
			this.historyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.historyButton.LanguageKey = "imgur.history";
			this.historyButton.Location = new System.Drawing.Point(13, 88);
			this.historyButton.Name = "historyButton";
			this.historyButton.Size = new System.Drawing.Size(75, 23);
			this.historyButton.TabIndex = 11;
			this.historyButton.UseVisualStyleBackColor = true;
			this.historyButton.Click += new System.EventHandler(this.ButtonHistoryClick);
			// 
			// checkbox_anonymous_access
			// 
			this.checkbox_anonymous_access.AutoSize = true;
			this.checkbox_anonymous_access.LanguageKey = "imgur.anonymous_access";
			this.checkbox_anonymous_access.Location = new System.Drawing.Point(15, 38);
			this.checkbox_anonymous_access.Name = "checkbox_anonymous_access";
			this.checkbox_anonymous_access.PropertyName = "AnonymousAccess";
			this.checkbox_anonymous_access.SectionName = "Imgur";
			this.checkbox_anonymous_access.Size = new System.Drawing.Size(139, 17);
			this.checkbox_anonymous_access.TabIndex = 13;
			this.checkbox_anonymous_access.UseVisualStyleBackColor = true;
			// 
			// checkbox_usepagelink
			// 
			this.checkbox_usepagelink.AutoSize = true;
			this.checkbox_usepagelink.LanguageKey = "imgur.use_page_link";
			this.checkbox_usepagelink.Location = new System.Drawing.Point(15, 57);
			this.checkbox_usepagelink.Name = "checkbox_usepagelink";
			this.checkbox_usepagelink.PropertyName = "UsePageLink";
			this.checkbox_usepagelink.SectionName = "Imgur";
			this.checkbox_usepagelink.Size = new System.Drawing.Size(251, 17);
			this.checkbox_usepagelink.TabIndex = 13;
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
		private GreenshotPlugin.Controls.GreenshotButton historyButton;
		private GreenshotPlugin.Controls.GreenshotComboBox combobox_uploadimageformat;
		private GreenshotPlugin.Controls.GreenshotLabel label_upload_format;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		private GreenshotPlugin.Controls.GreenshotButton buttonOK;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_anonymous_access;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkbox_usepagelink;
	}
}
