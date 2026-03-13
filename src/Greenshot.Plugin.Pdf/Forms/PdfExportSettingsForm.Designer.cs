/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using Greenshot.Plugin.Pdf.Configuration;

namespace Greenshot.Plugin.Pdf.Forms
{
    partial class PdfExportSettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.labelDocumentWidth = new System.Windows.Forms.Label();
            this.numericDocumentWidth = new System.Windows.Forms.NumericUpDown();
            this.labelDocumentHeight = new System.Windows.Forms.Label();
            this.numericDocumentHeight = new System.Windows.Forms.NumericUpDown();
            this.groupBoxDocument = new System.Windows.Forms.GroupBox();
            this.checkBoxUseFixedDocument = new GreenshotCheckBox();
            this.groupBoxMargins = new System.Windows.Forms.GroupBox();
            this.labelMarginTop = new System.Windows.Forms.Label();
            this.numericMarginTop = new System.Windows.Forms.NumericUpDown();
            this.labelMarginBottom = new System.Windows.Forms.Label();
            this.numericMarginBottom = new System.Windows.Forms.NumericUpDown();
            this.labelMarginLeft = new System.Windows.Forms.Label();
            this.numericMarginLeft = new System.Windows.Forms.NumericUpDown();
            this.labelMarginRight = new System.Windows.Forms.Label();
            this.numericMarginRight = new System.Windows.Forms.NumericUpDown();
            this.buttonOK = new GreenshotButton();
            this.buttonCancel = new GreenshotButton();
            ((System.ComponentModel.ISupportInitialize)(this.numericDocumentWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDocumentHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginRight)).BeginInit();
            this.SuspendLayout();

            // labelDocumentWidth
            this.labelDocumentWidth.AutoSize = true;
            this.labelDocumentWidth.Location = new System.Drawing.Point(12, 22);
            this.labelDocumentWidth.Name = "labelDocumentWidth";
            this.labelDocumentWidth.Size = new System.Drawing.Size(112, 13);
            this.labelDocumentWidth.TabIndex = 0;
            this.labelDocumentWidth.Text = "Document Width (mm):";

            // numericDocumentWidth
            this.numericDocumentWidth.DecimalPlaces = 0;
            this.numericDocumentWidth.Location = new System.Drawing.Point(140, 19);
            this.numericDocumentWidth.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.numericDocumentWidth.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.numericDocumentWidth.Name = "numericDocumentWidth";
            this.numericDocumentWidth.Size = new System.Drawing.Size(80, 20);
            this.numericDocumentWidth.TabIndex = 1;
            this.numericDocumentWidth.Value = new decimal(new int[] { 210, 0, 0, 0 });

            // labelDocumentHeight
            this.labelDocumentHeight.AutoSize = true;
            this.labelDocumentHeight.Location = new System.Drawing.Point(12, 48);
            this.labelDocumentHeight.Name = "labelDocumentHeight";
            this.labelDocumentHeight.Size = new System.Drawing.Size(118, 13);
            this.labelDocumentHeight.TabIndex = 2;
            this.labelDocumentHeight.Text = "Document Height (mm):";

            // numericDocumentHeight
            this.numericDocumentHeight.DecimalPlaces = 0;
            this.numericDocumentHeight.Location = new System.Drawing.Point(140, 45);
            this.numericDocumentHeight.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.numericDocumentHeight.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.numericDocumentHeight.Name = "numericDocumentHeight";
            this.numericDocumentHeight.Size = new System.Drawing.Size(80, 20);
            this.numericDocumentHeight.TabIndex = 3;
            this.numericDocumentHeight.Value = new decimal(new int[] { 297, 0, 0, 0 });

            // checkBoxUseFixedDocument
            this.checkBoxUseFixedDocument.AutoSize = true;
            this.checkBoxUseFixedDocument.Location = new System.Drawing.Point(12, 74);
            this.checkBoxUseFixedDocument.Name = "checkBoxUseFixedDocument";
            this.checkBoxUseFixedDocument.PropertyName = nameof(PdfExportSettings.UseFixedDocument);
            this.checkBoxUseFixedDocument.SectionName = "Pdf";
            this.checkBoxUseFixedDocument.Size = new System.Drawing.Size(215, 17);
            this.checkBoxUseFixedDocument.TabIndex = 4;
            this.checkBoxUseFixedDocument.Text = "Use Fixed Document";
            this.checkBoxUseFixedDocument.UseVisualStyleBackColor = true;

            // groupBoxDocument
            this.groupBoxDocument.Controls.Add(this.labelDocumentWidth);
            this.groupBoxDocument.Controls.Add(this.numericDocumentWidth);
            this.groupBoxDocument.Controls.Add(this.labelDocumentHeight);
            this.groupBoxDocument.Controls.Add(this.numericDocumentHeight);
            this.groupBoxDocument.Controls.Add(this.checkBoxUseFixedDocument);
            this.groupBoxDocument.Location = new System.Drawing.Point(12, 12);
            this.groupBoxDocument.Name = "groupBoxDocument";
            this.groupBoxDocument.Size = new System.Drawing.Size(240, 110);
            this.groupBoxDocument.TabIndex = 0;
            this.groupBoxDocument.TabStop = false;
            this.groupBoxDocument.Text = "Document";

            // labelMarginTop
            this.labelMarginTop.AutoSize = true;
            this.labelMarginTop.Location = new System.Drawing.Point(12, 22);
            this.labelMarginTop.Name = "labelMarginTop";
            this.labelMarginTop.Size = new System.Drawing.Size(84, 13);
            this.labelMarginTop.TabIndex = 0;
            this.labelMarginTop.Text = "Top Margin (mm):";

