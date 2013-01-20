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
namespace GreenshotImgurPlugin
{
	partial class ImgurHistory
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
			this.listview_imgur_uploads = new System.Windows.Forms.ListView();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.deleteButton = new System.Windows.Forms.Button();
			this.openButton = new System.Windows.Forms.Button();
			this.finishedButton = new System.Windows.Forms.Button();
			this.clipboardButton = new System.Windows.Forms.Button();
			this.clearHistoryButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// listview_imgur_uploads
			// 
			this.listview_imgur_uploads.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
									| System.Windows.Forms.AnchorStyles.Left) 
									| System.Windows.Forms.AnchorStyles.Right)));
			this.listview_imgur_uploads.FullRowSelect = true;
			this.listview_imgur_uploads.Location = new System.Drawing.Point(12, 12);
			this.listview_imgur_uploads.Name = "listview_imgur_uploads";
			this.listview_imgur_uploads.Size = new System.Drawing.Size(510, 249);
			this.listview_imgur_uploads.TabIndex = 0;
			this.listview_imgur_uploads.UseCompatibleStateImageBehavior = false;
			this.listview_imgur_uploads.View = System.Windows.Forms.View.Details;
			this.listview_imgur_uploads.SelectedIndexChanged += new System.EventHandler(this.Listview_imgur_uploadsSelectedIndexChanged);
			this.listview_imgur_uploads.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listview_imgur_uploads_ColumnClick);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.pictureBox1.InitialImage = null;
			this.pictureBox1.Location = new System.Drawing.Point(13, 272);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(90, 90);
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			// 
			// deleteButton
			// 
			this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.deleteButton.AutoSize = true;
			this.deleteButton.Location = new System.Drawing.Point(109, 272);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(75, 23);
			this.deleteButton.TabIndex = 2;
			this.deleteButton.Text = "Delete";
			this.deleteButton.UseVisualStyleBackColor = true;
			this.deleteButton.Click += new System.EventHandler(this.DeleteButtonClick);
			// 
			// openButton
			// 
			this.openButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.openButton.AutoSize = true;
			this.openButton.Location = new System.Drawing.Point(109, 305);
			this.openButton.Name = "openButton";
			this.openButton.Size = new System.Drawing.Size(75, 23);
			this.openButton.TabIndex = 3;
			this.openButton.Text = "Open";
			this.openButton.UseVisualStyleBackColor = true;
			this.openButton.Click += new System.EventHandler(this.OpenButtonClick);
			// 
			// finishedButton
			// 
			this.finishedButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.finishedButton.Location = new System.Drawing.Point(447, 338);
			this.finishedButton.Name = "finishedButton";
			this.finishedButton.Size = new System.Drawing.Size(75, 23);
			this.finishedButton.TabIndex = 4;
			this.finishedButton.Text = "Finished";
			this.finishedButton.UseVisualStyleBackColor = true;
			this.finishedButton.Click += new System.EventHandler(this.FinishedButtonClick);
			// 
			// clipboardButton
			// 
			this.clipboardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.clipboardButton.AutoSize = true;
			this.clipboardButton.Location = new System.Drawing.Point(109, 338);
			this.clipboardButton.Name = "clipboardButton";
			this.clipboardButton.Size = new System.Drawing.Size(129, 23);
			this.clipboardButton.TabIndex = 5;
			this.clipboardButton.Text = "Copy link(s) to clipboard";
			this.clipboardButton.UseVisualStyleBackColor = true;
			this.clipboardButton.Click += new System.EventHandler(this.ClipboardButtonClick);
			// 
			// clearHistoryButton
			// 
			this.clearHistoryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.clearHistoryButton.Location = new System.Drawing.Point(447, 272);
			this.clearHistoryButton.Name = "clearHistoryButton";
			this.clearHistoryButton.Size = new System.Drawing.Size(75, 23);
			this.clearHistoryButton.TabIndex = 6;
			this.clearHistoryButton.Text = "Clear history";
			this.clearHistoryButton.UseVisualStyleBackColor = true;
			this.clearHistoryButton.Click += new System.EventHandler(this.ClearHistoryButtonClick);
			// 
			// ImgurHistory
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(534, 372);
			this.Controls.Add(this.clearHistoryButton);
			this.Controls.Add(this.clipboardButton);
			this.Controls.Add(this.finishedButton);
			this.Controls.Add(this.openButton);
			this.Controls.Add(this.deleteButton);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.listview_imgur_uploads);
			this.Name = "ImgurHistory";
			this.LanguageKey = "imgur.history";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImgurHistoryFormClosing);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private System.Windows.Forms.Button clearHistoryButton;
		private System.Windows.Forms.Button clipboardButton;
		private System.Windows.Forms.Button finishedButton;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Button openButton;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.ListView listview_imgur_uploads;
	}
}
