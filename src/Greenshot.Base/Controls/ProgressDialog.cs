/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// Non-blocking progress dialog with a cancel button, shown by IUserInteraction.RunWithProgressAsync while work runs on the pool.
    /// Replaces the PleaseWaitForm (own STA thread) and the BackgroundForm (DoEvents). Only used on the UI thread.
    /// </summary>
    public sealed class ProgressDialog : Form
    {
        private readonly Label _messageLabel;
        private readonly ProgressBar _progressBar;
        private readonly Button _cancelButton;

        public ProgressDialog(string title, Action cancel)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(360, 110);
            Icon = GreenshotResources.GetGreenshotIcon();

            _messageLabel = new Label
            {
                AutoSize = false,
                Location = new Point(12, 12),
                Size = new Size(336, 32),
                Text = Language.GetString("wait_ie_capture") ?? title
            };
            _progressBar = new ProgressBar
            {
                Location = new Point(12, 48),
                Size = new Size(336, 18),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };
            _cancelButton = new Button
            {
                Location = new Point(273, 76),
                Size = new Size(75, 25),
                Text = Language.GetString("CANCEL") ?? "Cancel",
                Enabled = cancel != null
            };
            _cancelButton.Click += (_, _) =>
            {
                _cancelButton.Enabled = false;
                cancel?.Invoke();
            };
            CancelButton = _cancelButton;
            Controls.Add(_messageLabel);
            Controls.Add(_progressBar);
            Controls.Add(_cancelButton);
        }

        /// <summary>
        /// Show the progress, must be called on the UI thread.
        /// </summary>
        public void Report(ProgressInfo progress)
        {
            if (IsDisposed || progress == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(progress.Message))
            {
                _messageLabel.Text = progress.Message;
            }

            if (progress.Percentage.HasValue)
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
                _progressBar.Value = Math.Max(0, Math.Min(100, (int)progress.Percentage.Value));
            }
            else
            {
                _progressBar.Style = ProgressBarStyle.Marquee;
            }
        }
    }
}