            // numericMarginTop
            this.numericMarginTop.DecimalPlaces = 0;
            this.numericMarginTop.Location = new System.Drawing.Point(140, 19);
            this.numericMarginTop.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericMarginTop.Name = "numericMarginTop";
            this.numericMarginTop.Size = new System.Drawing.Size(80, 20);
            this.numericMarginTop.TabIndex = 1;
            this.numericMarginTop.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // labelMarginBottom
            this.labelMarginBottom.AutoSize = true;
            this.labelMarginBottom.Location = new System.Drawing.Point(12, 48);
            this.labelMarginBottom.Name = "labelMarginBottom";
            this.labelMarginBottom.Size = new System.Drawing.Size(98, 13);
            this.labelMarginBottom.TabIndex = 2;
            this.labelMarginBottom.Text = "Bottom Margin (mm):";

            // numericMarginBottom
            this.numericMarginBottom.DecimalPlaces = 0;
            this.numericMarginBottom.Location = new System.Drawing.Point(140, 45);
            this.numericMarginBottom.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericMarginBottom.Name = "numericMarginBottom";
            this.numericMarginBottom.Size = new System.Drawing.Size(80, 20);
            this.numericMarginBottom.TabIndex = 3;
            this.numericMarginBottom.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // labelMarginLeft
            this.labelMarginLeft.AutoSize = true;
            this.labelMarginLeft.Location = new System.Drawing.Point(12, 74);
            this.labelMarginLeft.Name = "labelMarginLeft";
            this.labelMarginLeft.Size = new System.Drawing.Size(89, 13);
            this.labelMarginLeft.TabIndex = 4;
            this.labelMarginLeft.Text = "Left Margin (mm):";

            // numericMarginLeft
            this.numericMarginLeft.DecimalPlaces = 0;
            this.numericMarginLeft.Location = new System.Drawing.Point(140, 71);
            this.numericMarginLeft.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericMarginLeft.Name = "numericMarginLeft";
            this.numericMarginLeft.Size = new System.Drawing.Size(80, 20);
            this.numericMarginLeft.TabIndex = 5;
            this.numericMarginLeft.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // labelMarginRight
            this.labelMarginRight.AutoSize = true;
            this.labelMarginRight.Location = new System.Drawing.Point(12, 100);
            this.labelMarginRight.Name = "labelMarginRight";
            this.labelMarginRight.Size = new System.Drawing.Size(96, 13);
            this.labelMarginRight.TabIndex = 6;
            this.labelMarginRight.Text = "Right Margin (mm):";

            // numericMarginRight
            this.numericMarginRight.DecimalPlaces = 0;
            this.numericMarginRight.Location = new System.Drawing.Point(140, 97);
            this.numericMarginRight.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numericMarginRight.Name = "numericMarginRight";
            this.numericMarginRight.Size = new System.Drawing.Size(80, 20);
            this.numericMarginRight.TabIndex = 7;
            this.numericMarginRight.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // groupBoxMargins
            this.groupBoxMargins.Controls.Add(this.labelMarginTop);
            this.groupBoxMargins.Controls.Add(this.numericMarginTop);
            this.groupBoxMargins.Controls.Add(this.labelMarginBottom);
            this.groupBoxMargins.Controls.Add(this.numericMarginBottom);
            this.groupBoxMargins.Controls.Add(this.labelMarginLeft);
            this.groupBoxMargins.Controls.Add(this.numericMarginLeft);
            this.groupBoxMargins.Controls.Add(this.labelMarginRight);
            this.groupBoxMargins.Controls.Add(this.numericMarginRight);
            this.groupBoxMargins.Location = new System.Drawing.Point(12, 128);
            this.groupBoxMargins.Name = "groupBoxMargins";
            this.groupBoxMargins.Size = new System.Drawing.Size(240, 135);
            this.groupBoxMargins.TabIndex = 1;
            this.groupBoxMargins.TabStop = false;
            this.groupBoxMargins.Text = "Margins";

            // buttonOK
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(12, 269);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(55, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;

            // buttonCancel
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(73, 269);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(55, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;

            // PdfExportSettingsForm
            this.AcceptButton = this.buttonOK;
            this.CancelButton = this.buttonCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(264, 304);
            this.Controls.Add(this.groupBoxDocument);
            this.Controls.Add(this.groupBoxMargins);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PdfExportSettingsForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PDF Export Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numericDocumentWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericDocumentHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericMarginRight)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label labelDocumentWidth;
        private System.Windows.Forms.NumericUpDown numericDocumentWidth;
        private System.Windows.Forms.Label labelDocumentHeight;
        private System.Windows.Forms.NumericUpDown numericDocumentHeight;
        private System.Windows.Forms.GroupBox groupBoxDocument;
        private System.Windows.Forms.GroupBox groupBoxMargins;
        private System.Windows.Forms.Label labelMarginTop;
        private System.Windows.Forms.NumericUpDown numericMarginTop;
        private System.Windows.Forms.Label labelMarginBottom;
        private System.Windows.Forms.NumericUpDown numericMarginBottom;
        private System.Windows.Forms.Label labelMarginLeft;
        private System.Windows.Forms.NumericUpDown numericMarginLeft;
        private System.Windows.Forms.Label labelMarginRight;
        private System.Windows.Forms.NumericUpDown numericMarginRight;
        private GreenshotCheckBox checkBoxUseFixedDocument;
        private GreenshotButton buttonOK;
        private GreenshotButton buttonCancel;
    }
}
