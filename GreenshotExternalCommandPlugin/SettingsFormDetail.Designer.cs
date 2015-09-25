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
namespace GreenshotExternalCommandPlugin {
	partial class SettingsFormDetail {
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
			this.buttonOk = new GreenshotPlugin.Controls.GreenshotButton();
			this.buttonCancel = new GreenshotPlugin.Controls.GreenshotButton();
			this.groupBox1 = new GreenshotPlugin.Controls.GreenshotGroupBox();
			this.label4 = new GreenshotPlugin.Controls.GreenshotLabel();
			this.buttonPathSelect = new System.Windows.Forms.Button();
			this.label3 = new GreenshotPlugin.Controls.GreenshotLabel();
			this.textBox_name = new System.Windows.Forms.TextBox();
			this.label2 = new GreenshotPlugin.Controls.GreenshotLabel();
			this.textBox_arguments = new System.Windows.Forms.TextBox();
			this.label1 = new GreenshotPlugin.Controls.GreenshotLabel();
			this.textBox_commandline = new System.Windows.Forms.TextBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonOk
			// 
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Enabled = false;
			this.buttonOk.LanguageKey = "OK";
			this.buttonOk.Location = new System.Drawing.Point(273, 140);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 10;
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.ButtonOkClick);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.LanguageKey = "CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(9, 140);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 11;
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.buttonPathSelect);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.textBox_name);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.textBox_arguments);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.textBox_commandline);
			this.groupBox1.LanguageKey = "settings_title";
			this.groupBox1.Location = new System.Drawing.Point(10, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(339, 122);
			this.groupBox1.TabIndex = 28;
			this.groupBox1.TabStop = false;
			// 
			// label4
			// 
			this.label4.LanguageKey = "externalcommand.label_information";
			this.label4.Location = new System.Drawing.Point(68, 98);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(225, 21);
			this.label4.TabIndex = 19;
			// 
			// buttonPathSelect
			// 
			this.buttonPathSelect.Location = new System.Drawing.Point(298, 47);
			this.buttonPathSelect.Name = "buttonPathSelect";
			this.buttonPathSelect.Size = new System.Drawing.Size(33, 23);
			this.buttonPathSelect.TabIndex = 3;
			this.buttonPathSelect.Text = "...";
			this.buttonPathSelect.UseVisualStyleBackColor = true;
			this.buttonPathSelect.Click += new System.EventHandler(this.Button3Click);
			// 
			// label3
			// 
			this.label3.LanguageKey = "externalcommand.label_name";
			this.label3.Location = new System.Drawing.Point(6, 26);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(56, 17);
			this.label3.TabIndex = 17;
			// 
			// textBox_name
			// 
			this.textBox_name.Location = new System.Drawing.Point(68, 23);
			this.textBox_name.Name = "textBox_name";
			this.textBox_name.Size = new System.Drawing.Size(225, 20);
			this.textBox_name.TabIndex = 1;
			this.textBox_name.TextChanged += new System.EventHandler(this.textBox_name_TextChanged);
			// 
			// label2
			// 
			this.label2.LanguageKey = "externalcommand.label_argument";
			this.label2.Location = new System.Drawing.Point(6, 78);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(56, 17);
			this.label2.TabIndex = 16;
			// 
			// textBox_arguments
			// 
			this.textBox_arguments.Location = new System.Drawing.Point(68, 75);
			this.textBox_arguments.Name = "textBox_arguments";
			this.textBox_arguments.Size = new System.Drawing.Size(225, 20);
			this.textBox_arguments.TabIndex = 4;
            this.textBox_arguments.TextChanged += new System.EventHandler(this.textBox_arguments_TextChanged);
			// 
			// label1
			// 
			this.label1.LanguageKey = "externalcommand.label_command";
			this.label1.Location = new System.Drawing.Point(6, 52);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(56, 17);
			this.label1.TabIndex = 15;
			// 
			// textBox_commandline
			// 
			this.textBox_commandline.Location = new System.Drawing.Point(68, 49);
			this.textBox_commandline.Name = "textBox_commandline";
			this.textBox_commandline.Size = new System.Drawing.Size(225, 20);
			this.textBox_commandline.TabIndex = 2;
			this.textBox_commandline.TextChanged += new System.EventHandler(this.textBox_commandline_TextChanged);
			// 
			// SettingsFormDetail
			// 
			this.AcceptButton = this.buttonOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(360, 172);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.LanguageKey = "externalcommand.settings_detail_title";
			this.Name = "SettingsFormDetail";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}
		private GreenshotPlugin.Controls.GreenshotLabel label1;
		private GreenshotPlugin.Controls.GreenshotLabel label2;
		private GreenshotPlugin.Controls.GreenshotLabel label3;
		private GreenshotPlugin.Controls.GreenshotLabel label4;
		private GreenshotPlugin.Controls.GreenshotGroupBox groupBox1;
		private GreenshotPlugin.Controls.GreenshotButton buttonCancel;
		private GreenshotPlugin.Controls.GreenshotButton buttonOk;
		private System.Windows.Forms.TextBox textBox_commandline;
		private System.Windows.Forms.TextBox textBox_arguments;
		private System.Windows.Forms.TextBox textBox_name;
		private System.Windows.Forms.Button buttonPathSelect;
	}
}
