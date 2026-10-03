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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Non-blocking progress window with a cancel button, shown by IUserInteraction.RunWithProgressAsync while work runs on the pool.
    /// Only used on the UI thread.
    /// </summary>
    public sealed class ProgressWindow : Window
    {
        private readonly TextBlock _message;
        private readonly ProgressBar _progressBar;
        private readonly Button _cancelButton;
        private Action _cancel;
        private bool _closingByCode;

        public ProgressWindow(string title, Action cancel)
        {
            _cancel = cancel;
            ThemedControls.ApplyDialogLook(this, title);
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            Width = 380;
            SizeToContent = SizeToContent.Height;
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch
            {
                // The window works without an icon
            }

            _message = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = WpfThemeHelper.TextSecondary,
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = Visibility.Collapsed
            };
            _progressBar = new ProgressBar
            {
                Height = 6,
                IsIndeterminate = true,
                Minimum = 0,
                Maximum = 100,
                Foreground = WpfThemeHelper.Accent,
                Background = WpfThemeHelper.CardBorder,
                BorderThickness = new Thickness(0)
            };
            _cancelButton = ThemedControls.CreateButton(Texts.Core.Cancel ?? "Cancel", false, true);
            _cancelButton.IsEnabled = cancel != null;
            _cancelButton.Click += (s, e) =>
            {
                _cancelButton.IsEnabled = false;
                RequestCancel();
            };
            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            buttonRow.Children.Add(_cancelButton);

            var root = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
            root.Children.Add(ThemedControls.CreateHeader(this, title));
            root.Children.Add(_message);
            root.Children.Add(_progressBar);
            root.Children.Add(buttonRow);
            Content = root;
        }

        /// <summary>
        /// The work ended, cancelling isn't possible anymore (call on the UI thread before closing the window).
        /// </summary>
        public void DetachCancel()
        {
            _cancel = null;
            _cancelButton.IsEnabled = false;
        }

        /// <summary>
        /// Close the window because the work ended, this doesn't cancel anything
        /// </summary>
        public void CloseByCode()
        {
            _closingByCode = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // Closing the window (Escape, Alt+F4) cancels the work too
            if (!_closingByCode)
            {
                RequestCancel();
            }
        }

        private void RequestCancel()
        {
            var cancel = _cancel;
            _cancel = null;
            cancel?.Invoke();
        }

        /// <summary>
        /// Show the progress, must be called on the UI thread.
        /// </summary>
        public void Report(ProgressInfo progress)
        {
            if (progress == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(progress.Message))
            {
                _message.Text = progress.Message;
                _message.Visibility = Visibility.Visible;
            }

            if (progress.Percentage.HasValue)
            {
                _progressBar.IsIndeterminate = false;
                _progressBar.Value = Math.Max(0, Math.Min(100, progress.Percentage.Value));
            }
            else
            {
                _progressBar.IsIndeterminate = true;
            }
        }
    }
}
