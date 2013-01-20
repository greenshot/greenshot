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
namespace GreenshotPlugin.Controls {
	partial class OAuthLoginForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
			this.addressTextBox = new System.Windows.Forms.TextBox();
			this.browser = new ExtendedWebBrowser();
			this.SuspendLayout();
			// 
			// addressTextBox
			// 
			this.addressTextBox.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.addressTextBox.Dock = System.Windows.Forms.DockStyle.Top;
			this.addressTextBox.Enabled = false;
			this.addressTextBox.Location = new System.Drawing.Point(0, 0);
			this.addressTextBox.Name = "addressTextBox";
			this.addressTextBox.Size = new System.Drawing.Size(595, 20);
			this.addressTextBox.TabIndex = 3;
			this.addressTextBox.TabStop = false;
			// 
			// browser
			// 
			this.browser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.browser.Location = new System.Drawing.Point(0, 20);
			this.browser.MinimumSize = new System.Drawing.Size(100, 100);
			this.browser.Name = "browser";
			this.browser.Size = new System.Drawing.Size(595, 295);
			this.browser.TabIndex = 4;
			// 
			// OAuthLoginForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(595, 315);
			this.Controls.Add(this.browser);
			this.Controls.Add(this.addressTextBox);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OAuthLoginForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox addressTextBox;
		private ExtendedWebBrowser browser;

	}
}
