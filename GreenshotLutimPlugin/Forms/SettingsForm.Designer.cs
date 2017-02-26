/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
namespace GreenshotLutimPlugin {
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
            this.historyButton = new GreenshotPlugin.Controls.GreenshotButton();
            this.label_lutimurl = new GreenshotPlugin.Controls.GreenshotLabel();
            this.textbox_lutimurl = new GreenshotPlugin.Controls.GreenshotTextBox();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.LanguageKey = "lutim.OK";
            this.buttonOK.Location = new System.Drawing.Point(222, 73);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 10;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.LanguageKey = "lutim.CANCEL";
            this.buttonCancel.Location = new System.Drawing.Point(303, 73);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 11;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // combobox_uploadimageformat
            // 
            this.combobox_uploadimageformat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combobox_uploadimageformat.FormattingEnabled = true;
            this.combobox_uploadimageformat.Location = new System.Drawing.Point(168, 32);
            this.combobox_uploadimageformat.Name = "combobox_uploadimageformat";
            this.combobox_uploadimageformat.PropertyName = "UploadFormat";
            this.combobox_uploadimageformat.SectionName = "Lutim";
            this.combobox_uploadimageformat.Size = new System.Drawing.Size(210, 21);
            this.combobox_uploadimageformat.TabIndex = 1;
            // 
            // label_upload_format
            // 
            this.label_upload_format.LanguageKey = "lutim.label_upload_format";
            this.label_upload_format.Location = new System.Drawing.Point(12, 35);
            this.label_upload_format.Name = "label_upload_format";
            this.label_upload_format.Size = new System.Drawing.Size(150, 20);
            this.label_upload_format.TabIndex = 9;
            this.label_upload_format.Text = "Image format";
            // 
            // historyButton
            // 
            this.historyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.historyButton.LanguageKey = "lutim.history";
            this.historyButton.Location = new System.Drawing.Point(13, 73);
            this.historyButton.Name = "historyButton";
            this.historyButton.Size = new System.Drawing.Size(75, 23);
            this.historyButton.TabIndex = 20;
            this.historyButton.Text = "History";
            this.historyButton.UseVisualStyleBackColor = true;
            this.historyButton.Click += new System.EventHandler(this.ButtonHistoryClick);
            // 
            // label_lutimurl
            // 
            this.label_lutimurl.LanguageKey = "lutim.label_lutim_url";
            this.label_lutimurl.Location = new System.Drawing.Point(12, 9);
            this.label_lutimurl.Name = "label_lutimurl";
            this.label_lutimurl.Size = new System.Drawing.Size(150, 20);
            this.label_lutimurl.TabIndex = 21;
            this.label_lutimurl.Text = "Lutim URL";
            // 
            // textbox_lutimurl
            // 
            this.textbox_lutimurl.Location = new System.Drawing.Point(168, 6);
            this.textbox_lutimurl.Name = "textbox_lutimurl";
            this.textbox_lutimurl.PropertyName = "LutimUrl";
            this.textbox_lutimurl.SectionName = "Lutim";
            this.textbox_lutimurl.Size = new System.Drawing.Size(210, 20);
            this.textbox_lutimurl.TabIndex = 22;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(387, 108);
            this.Controls.Add(this.textbox_lutimurl);
            this.Controls.Add(this.label_lutimurl);
            this.Controls.Add(this.historyButton);
            this.Controls.Add(this.label_upload_format);
            this.Controls.Add(this.combobox_uploadimageformat);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.LanguageKey = "lutim.settings_title";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "Lutim settings";
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		private GreenshotPlugin.Controls.GreenshotButton historyButton;
		private GreenshotPlugin.Controls.GreenshotComboBox combobox_uploadimageformat;
		private GreenshotPlugin.Controls.GreenshotLabel label_upload_format;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		private GreenshotPlugin.Controls.GreenshotButton buttonOK;
        private GreenshotPlugin.Controls.GreenshotLabel label_lutimurl;
        private GreenshotPlugin.Controls.GreenshotTextBox textbox_lutimurl;
    }
}
