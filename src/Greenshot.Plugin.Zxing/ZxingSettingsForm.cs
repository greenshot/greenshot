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

using System;
using System.Windows.Forms;

namespace Greenshot.Plugin.Zxing
{
    public class ZxingSettingsForm : Form
    {
        private IZxingConfiguration _config;
        private CheckBox _chkScanOnCapture;
        private Button _btnOk;
        private Button _btnCancel;

        public ZxingSettingsForm(IZxingConfiguration config)
        {
            _config = config;
            InitializeComponent();
            _chkScanOnCapture.Checked = _config.ScanOnCapture;
        }

        private void InitializeComponent()
        {
            this._chkScanOnCapture = new CheckBox();
            this._btnOk = new Button();
            this._btnCancel = new Button();
            this.SuspendLayout();

            // chkScanOnCapture
            this._chkScanOnCapture.AutoSize = true;
            this._chkScanOnCapture.Location = new System.Drawing.Point(20, 30);
            this._chkScanOnCapture.Name = "_chkScanOnCapture";
            this._chkScanOnCapture.Size = new System.Drawing.Size(280, 17);
            this._chkScanOnCapture.TabIndex = 0;
            this._chkScanOnCapture.Text = "Enable QR code scanning on region capture";
            this._chkScanOnCapture.UseVisualStyleBackColor = true;

            // btnOk
            this._btnOk.DialogResult = DialogResult.OK;
            this._btnOk.Location = new System.Drawing.Point(120, 80);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(75, 25);
            this._btnOk.TabIndex = 1;
            this._btnOk.Text = "OK";
            this._btnOk.UseVisualStyleBackColor = true;
            this._btnOk.Click += new EventHandler(this.BtnOk_Click);

            // btnCancel
            this._btnCancel.DialogResult = DialogResult.Cancel;
            this._btnCancel.Location = new System.Drawing.Point(210, 80);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(75, 25);
            this._btnCancel.TabIndex = 2;
            this._btnCancel.Text = "Cancel";
            this._btnCancel.UseVisualStyleBackColor = true;

            // ZxingSettingsForm
            this.AcceptButton = this._btnOk;
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(310, 130);
            this.Controls.Add(this._chkScanOnCapture);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ZxingSettingsForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "ZXing Plugin Settings";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            _config.ScanOnCapture = _chkScanOnCapture.Checked;
            this.Close();
        }
    }
}
