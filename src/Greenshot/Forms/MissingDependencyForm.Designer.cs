/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using Greenshot.Base.Controls;

namespace Greenshot.Forms
{
    partial class MissingDependencyForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelInfo = new GreenshotLabel();
            this.labelAssemblyCaption = new GreenshotLabel();
            this.labelMissingAssembly = new System.Windows.Forms.Label();
            this.btnDownload = new GreenshotButton();
            this.btnClose = new GreenshotButton();
            this.linkDetails = new System.Windows.Forms.LinkLabel();
            this.panelDetails = new System.Windows.Forms.Panel();
            this.textBoxDetails = new System.Windows.Forms.TextBox();
            this.panelDetails.SuspendLayout();
            this.SuspendLayout();
            //
            // labelInfo
            //
            this.labelInfo.LanguageKey = "missing_dependency_info";
            this.labelInfo.Location = new System.Drawing.Point(12, 9);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(504, 80);
            this.labelInfo.TabIndex = 0;
            //
            // labelAssemblyCaption
            //
            this.labelAssemblyCaption.LanguageKey = "missing_dependency_assembly_label";
            this.labelAssemblyCaption.Location = new System.Drawing.Point(12, 97);
            this.labelAssemblyCaption.Name = "labelAssemblyCaption";
            this.labelAssemblyCaption.Size = new System.Drawing.Size(180, 20);
            this.labelAssemblyCaption.TabIndex = 1;
            //
            // labelMissingAssembly
            //
            this.labelMissingAssembly.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.labelMissingAssembly.Location = new System.Drawing.Point(196, 97);
            this.labelMissingAssembly.Name = "labelMissingAssembly";
            this.labelMissingAssembly.Size = new System.Drawing.Size(320, 20);
            this.labelMissingAssembly.TabIndex = 2;
            //
            // btnDownload
            //
            this.btnDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDownload.LanguageKey = "missing_dependency_download";
            this.btnDownload.Location = new System.Drawing.Point(12, 128);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(200, 26);
            this.btnDownload.TabIndex = 3;
            this.btnDownload.UseVisualStyleBackColor = true;
            this.btnDownload.Click += new System.EventHandler(this.BtnDownload_Click);
            //
            // btnClose
            //
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.LanguageKey = "bugreport_cancel";
            this.btnClose.Location = new System.Drawing.Point(389, 128);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(139, 26);
            this.btnClose.TabIndex = 4;
            this.btnClose.UseVisualStyleBackColor = true;
            //
            // linkDetails
            //
            this.linkDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.linkDetails.AutoSize = true;
            this.linkDetails.Location = new System.Drawing.Point(12, 164);
            this.linkDetails.Name = "linkDetails";
            this.linkDetails.TabIndex = 5;
            this.linkDetails.TabStop = true;
            this.linkDetails.Text = "Show technical details";
            this.linkDetails.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkDetails_LinkClicked);
            //
            // panelDetails
            //
            this.panelDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panelDetails.Controls.Add(this.textBoxDetails);
            this.panelDetails.Location = new System.Drawing.Point(12, 185);
            this.panelDetails.Name = "panelDetails";
            this.panelDetails.Size = new System.Drawing.Size(516, 180);
            this.panelDetails.TabIndex = 6;
            this.panelDetails.Visible = false;
            //
            // textBoxDetails
            //
            this.textBoxDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxDetails.Multiline = true;
            this.textBoxDetails.Name = "textBoxDetails";
            this.textBoxDetails.ReadOnly = true;
            this.textBoxDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxDetails.TabIndex = 0;
            //
            // MissingDependencyForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(540, 375);
            this.Controls.Add(this.panelDetails);
            this.Controls.Add(this.linkDetails);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.labelMissingAssembly);
            this.Controls.Add(this.labelAssemblyCaption);
            this.Controls.Add(this.labelInfo);
            this.LanguageKey = "missing_dependency_title";
            this.Name = "MissingDependencyForm";
            this.Text = "Missing dependency";
            this.panelDetails.ResumeLayout(false);
            this.panelDetails.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private GreenshotLabel labelInfo;
        private GreenshotLabel labelAssemblyCaption;
        private System.Windows.Forms.Label labelMissingAssembly;
        private GreenshotButton btnDownload;
        private GreenshotButton btnClose;
        private System.Windows.Forms.LinkLabel linkDetails;
        private System.Windows.Forms.Panel panelDetails;
        private System.Windows.Forms.TextBox textBoxDetails;
    }
}
