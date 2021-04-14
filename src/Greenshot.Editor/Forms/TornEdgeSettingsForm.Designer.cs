/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Editor.Forms {
	partial class TornEdgeSettingsForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
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
			this.thickness = new System.Windows.Forms.NumericUpDown();
			this.offsetX = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.offsetY = new System.Windows.Forms.NumericUpDown();
			this.shadowDarkness = new System.Windows.Forms.TrackBar();
			this.buttonOK = new GreenshotButton();
			this.buttonCancel = new GreenshotButton();
			this.labelDarkness = new GreenshotLabel();
			this.labelOffset = new GreenshotLabel();
			this.labelThickness = new GreenshotLabel();
			this.toothsize = new System.Windows.Forms.NumericUpDown();
			this.label_toothsize = new GreenshotLabel();
			this.label_horizontaltoothrange = new GreenshotLabel();
			this.horizontaltoothrange = new System.Windows.Forms.NumericUpDown();
			this.labelVerticaltoothrange = new GreenshotLabel();
			this.verticaltoothrange = new System.Windows.Forms.NumericUpDown();
			this.top = new GreenshotCheckBox();
			this.right = new GreenshotCheckBox();
			this.bottom = new GreenshotCheckBox();
			this.left = new GreenshotCheckBox();
			this.shadowCheckbox = new GreenshotCheckBox();
			this.all = new GreenshotCheckBox();
			((System.ComponentModel.ISupportInitialize)(this.thickness)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.offsetX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.offsetY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.shadowDarkness)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.toothsize)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.horizontaltoothrange)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.verticaltoothrange)).BeginInit();
			this.SuspendLayout();
			// 
			// thickness
			// 
			this.thickness.Location = new System.Drawing.Point(173, 35);
			this.thickness.Maximum = new decimal(new int[] {
			20,
			0,
			0,
			0});
			this.thickness.Minimum = new decimal(new int[] {
			1,
			0,
			0,
			0});
			this.thickness.Name = "thickness";
			this.thickness.Size = new System.Drawing.Size(45, 20);
			this.thickness.TabIndex = 2;
			this.thickness.Value = new decimal(new int[] {
			9,
			0,
			0,
			0});
			// 
			// offsetX
			// 
			this.offsetX.Location = new System.Drawing.Point(102, 61);
			this.offsetX.Maximum = new decimal(new int[] {
			20,
			0,
			0,
			0});
			this.offsetX.Minimum = new decimal(new int[] {
			20,
			0,
			0,
			-2147483648});
			this.offsetX.Name = "offsetX";
			this.offsetX.Size = new System.Drawing.Size(45, 20);
			this.offsetX.TabIndex = 3;
			this.offsetX.Value = new decimal(new int[] {
			1,
			0,
			0,
			-2147483648});
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(153, 63);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(12, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "x";
			// 
			// offsetY
			// 
			this.offsetY.Location = new System.Drawing.Point(173, 61);
			this.offsetY.Maximum = new decimal(new int[] {
			20,
			0,
			0,
			0});
			this.offsetY.Minimum = new decimal(new int[] {
			20,
			0,
			0,
			-2147483648});
			this.offsetY.Name = "offsetY";
			this.offsetY.Size = new System.Drawing.Size(45, 20);
			this.offsetY.TabIndex = 4;
			this.offsetY.Value = new decimal(new int[] {
			1,
			0,
			0,
			-2147483648});
			// 
			// shadowDarkness
			// 
			this.shadowDarkness.Location = new System.Drawing.Point(102, 87);
			this.shadowDarkness.Maximum = 40;
			this.shadowDarkness.Minimum = 1;
			this.shadowDarkness.Name = "shadowDarkness";
			this.shadowDarkness.Size = new System.Drawing.Size(116, 45);
			this.shadowDarkness.TabIndex = 5;
			this.shadowDarkness.Value = 40;
			// 
			// buttonOK
			// 
			this.buttonOK.LanguageKey = "OK";
			this.buttonOK.Location = new System.Drawing.Point(334, 192);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 20;
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.ButtonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.LanguageKey = "CANCEL";
			this.buttonCancel.Location = new System.Drawing.Point(415, 192);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 21;
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// labelDarkness
			// 
			this.labelDarkness.LanguageKey = "editor_dropshadow_darkness";
			this.labelDarkness.Location = new System.Drawing.Point(12, 97);
			this.labelDarkness.Name = "labelDarkness";
			this.labelDarkness.Size = new System.Drawing.Size(92, 13);
			this.labelDarkness.TabIndex = 13;
			this.labelDarkness.Text = "Shadow darkness";
			// 
			// labelOffset
			// 
			this.labelOffset.LanguageKey = "editor_dropshadow_offset";
			this.labelOffset.Location = new System.Drawing.Point(12, 63);
			this.labelOffset.Name = "labelOffset";
			this.labelOffset.Size = new System.Drawing.Size(75, 13);
			this.labelOffset.TabIndex = 14;
			// 
			// labelThickness
			// 
			this.labelThickness.LanguageKey = "editor_dropshadow_thickness";
			this.labelThickness.Location = new System.Drawing.Point(12, 37);
			this.labelThickness.Name = "labelThickness";
			this.labelThickness.Size = new System.Drawing.Size(94, 13);
			this.labelThickness.TabIndex = 15;
			// 
			// toothsize
			// 
			this.toothsize.Location = new System.Drawing.Point(173, 138);
			this.toothsize.Maximum = new decimal(new int[] {
			40,
			0,
			0,
			0});
			this.toothsize.Minimum = new decimal(new int[] {
			2,
			0,
			0,
			0});
			this.toothsize.Name = "toothsize";
			this.toothsize.Size = new System.Drawing.Size(45, 20);
			this.toothsize.TabIndex = 6;
			this.toothsize.Value = new decimal(new int[] {
			12,
			0,
			0,
			0});
			// 
			// label_toothsize
			// 
			this.label_toothsize.LanguageKey = "editor_tornedge_toothsize";
			this.label_toothsize.Location = new System.Drawing.Point(12, 140);
			this.label_toothsize.Name = "label_toothsize";
			this.label_toothsize.Size = new System.Drawing.Size(56, 13);
			this.label_toothsize.TabIndex = 17;
			// 
			// label_horizontaltoothrange
			// 
			this.label_horizontaltoothrange.LanguageKey = "editor_tornedge_horizontaltoothrange";
			this.label_horizontaltoothrange.Location = new System.Drawing.Point(12, 166);
			this.label_horizontaltoothrange.Name = "label_horizontaltoothrange";
			this.label_horizontaltoothrange.Size = new System.Drawing.Size(111, 13);
			this.label_horizontaltoothrange.TabIndex = 19;			// 
			// horizontaltoothrange
			// 
			this.horizontaltoothrange.Location = new System.Drawing.Point(173, 164);
			this.horizontaltoothrange.Maximum = new decimal(new int[] {
			40,
			0,
			0,
			0});
			this.horizontaltoothrange.Minimum = new decimal(new int[] {
			2,
			0,
			0,
			0});
			this.horizontaltoothrange.Name = "horizontaltoothrange";
			this.horizontaltoothrange.Size = new System.Drawing.Size(45, 20);
			this.horizontaltoothrange.TabIndex = 7;
			this.horizontaltoothrange.Value = new decimal(new int[] {
			20,
			0,
			0,
			0});
			// 
			// labelVerticaltoothrange
			// 
			this.labelVerticaltoothrange.LanguageKey = "editor_tornedge_verticaltoothrange";
			this.labelVerticaltoothrange.Location = new System.Drawing.Point(12, 192);
			this.labelVerticaltoothrange.Name = "labelVerticaltoothrange";
			this.labelVerticaltoothrange.Size = new System.Drawing.Size(99, 13);
			this.labelVerticaltoothrange.TabIndex = 21;
			// 
			// verticaltoothrange
			// 
			this.verticaltoothrange.Location = new System.Drawing.Point(173, 190);
			this.verticaltoothrange.Maximum = new decimal(new int[] {
			40,
			0,
			0,
			0});
			this.verticaltoothrange.Minimum = new decimal(new int[] {
			2,
			0,
			0,
			0});
			this.verticaltoothrange.Name = "verticaltoothrange";
			this.verticaltoothrange.Size = new System.Drawing.Size(45, 20);
			this.verticaltoothrange.TabIndex = 8;
			this.verticaltoothrange.Value = new decimal(new int[] {
			20,
			0,
			0,
			0});
			// 
			// top
			// 
			this.top.CheckAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.top.LanguageKey = "editor_tornedge_top";
			this.top.Location = new System.Drawing.Point(263, 35);
			this.top.Name = "top";
			this.top.Size = new System.Drawing.Size(228, 33);
			this.top.TabIndex = 10;
			this.top.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
			this.top.UseVisualStyleBackColor = true;
			this.top.CheckedChanged += new System.EventHandler(this.AnySideCheckedChanged);
			// 
			// right
			// 
			this.right.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.right.LanguageKey = "editor_tornedge_right";
			this.right.Location = new System.Drawing.Point(393, 60);
			this.right.Name = "right";
			this.right.Size = new System.Drawing.Size(98, 49);
			this.right.TabIndex = 11;
			this.right.UseVisualStyleBackColor = true;
			this.right.CheckedChanged += new System.EventHandler(this.AnySideCheckedChanged);
			// 
			// bottom
			// 
			this.bottom.CheckAlign = System.Drawing.ContentAlignment.TopCenter;
			this.bottom.LanguageKey = "editor_tornedge_bottom";
			this.bottom.Location = new System.Drawing.Point(263, 98);
			this.bottom.Name = "bottom";
			this.bottom.Size = new System.Drawing.Size(228, 31);
			this.bottom.TabIndex = 12;
			this.bottom.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			this.bottom.UseVisualStyleBackColor = true;
			this.bottom.CheckedChanged += new System.EventHandler(this.AnySideCheckedChanged);
			// 
			// left
			// 
			this.left.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.left.LanguageKey = "editor_tornedge_left";
			this.left.Location = new System.Drawing.Point(243, 60);
			this.left.Name = "left";
			this.left.Size = new System.Drawing.Size(118, 49);
			this.left.TabIndex = 13;
			this.left.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.left.UseVisualStyleBackColor = true;
			this.left.CheckedChanged += new System.EventHandler(this.AnySideCheckedChanged);
			// 
			// shadowCheckbox
			// 
			this.shadowCheckbox.LanguageKey = "editor_tornedge_shadow";
			this.shadowCheckbox.Location = new System.Drawing.Point(12, 12);
			this.shadowCheckbox.Name = "shadowCheckbox";
			this.shadowCheckbox.Size = new System.Drawing.Size(110, 17);
			this.shadowCheckbox.TabIndex = 1;
			this.shadowCheckbox.UseVisualStyleBackColor = true;
			this.shadowCheckbox.CheckedChanged += new System.EventHandler(this.ShadowCheckbox_CheckedChanged);
			// 
			// all
			// 
			this.all.LanguageKey = "editor_tornedge_all";
			this.all.Location = new System.Drawing.Point(251, 12);
			this.all.Name = "all";
			this.all.Size = new System.Drawing.Size(88, 17);
			this.all.TabIndex = 9;
			this.all.UseVisualStyleBackColor = true;
			this.all.CheckedChanged += new System.EventHandler(this.all_CheckedChanged);
			// 
			// TornEdgeSettingsForm
			// 
			this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(502, 223);
			this.ControlBox = false;
			this.Controls.Add(this.all);
			this.Controls.Add(this.shadowCheckbox);
			this.Controls.Add(this.left);
			this.Controls.Add(this.bottom);
			this.Controls.Add(this.right);
			this.Controls.Add(this.top);
			this.Controls.Add(this.labelVerticaltoothrange);
			this.Controls.Add(this.verticaltoothrange);
			this.Controls.Add(this.label_horizontaltoothrange);
			this.Controls.Add(this.horizontaltoothrange);
			this.Controls.Add(this.label_toothsize);
			this.Controls.Add(this.toothsize);
			this.Controls.Add(this.labelThickness);
			this.Controls.Add(this.labelOffset);
			this.Controls.Add(this.labelDarkness);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.shadowDarkness);
			this.Controls.Add(this.offsetY);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.offsetX);
			this.Controls.Add(this.thickness);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.LanguageKey = "editor_tornedge_settings";
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TornEdgeSettingsForm";
			this.ShowIcon = false;
			this.Load += new System.EventHandler(this.TornEdgeSettingsForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.thickness)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.offsetX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.offsetY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.shadowDarkness)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.toothsize)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.horizontaltoothrange)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.verticaltoothrange)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.NumericUpDown thickness;
		private System.Windows.Forms.NumericUpDown offsetX;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown offsetY;
		private System.Windows.Forms.TrackBar shadowDarkness;
		private GreenshotButton buttonOK;
		private GreenshotButton buttonCancel;
		private GreenshotLabel labelDarkness;
		private GreenshotLabel labelOffset;
		private GreenshotLabel labelThickness;
		private System.Windows.Forms.NumericUpDown toothsize;
		private GreenshotLabel label_toothsize;
		private GreenshotLabel label_horizontaltoothrange;
		private System.Windows.Forms.NumericUpDown horizontaltoothrange;
		private GreenshotLabel labelVerticaltoothrange;
		private System.Windows.Forms.NumericUpDown verticaltoothrange;
		private GreenshotCheckBox top;
		private GreenshotCheckBox right;
		private GreenshotCheckBox bottom;
		private GreenshotCheckBox left;
		private GreenshotCheckBox shadowCheckbox;
        private GreenshotCheckBox all;
	}
}