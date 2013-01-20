/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
namespace PluginExample
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
			this.checkBox1 = new GreenshotPlugin.Controls.GreenshotCheckBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.checkBox2 = new GreenshotPlugin.Controls.GreenshotCheckBox();
			this.SuspendLayout();
			// 
			// checkBox1
			// 
			this.checkBox1.Location = new System.Drawing.Point(12, 12);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.PropertyName = "AnnotationProcessor";
			this.checkBox1.SectionName = "PluginExample";
			this.checkBox1.Size = new System.Drawing.Size(152, 24);
			this.checkBox1.TabIndex = 0;
			this.checkBox1.Text = "Annotation processor";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 72);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 1;
			this.button1.Text = "Ok";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.Button1Click);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(100, 72);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 2;
			this.button2.Text = "Cancel";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.Button2Click);
			// 
			// checkBox2
			// 
			this.checkBox2.Location = new System.Drawing.Point(12, 42);
			this.checkBox2.Name = "checkBox2";
			this.checkBox2.PropertyName = "GreyScaleProcessor";
			this.checkBox2.SectionName = "PluginExample";
			this.checkBox2.Size = new System.Drawing.Size(152, 24);
			this.checkBox2.TabIndex = 3;
			this.checkBox2.Text = "Greyscale processor";
			this.checkBox2.UseVisualStyleBackColor = true;
			// 
			// SettingsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(200, 106);
			this.Controls.Add(this.checkBox2);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.checkBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "SettingsForm";
			this.Text = "Plugin example settings";
			this.ResumeLayout(false);

		}
		private GreenshotPlugin.Controls.GreenshotCheckBox checkBox2;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private GreenshotPlugin.Controls.GreenshotCheckBox checkBox1;
	}
}
