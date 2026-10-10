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

using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Controls;

namespace Greenshot.Plugin.CaptionBar
{
    partial class CaptionBarSettingsForm
    {
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.checkBoxEnabled = new GreenshotCheckBox();
            this.label_enabled = new GreenshotLabel();
            this.textBoxCustomText = new GreenshotTextBox();
            this.label_customtext = new GreenshotLabel();
            this.numericBarHeight = new NumericUpDown();
            this.label_barheight = new GreenshotLabel();
            this.textBoxFontName = new GreenshotTextBox();
            this.label_fontname = new GreenshotLabel();
            this.numericFontSize = new NumericUpDown();
            this.label_fontsize = new GreenshotLabel();
            this.buttonBackgroundColor = new GreenshotButton();
            this.label_backgroundcolor = new GreenshotLabel();
            this.panelBackgroundColorPreview = new Panel();
            this.buttonTextColor = new GreenshotButton();
            this.label_textcolor = new GreenshotLabel();
            this.panelTextColorPreview = new Panel();
            this.checkBoxShowTimestamp = new GreenshotCheckBox();
            this.label_showtimestamp = new GreenshotLabel();
            this.textBoxTimestampFormat = new GreenshotTextBox();
            this.label_timestampformat = new GreenshotLabel();
            this.comboBoxTimestampAlignment = new GreenshotComboBox();
            this.label_timestampalignment = new GreenshotLabel();
            this.comboBoxCustomTextAlignment = new GreenshotComboBox();
            this.label_customtextalignment = new GreenshotLabel();
            this.numericTextPadding = new NumericUpDown();
            this.label_textpadding = new GreenshotLabel();
            this.buttonOK = new GreenshotButton();
            this.buttonCancel = new GreenshotButton();
            ((System.ComponentModel.ISupportInitialize)(this.numericBarHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericFontSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTextPadding)).BeginInit();
            this.SuspendLayout();
            //
            // label_enabled
            //
            this.label_enabled.AutoSize = true;
            this.label_enabled.Location = new Point(20, 18);
            this.label_enabled.Name = "label_enabled";
            this.label_enabled.Size = new Size(170, 20);
            this.label_enabled.TabIndex = 0;
            this.label_enabled.Text = "Enable Caption Bar:";
            //
            // checkBoxEnabled
            //
            this.checkBoxEnabled.Location = new Point(200, 15);
            this.checkBoxEnabled.Name = "checkBoxEnabled";
            this.checkBoxEnabled.PropertyName = "Enabled";
            this.checkBoxEnabled.SectionName = "CaptionBar";
            this.checkBoxEnabled.Size = new Size(20, 20);
            this.checkBoxEnabled.TabIndex = 1;
            this.checkBoxEnabled.UseVisualStyleBackColor = true;
            //
            // label_customtext
            //
            this.label_customtext.AutoSize = true;
            this.label_customtext.Location = new Point(20, 48);
            this.label_customtext.Name = "label_customtext";
            this.label_customtext.Size = new Size(170, 20);
            this.label_customtext.TabIndex = 2;
            this.label_customtext.Text = "Custom Text (Name + User):";
            //
            // textBoxCustomText
            //
            this.textBoxCustomText.Location = new Point(200, 45);
            this.textBoxCustomText.Name = "textBoxCustomText";
            this.textBoxCustomText.PropertyName = "CustomText";
            this.textBoxCustomText.SectionName = "CaptionBar";
            this.textBoxCustomText.Size = new Size(280, 23);
            this.textBoxCustomText.TabIndex = 3;
            //
            // label_barheight
            //
            this.label_barheight.AutoSize = true;
            this.label_barheight.Location = new Point(20, 78);
            this.label_barheight.Name = "label_barheight";
            this.label_barheight.Size = new Size(170, 20);
            this.label_barheight.TabIndex = 4;
            this.label_barheight.Text = "Caption Bar Height (pixels):";
            //
            // numericBarHeight
            //
            this.numericBarHeight.Location = new Point(200, 75);
            this.numericBarHeight.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            this.numericBarHeight.Minimum = new decimal(new int[] { 20, 0, 0, 0 });
            this.numericBarHeight.Name = "numericBarHeight";
            this.numericBarHeight.Size = new Size(80, 23);
            this.numericBarHeight.TabIndex = 5;
            this.numericBarHeight.Value = new decimal(new int[] { 35, 0, 0, 0 });
            //
            // label_fontname
            //
            this.label_fontname.AutoSize = true;
            this.label_fontname.Location = new Point(20, 108);
            this.label_fontname.Name = "label_fontname";
            this.label_fontname.Size = new Size(170, 20);
            this.label_fontname.TabIndex = 6;
            this.label_fontname.Text = "Font Name:";
            //
            // textBoxFontName
            //
            this.textBoxFontName.Location = new Point(200, 105);
            this.textBoxFontName.Name = "textBoxFontName";
            this.textBoxFontName.PropertyName = "FontName";
            this.textBoxFontName.SectionName = "CaptionBar";
            this.textBoxFontName.Size = new Size(280, 23);
            this.textBoxFontName.TabIndex = 7;
            //
            // label_fontsize
            //
            this.label_fontsize.AutoSize = true;
            this.label_fontsize.Location = new Point(20, 138);
            this.label_fontsize.Name = "label_fontsize";
            this.label_fontsize.Size = new Size(170, 20);
            this.label_fontsize.TabIndex = 8;
            this.label_fontsize.Text = "Font Size (points):";
            //
            // numericFontSize
            //
            this.numericFontSize.Location = new Point(200, 135);
            this.numericFontSize.Maximum = new decimal(new int[] { 72, 0, 0, 0 });
            this.numericFontSize.Minimum = new decimal(new int[] { 6, 0, 0, 0 });
            this.numericFontSize.Name = "numericFontSize";
            this.numericFontSize.Size = new Size(80, 23);
            this.numericFontSize.TabIndex = 9;
            this.numericFontSize.Value = new decimal(new int[] { 13, 0, 0, 0 });
            //
            // label_backgroundcolor
            //
            this.label_backgroundcolor.AutoSize = true;
            this.label_backgroundcolor.Location = new Point(20, 168);
            this.label_backgroundcolor.Name = "label_backgroundcolor";
            this.label_backgroundcolor.Size = new Size(170, 20);
            this.label_backgroundcolor.TabIndex = 10;
            this.label_backgroundcolor.Text = "Background Color:";
            //
            // buttonBackgroundColor
            //
            this.buttonBackgroundColor.Location = new Point(200, 165);
            this.buttonBackgroundColor.Name = "buttonBackgroundColor";
            this.buttonBackgroundColor.Size = new Size(100, 25);
            this.buttonBackgroundColor.TabIndex = 11;
            this.buttonBackgroundColor.Text = "Choose Color";
            this.buttonBackgroundColor.UseVisualStyleBackColor = true;
            this.buttonBackgroundColor.Click += new System.EventHandler(this.ButtonBackgroundColor_Click);
            //
            // panelBackgroundColorPreview
            //
            this.panelBackgroundColorPreview.BorderStyle = BorderStyle.FixedSingle;
            this.panelBackgroundColorPreview.Location = new Point(310, 165);
            this.panelBackgroundColorPreview.Name = "panelBackgroundColorPreview";
            this.panelBackgroundColorPreview.Size = new Size(50, 25);
            this.panelBackgroundColorPreview.TabIndex = 12;
            //
            // label_textcolor
            //
            this.label_textcolor.AutoSize = true;
            this.label_textcolor.Location = new Point(20, 198);
            this.label_textcolor.Name = "label_textcolor";
            this.label_textcolor.Size = new Size(170, 20);
            this.label_textcolor.TabIndex = 13;
            this.label_textcolor.Text = "Text Color:";
            //
            // buttonTextColor
            //
            this.buttonTextColor.Location = new Point(200, 195);
            this.buttonTextColor.Name = "buttonTextColor";
            this.buttonTextColor.Size = new Size(100, 25);
            this.buttonTextColor.TabIndex = 14;
            this.buttonTextColor.Text = "Choose Color";
            this.buttonTextColor.UseVisualStyleBackColor = true;
            this.buttonTextColor.Click += new System.EventHandler(this.ButtonTextColor_Click);
            //
            // panelTextColorPreview
            //
            this.panelTextColorPreview.BorderStyle = BorderStyle.FixedSingle;
            this.panelTextColorPreview.Location = new Point(310, 195);
            this.panelTextColorPreview.Name = "panelTextColorPreview";
            this.panelTextColorPreview.Size = new Size(50, 25);
            this.panelTextColorPreview.TabIndex = 15;
            //
            // label_showtimestamp
            //
            this.label_showtimestamp.AutoSize = true;
            this.label_showtimestamp.Location = new Point(20, 228);
            this.label_showtimestamp.Name = "label_showtimestamp";
            this.label_showtimestamp.Size = new Size(170, 20);
            this.label_showtimestamp.TabIndex = 16;
            this.label_showtimestamp.Text = "Show Timestamp:";
            //
            // checkBoxShowTimestamp
            //
            this.checkBoxShowTimestamp.Location = new Point(200, 225);
            this.checkBoxShowTimestamp.Name = "checkBoxShowTimestamp";
            this.checkBoxShowTimestamp.PropertyName = "ShowTimestamp";
            this.checkBoxShowTimestamp.SectionName = "CaptionBar";
            this.checkBoxShowTimestamp.Size = new Size(20, 20);
            this.checkBoxShowTimestamp.TabIndex = 17;
            this.checkBoxShowTimestamp.UseVisualStyleBackColor = true;
            //
            // label_timestampformat
            //
            this.label_timestampformat.AutoSize = true;
            this.label_timestampformat.Location = new Point(20, 258);
            this.label_timestampformat.Name = "label_timestampformat";
            this.label_timestampformat.Size = new Size(170, 20);
            this.label_timestampformat.TabIndex = 18;
            this.label_timestampformat.Text = "Timestamp Format:";
            //
            // textBoxTimestampFormat
            //
            this.textBoxTimestampFormat.Location = new Point(200, 255);
            this.textBoxTimestampFormat.Name = "textBoxTimestampFormat";
            this.textBoxTimestampFormat.PropertyName = "TimestampFormat";
            this.textBoxTimestampFormat.SectionName = "CaptionBar";
            this.textBoxTimestampFormat.Size = new Size(280, 23);
            this.textBoxTimestampFormat.TabIndex = 19;
            //
            // label_timestampalignment
            //
            this.label_timestampalignment.AutoSize = true;
            this.label_timestampalignment.Location = new Point(20, 288);
            this.label_timestampalignment.Name = "label_timestampalignment";
            this.label_timestampalignment.Size = new Size(170, 20);
            this.label_timestampalignment.TabIndex = 20;
            this.label_timestampalignment.Text = "Timestamp Alignment:";
            //
            // comboBoxTimestampAlignment
            //
            this.comboBoxTimestampAlignment.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBoxTimestampAlignment.FormattingEnabled = true;
            this.comboBoxTimestampAlignment.Location = new Point(200, 285);
            this.comboBoxTimestampAlignment.Name = "comboBoxTimestampAlignment";
            this.comboBoxTimestampAlignment.PropertyName = "TimestampAlignment";
            this.comboBoxTimestampAlignment.SectionName = "CaptionBar";
            this.comboBoxTimestampAlignment.Size = new Size(200, 23);
            this.comboBoxTimestampAlignment.TabIndex = 21;
            //
            // label_customtextalignment
            //
            this.label_customtextalignment.AutoSize = true;
            this.label_customtextalignment.Location = new Point(20, 318);
            this.label_customtextalignment.Name = "label_customtextalignment";
            this.label_customtextalignment.Size = new Size(170, 20);
            this.label_customtextalignment.TabIndex = 22;
            this.label_customtextalignment.Text = "Custom Text Alignment:";
            //
            // comboBoxCustomTextAlignment
            //
            this.comboBoxCustomTextAlignment.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBoxCustomTextAlignment.FormattingEnabled = true;
            this.comboBoxCustomTextAlignment.Location = new Point(200, 315);
            this.comboBoxCustomTextAlignment.Name = "comboBoxCustomTextAlignment";
            this.comboBoxCustomTextAlignment.PropertyName = "CustomTextAlignment";
            this.comboBoxCustomTextAlignment.SectionName = "CaptionBar";
            this.comboBoxCustomTextAlignment.Size = new Size(200, 23);
            this.comboBoxCustomTextAlignment.TabIndex = 23;
            //
            // label_textpadding
            //
            this.label_textpadding.AutoSize = true;
            this.label_textpadding.Location = new Point(20, 348);
            this.label_textpadding.Name = "label_textpadding";
            this.label_textpadding.Size = new Size(170, 20);
            this.label_textpadding.TabIndex = 24;
            this.label_textpadding.Text = "Text Padding (pixels):";
            //
            // numericTextPadding
            //
            this.numericTextPadding.Location = new Point(200, 345);
            this.numericTextPadding.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            this.numericTextPadding.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numericTextPadding.Name = "numericTextPadding";
            this.numericTextPadding.Size = new Size(80, 23);
            this.numericTextPadding.TabIndex = 25;
            this.numericTextPadding.Value = new decimal(new int[] { 10, 0, 0, 0 });
            //
            // buttonOK
            //
            this.buttonOK.DialogResult = DialogResult.OK;
            this.buttonOK.Location = new Point(280, 385);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new Size(90, 30);
            this.buttonOK.TabIndex = 26;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = DialogResult.Cancel;
            this.buttonCancel.Location = new Point(380, 385);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new Size(90, 30);
            this.buttonCancel.TabIndex = 27;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            //
            // CaptionBarSettingsForm
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new Size(500, 435);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.numericTextPadding);
            this.Controls.Add(this.label_textpadding);
            this.Controls.Add(this.comboBoxCustomTextAlignment);
            this.Controls.Add(this.label_customtextalignment);
            this.Controls.Add(this.comboBoxTimestampAlignment);
            this.Controls.Add(this.label_timestampalignment);
            this.Controls.Add(this.textBoxTimestampFormat);
            this.Controls.Add(this.label_timestampformat);
            this.Controls.Add(this.checkBoxShowTimestamp);
            this.Controls.Add(this.label_showtimestamp);
            this.Controls.Add(this.panelTextColorPreview);
            this.Controls.Add(this.buttonTextColor);
            this.Controls.Add(this.label_textcolor);
            this.Controls.Add(this.panelBackgroundColorPreview);
            this.Controls.Add(this.buttonBackgroundColor);
            this.Controls.Add(this.label_backgroundcolor);
            this.Controls.Add(this.numericFontSize);
            this.Controls.Add(this.label_fontsize);
            this.Controls.Add(this.textBoxFontName);
            this.Controls.Add(this.label_fontname);
            this.Controls.Add(this.numericBarHeight);
            this.Controls.Add(this.label_barheight);
            this.Controls.Add(this.textBoxCustomText);
            this.Controls.Add(this.label_customtext);
            this.Controls.Add(this.checkBoxEnabled);
            this.Controls.Add(this.label_enabled);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CaptionBarSettingsForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "CaptionBar Plugin Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numericBarHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericFontSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericTextPadding)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private GreenshotCheckBox checkBoxEnabled;
        private GreenshotLabel label_enabled;
        private GreenshotTextBox textBoxCustomText;
        private GreenshotLabel label_customtext;
        private NumericUpDown numericBarHeight;
        private GreenshotLabel label_barheight;
        private GreenshotTextBox textBoxFontName;
        private GreenshotLabel label_fontname;
        private NumericUpDown numericFontSize;
        private GreenshotLabel label_fontsize;
        private GreenshotButton buttonBackgroundColor;
        private GreenshotLabel label_backgroundcolor;
        private Panel panelBackgroundColorPreview;
        private GreenshotButton buttonTextColor;
        private GreenshotLabel label_textcolor;
        private Panel panelTextColorPreview;
        private GreenshotCheckBox checkBoxShowTimestamp;
        private GreenshotLabel label_showtimestamp;
        private GreenshotTextBox textBoxTimestampFormat;
        private GreenshotLabel label_timestampformat;
        private GreenshotComboBox comboBoxTimestampAlignment;
        private GreenshotLabel label_timestampalignment;
        private GreenshotComboBox comboBoxCustomTextAlignment;
        private GreenshotLabel label_customtextalignment;
        private NumericUpDown numericTextPadding;
        private GreenshotLabel label_textpadding;
        private GreenshotButton buttonOK;
        private GreenshotButton buttonCancel;
    }
}
