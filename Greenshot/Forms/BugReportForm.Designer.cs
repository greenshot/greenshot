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
namespace Greenshot.Forms {
	partial class BugReportForm {
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
			this.labelBugReportInfo = new GreenshotPlugin.Controls.GreenshotLabel();
			this.textBoxDescription = new System.Windows.Forms.TextBox();
			this.btnClose = new GreenshotPlugin.Controls.GreenshotButton();
			this.linkLblBugs = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// labelBugReportInfo
			// 
			this.labelBugReportInfo.LanguageKey = "bugreport_info";
			this.labelBugReportInfo.Location = new System.Drawing.Point(12, 9);
			this.labelBugReportInfo.Name = "labelBugReportInfo";
			this.labelBugReportInfo.Size = new System.Drawing.Size(481, 141);
			this.labelBugReportInfo.TabIndex = 0;
			// 
			// textBoxDescription
			// 
			this.textBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxDescription.Location = new System.Drawing.Point(12, 179);
			this.textBoxDescription.Multiline = true;
			this.textBoxDescription.Name = "textBoxDescription";
			this.textBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textBoxDescription.Size = new System.Drawing.Size(504, 232);
			this.textBoxDescription.TabIndex = 1;
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnClose.LanguageKey = "bugreport_cancel";
			this.btnClose.Location = new System.Drawing.Point(377, 417);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(139, 23);
			this.btnClose.TabIndex = 2;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			// 
			// linkLblBugs
			// 
			this.linkLblBugs.Location = new System.Drawing.Point(12, 153);
			this.linkLblBugs.Name = "linkLblBugs";
			this.linkLblBugs.Size = new System.Drawing.Size(465, 23);
			this.linkLblBugs.TabIndex = 9;
			this.linkLblBugs.TabStop = true;
			this.linkLblBugs.Text = "http://getgreenshot.org/tickets/";
			this.linkLblBugs.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLblBugsLinkClicked);
			// 
			// BugReportForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(528, 452);
			this.Controls.Add(this.linkLblBugs);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.textBoxDescription);
			this.Controls.Add(this.labelBugReportInfo);
			this.LanguageKey = "bugreport_title";
			this.Name = "BugReportForm";
			this.Text = "Error";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		private System.Windows.Forms.LinkLabel linkLblBugs;
		private GreenshotPlugin.Controls.GreenshotButton btnClose;
		private System.Windows.Forms.TextBox textBoxDescription;
		private GreenshotPlugin.Controls.GreenshotLabel labelBugReportInfo;
	}
}
