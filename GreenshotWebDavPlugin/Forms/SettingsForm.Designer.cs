/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
namespace GreenshotWebDavPlugin
{
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.buttonOK = new GreenshotPlugin.Controls.GreenshotButton();
            this.buttonCancel = new GreenshotPlugin.Controls.GreenshotButton();
            this.combobox_uploadimageformat = new GreenshotPlugin.Controls.GreenshotComboBox();
            this.label_upload_format = new GreenshotPlugin.Controls.GreenshotLabel();
            this.label_url = new GreenshotPlugin.Controls.GreenshotLabel();
            this.textbox_username = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.textbox_url = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.textbox_password = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.label_username = new GreenshotPlugin.Controls.GreenshotLabel();
            this.label_password = new GreenshotPlugin.Controls.GreenshotLabel();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.LanguageKey = "button_ok";
            this.buttonOK.Location = new System.Drawing.Point(270, 120);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 10;
            this.buttonOK.Text = "Ok";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.LanguageKey = "button_cancel";
            this.buttonCancel.Location = new System.Drawing.Point(351, 120);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 11;
            this.buttonCancel.Text = "Cancel";
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
            this.combobox_uploadimageformat.SectionName = "WebDAV";
            this.combobox_uploadimageformat.Size = new System.Drawing.Size(251, 21);
            this.combobox_uploadimageformat.TabIndex = 1;
            // 
            // label_upload_format
            // 
            this.label_upload_format.LanguageKey = "webdav.label_upload_format";
            this.label_upload_format.Location = new System.Drawing.Point(11, 9);
            this.label_upload_format.Name = "label_upload_format";
            this.label_upload_format.Size = new System.Drawing.Size(157, 20);
            this.label_upload_format.TabIndex = 3;
            this.label_upload_format.Text = "Image format";
            // 
            // label_url
            // 
            this.label_url.LanguageKey = "webdav.label_url";
            this.label_url.Location = new System.Drawing.Point(11, 36);
            this.label_url.Name = "label_url";
            this.label_url.Size = new System.Drawing.Size(157, 21);
            this.label_url.TabIndex = 7;
            this.label_url.Text = "URL";
            // 
            // textbox_username
            // 
            this.textbox_username.Location = new System.Drawing.Point(174, 60);
            this.textbox_username.Name = "textbox_username";
            this.textbox_username.PropertyName = "Username";
            this.textbox_username.SectionName = "WebDAV";
            this.textbox_username.Size = new System.Drawing.Size(251, 20);
            this.textbox_username.TabIndex = 12;
            // 
            // textbox_url
            // 
            this.textbox_url.Location = new System.Drawing.Point(174, 34);
            this.textbox_url.Name = "textbox_url";
            this.textbox_url.PropertyName = "Url";
            this.textbox_url.SectionName = "WebDAV";
            this.textbox_url.Size = new System.Drawing.Size(251, 20);
            this.textbox_url.TabIndex = 13;
            // 
            // textbox_password
            // 
            this.textbox_password.Location = new System.Drawing.Point(174, 87);
            this.textbox_password.Name = "textbox_password";
            this.textbox_password.PropertyName = "Password";
            this.textbox_password.SectionName = "WebDAV";
            this.textbox_password.Size = new System.Drawing.Size(251, 20);
            this.textbox_password.TabIndex = 14;
            // 
            // label_username
            // 
            this.label_username.AutoSize = true;
            this.label_username.LanguageKey = "webdav.label_username";
            this.label_username.Location = new System.Drawing.Point(11, 60);
            this.label_username.Name = "label_username";
            this.label_username.Size = new System.Drawing.Size(55, 13);
            this.label_username.TabIndex = 15;
            this.label_username.Text = "Username";
            // 
            // label_password
            // 
            this.label_password.AutoSize = true;
            this.label_password.LanguageKey = "webdav.label_password";
            this.label_password.Location = new System.Drawing.Point(14, 87);
            this.label_password.Name = "label_password";
            this.label_password.Size = new System.Drawing.Size(53, 13);
            this.label_password.TabIndex = 16;
            this.label_password.Text = "Password";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(432, 155);
            this.Controls.Add(this.label_password);
            this.Controls.Add(this.label_username);
            this.Controls.Add(this.textbox_password);
            this.Controls.Add(this.textbox_url);
            this.Controls.Add(this.textbox_username);
            this.Controls.Add(this.label_url);
            this.Controls.Add(this.label_upload_format);
            this.Controls.Add(this.combobox_uploadimageformat);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.LanguageKey = "webdav.settings_title";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "WebDAV settings";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		private GreenshotPlugin.Controls.GreenshotComboBox combobox_uploadimageformat;
		private GreenshotPlugin.Controls.GreenshotLabel label_upload_format;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		private GreenshotPlugin.Controls.GreenshotButton buttonOK;
        private GreenshotPlugin.Controls.GreenshotLabel label_url;
        private GreenshotPlugin.Controls.GreenshotTextBox textbox_username;
        private GreenshotPlugin.Controls.GreenshotTextBox textbox_url;
        private GreenshotPlugin.Controls.GreenshotTextBox textbox_password;
        private GreenshotPlugin.Controls.GreenshotLabel label_username;
        private GreenshotPlugin.Controls.GreenshotLabel label_password;
    }
}
