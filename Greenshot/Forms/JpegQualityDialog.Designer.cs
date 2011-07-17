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
namespace Greenshot {
	partial class JpegQualityDialog : System.Windows.Forms.Form {
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
			this.label_choosejpegquality = new System.Windows.Forms.Label();
			this.textBoxJpegQuality = new System.Windows.Forms.TextBox();
			this.trackBarJpegQuality = new System.Windows.Forms.TrackBar();
			this.checkbox_dontaskagain = new System.Windows.Forms.CheckBox();
			this.button_ok = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.trackBarJpegQuality)).BeginInit();
			this.SuspendLayout();
			// 
			// label_choosejpegquality
			// 
			this.label_choosejpegquality.Location = new System.Drawing.Point(12, 9);
			this.label_choosejpegquality.Name = "label_choosejpegquality";
			this.label_choosejpegquality.Size = new System.Drawing.Size(268, 32);
			this.label_choosejpegquality.TabIndex = 15;
			this.label_choosejpegquality.Text = "Choose JPEG Quality";
			// 
			// textBoxJpegQuality
			// 
			this.textBoxJpegQuality.Location = new System.Drawing.Point(245, 44);
			this.textBoxJpegQuality.Name = "textBoxJpegQuality";
			this.textBoxJpegQuality.ReadOnly = true;
			this.textBoxJpegQuality.Size = new System.Drawing.Size(35, 20);
			this.textBoxJpegQuality.TabIndex = 16;
			this.textBoxJpegQuality.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// trackBarJpegQuality
			// 
			this.trackBarJpegQuality.LargeChange = 10;
			this.trackBarJpegQuality.Location = new System.Drawing.Point(12, 44);
			this.trackBarJpegQuality.Maximum = 100;
			this.trackBarJpegQuality.Name = "trackBarJpegQuality";
			this.trackBarJpegQuality.Size = new System.Drawing.Size(233, 45);
			this.trackBarJpegQuality.TabIndex = 14;
			this.trackBarJpegQuality.TickFrequency = 10;
			this.trackBarJpegQuality.Scroll += new System.EventHandler(this.TrackBarJpegQualityScroll);
			// 
			// checkbox_dontaskagain
			// 
			this.checkbox_dontaskagain.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_dontaskagain.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_dontaskagain.Location = new System.Drawing.Point(12, 81);
			this.checkbox_dontaskagain.Name = "checkbox_dontaskagain";
			this.checkbox_dontaskagain.Size = new System.Drawing.Size(268, 39);
			this.checkbox_dontaskagain.TabIndex = 17;
			this.checkbox_dontaskagain.Text = "Save as default quality and do not ask again.";
			this.checkbox_dontaskagain.TextAlign = System.Drawing.ContentAlignment.TopLeft;
			this.checkbox_dontaskagain.UseVisualStyleBackColor = true;
			// 
			// button_ok
			// 
			this.button_ok.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button_ok.Location = new System.Drawing.Point(205, 126);
			this.button_ok.Name = "button_ok";
			this.button_ok.Size = new System.Drawing.Size(75, 23);
			this.button_ok.TabIndex = 18;
			this.button_ok.Text = "OK";
			this.button_ok.UseVisualStyleBackColor = true;
			this.button_ok.Click += new System.EventHandler(this.Button_okClick);
			// 
			// JpegQualityDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(299, 161);
			this.Controls.Add(this.button_ok);
			this.Controls.Add(this.checkbox_dontaskagain);
			this.Controls.Add(this.label_choosejpegquality);
			this.Controls.Add(this.textBoxJpegQuality);
			this.Controls.Add(this.trackBarJpegQuality);
			this.Icon = GreenshotPlugin.Core.GreenshotResources.getGreenshotIcon();
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "JpegQualityDialog";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "JpegQualityDialog";
			((System.ComponentModel.ISupportInitialize)(this.trackBarJpegQuality)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private System.Windows.Forms.Button button_ok;
		private System.Windows.Forms.CheckBox checkbox_dontaskagain;
		private System.Windows.Forms.TrackBar trackBarJpegQuality;
		private System.Windows.Forms.TextBox textBoxJpegQuality;
		private System.Windows.Forms.Label label_choosejpegquality;
	}
}
