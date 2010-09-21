/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
	partial class LoginForm {
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
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.label_password = new System.Windows.Forms.Label();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.label_user = new System.Windows.Forms.Label();
			this.textBoxUser = new System.Windows.Forms.TextBox();
			this.label_url = new System.Windows.Forms.Label();
			this.textBoxUrl = new System.Windows.Forms.TextBox();
			this.checkBoxDoNotStorePassword = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// textBoxPassword
			// 
			this.textBoxPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxPassword.Location = new System.Drawing.Point(102, 73);
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.PasswordChar = '*';
			this.textBoxPassword.Size = new System.Drawing.Size(276, 20);
			this.textBoxPassword.TabIndex = 0;
			this.textBoxPassword.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBoxPasswordKeyUp);
			// 
			// label_password
			// 
			this.label_password.Location = new System.Drawing.Point(12, 73);
			this.label_password.Name = "label_password";
			this.label_password.Size = new System.Drawing.Size(84, 20);
			this.label_password.TabIndex = 1;
			this.label_password.Text = "Password";
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(222, 139);
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
			this.buttonCancel.Location = new System.Drawing.Point(303, 139);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.ButtonCancelClick);
			// 
			// label_user
			// 
			this.label_user.Location = new System.Drawing.Point(12, 47);
			this.label_user.Name = "label_user";
			this.label_user.Size = new System.Drawing.Size(84, 20);
			this.label_user.TabIndex = 5;
			this.label_user.Text = "User";
			// 
			// textBoxUser
			// 
			this.textBoxUser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxUser.Location = new System.Drawing.Point(102, 47);
			this.textBoxUser.Name = "textBoxUser";
			this.textBoxUser.Size = new System.Drawing.Size(276, 20);
			this.textBoxUser.TabIndex = 4;
			// 
			// label_url
			// 
			this.label_url.Location = new System.Drawing.Point(12, 21);
			this.label_url.Name = "label_url";
			this.label_url.Size = new System.Drawing.Size(84, 20);
			this.label_url.TabIndex = 7;
			this.label_url.Text = "Url";
			// 
			// textBoxUrl
			// 
			this.textBoxUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxUrl.Location = new System.Drawing.Point(102, 21);
			this.textBoxUrl.Name = "textBoxUrl";
			this.textBoxUrl.Size = new System.Drawing.Size(276, 20);
			this.textBoxUrl.TabIndex = 6;
			// 
			// checkBoxDoNotStorePassword
			// 
			this.checkBoxDoNotStorePassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxDoNotStorePassword.Checked = true;
			this.checkBoxDoNotStorePassword.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxDoNotStorePassword.Location = new System.Drawing.Point(102, 99);
			this.checkBoxDoNotStorePassword.Name = "checkBoxDoNotStorePassword";
			this.checkBoxDoNotStorePassword.Size = new System.Drawing.Size(276, 24);
			this.checkBoxDoNotStorePassword.TabIndex = 8;
			this.checkBoxDoNotStorePassword.Text = "Do not store the password";
			this.checkBoxDoNotStorePassword.UseVisualStyleBackColor = true;
			// 
			// LoginForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(387, 174);
			this.Controls.Add(this.checkBoxDoNotStorePassword);
			this.Controls.Add(this.label_url);
			this.Controls.Add(this.textBoxUrl);
			this.Controls.Add(this.label_user);
			this.Controls.Add(this.textBoxUser);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.label_password);
			this.Controls.Add(this.textBoxPassword);
			this.Name = "LoginForm";
			this.Text = "Please enter your Jira data";
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private System.Windows.Forms.CheckBox checkBoxDoNotStorePassword;
		private System.Windows.Forms.TextBox textBoxUrl;
		private System.Windows.Forms.Label label_url;
		private System.Windows.Forms.TextBox textBoxUser;
		private System.Windows.Forms.Label label_password;
		private System.Windows.Forms.Label label_user;
		private System.Windows.Forms.TextBox textBoxPassword;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
	}
}
