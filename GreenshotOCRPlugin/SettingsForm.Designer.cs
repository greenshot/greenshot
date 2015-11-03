/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
namespace GreenshotOcrPlugin
{
	partial class SettingsForm
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
			this.comboBox_languages = new System.Windows.Forms.ComboBox();
			this.checkBox_orientImage = new GreenshotPlugin.Controls.GreenshotCheckBox();
			this.checkBox_straightenImage = new GreenshotPlugin.Controls.GreenshotCheckBox();
			this.label_language = new GreenshotPlugin.Controls.GreenshotLabel();
			this.buttonOK = new GreenshotPlugin.Controls.GreenshotButton();
			this.buttonCancel = new GreenshotPlugin.Controls.GreenshotButton();
			this.SuspendLayout();
			// 
			// comboBox_languages
			// 
			this.comboBox_languages.FormattingEnabled = true;
			this.comboBox_languages.Items.AddRange(new object[] {
            "English",
            "Deutsch"});
			this.comboBox_languages.Location = new System.Drawing.Point(74, 12);
			this.comboBox_languages.Name = "comboBox_languages";
			this.comboBox_languages.Size = new System.Drawing.Size(153, 21);
			this.comboBox_languages.TabIndex = 1;
			// 
			// checkBox_orientImage
			// 
			this.checkBox_orientImage.LanguageKey = "ocr.orient_image";
			this.checkBox_orientImage.Location = new System.Drawing.Point(13, 68);
			this.checkBox_orientImage.Name = "checkBox_orientImage";
			this.checkBox_orientImage.PropertyName = "orientimage";
			this.checkBox_orientImage.SectionName = "OCR";
			this.checkBox_orientImage.Size = new System.Drawing.Size(104, 24);
			this.checkBox_orientImage.TabIndex = 3;
			this.checkBox_orientImage.UseVisualStyleBackColor = true;
			// 
			// checkBox_straightenImage
			// 
			this.checkBox_straightenImage.LanguageKey = "ocr.straighten_image";
			this.checkBox_straightenImage.Location = new System.Drawing.Point(13, 41);
			this.checkBox_straightenImage.Name = "checkBox_straightenImage";
			this.checkBox_straightenImage.PropertyName = "straightenImage";
			this.checkBox_straightenImage.SectionName = "OCR";
			this.checkBox_straightenImage.Size = new System.Drawing.Size(109, 24);
			this.checkBox_straightenImage.TabIndex = 2;
			this.checkBox_straightenImage.UseVisualStyleBackColor = true;
			// 
			// label_language
			// 
			this.label_language.LanguageKey = "ocr.language";
			this.label_language.Location = new System.Drawing.Point(13, 15);
			this.label_language.Name = "label_language";
			this.label_language.Size = new System.Drawing.Size(55, 23);
			this.label_language.TabIndex = 3;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.LanguageKey = "OK";
			this.buttonOK.Location = new System.Drawing.Point(12, 98);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(104, 23);
			this.buttonOK.TabIndex = 4;
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.ButtonOkClick);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.LanguageKey = "CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(128, 98);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(104, 23);
			this.buttonCancel.TabIndex = 5;
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(244, 135);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.label_language);
			this.Controls.Add(this.checkBox_straightenImage);
			this.Controls.Add(this.checkBox_orientImage);
			this.Controls.Add(this.comboBox_languages);
			this.LanguageKey = "settings_title";
			this.Name = "SettingsForm";
			this.ResumeLayout(false);

		}
		private GreenshotPlugin.Controls.GreenshotLabel label_language;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		private GreenshotPlugin.Controls.GreenshotButton buttonOK;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkBox_orientImage;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkBox_straightenImage;
		private System.Windows.Forms.ComboBox comboBox_languages;
	}
}
