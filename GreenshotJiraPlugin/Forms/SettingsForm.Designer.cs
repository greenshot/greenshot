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
namespace GreenshotJiraPlugin {
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
			this.label_url = new GreenshotPlugin.Controls.GreenshotLabel();
			this.textBoxUrl = new GreenshotPlugin.Controls.GreenshotTextBox();
			this.combobox_uploadimageformat = new GreenshotPlugin.Controls.GreenshotComboBox();
			this.label_upload_format = new GreenshotPlugin.Controls.GreenshotLabel();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.LanguageKey = "OK";
			this.buttonOK.Location = new System.Drawing.Point(222, 84);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.ButtonOKClick);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.LanguageKey = "CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(303, 84);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.ButtonCancelClick);
			// 
			// label_url
			// 
			this.label_url.LanguageKey = "label_url";
			this.label_url.Location = new System.Drawing.Point(12, 21);
			this.label_url.Name = "label_url";
			this.label_url.Size = new System.Drawing.Size(146, 20);
			this.label_url.TabIndex = 7;
			this.label_url.Text = "Url";
			// 
			// textBoxUrl
			// 
			this.textBoxUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxUrl.Location = new System.Drawing.Point(164, 21);
			this.textBoxUrl.Name = "textBoxUrl";
			this.textBoxUrl.PropertyName = "Url";
			this.textBoxUrl.SectionName = "Jira";
			this.textBoxUrl.Size = new System.Drawing.Size(214, 20);
			this.textBoxUrl.TabIndex = 6;
			// 
			// combobox_uploadimageformat
			// 
			this.combobox_uploadimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.combobox_uploadimageformat.FormattingEnabled = true;
			this.combobox_uploadimageformat.Location = new System.Drawing.Point(164, 47);
			this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
			this.combobox_uploadimageformat.PropertyName = "UploadFormat";
			this.combobox_uploadimageformat.SectionName = "Jira";
			this.combobox_uploadimageformat.Size = new System.Drawing.Size(214, 21);
			this.combobox_uploadimageformat.TabIndex = 8;
			// 
			// label_upload_format
			// 
			this.label_upload_format.LanguageKey = "label_upload_format";
			this.label_upload_format.Location = new System.Drawing.Point(12, 50);
			this.label_upload_format.Name = "label_upload_format";
			this.label_upload_format.Size = new System.Drawing.Size(146, 20);
			this.label_upload_format.TabIndex = 9;
			this.label_upload_format.Text = "Upload format";
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(387, 115);
			this.Controls.Add(this.label_upload_format);
			this.Controls.Add(this.combobox_uploadimageformat);
			this.Controls.Add(this.label_url);
			this.Controls.Add(this.textBoxUrl);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.LanguageKey = "settings_title";
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SettingsForm";
			this.Text = "Please enter your Jira data";
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private GreenshotPlugin.Controls.GreenshotComboBox combobox_uploadimageformat;
		private GreenshotPlugin.Controls.GreenshotLabel label_upload_format;
		private GreenshotPlugin.Controls.GreenshotTextBox textBoxUrl;
		private GreenshotPlugin.Controls.GreenshotLabel label_url;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		private GreenshotPlugin.Controls.GreenshotButton buttonOK;
	}
}
