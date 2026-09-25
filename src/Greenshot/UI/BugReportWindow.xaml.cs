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
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Greenshot.UI.ViewModels;

namespace Greenshot.UI
{
    /// <summary>
    /// Modern WPF Bug Report Window with ThemeManager integration, collapsible details,
    /// stable stack trace hashing, and GitHub duplicate search support.
    /// </summary>
    public partial class BugReportWindow : Window
    {
        public BugReportViewModel ViewModel => DataContext as BugReportViewModel;

        public BugReportWindow(BugReportViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public BugReportWindow(Exception ex, string fullReport = null)
            : this(new BugReportViewModel(ex, fullReport))
        {
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSearchGitHubClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.SearchGitHub();
        }

        private void OnOpenSelfServiceClicked(object sender, RoutedEventArgs e)
        {
            SelfService.SelfServiceWindow.ShowSelfService(this);
        }

        private void OnReportIssueClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.ReportIssue();
        }

        private void OnCopyReportClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.CopyReportToClipboard();
        }

        private void OnCopyStackTraceClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.CopyStackTraceToClipboard();
        }

        private void OnToggleDetailsClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.ToggleDetails();
        }

        private void OnUpgradeClicked(object sender, RoutedEventArgs e)
        {
            ViewModel?.OpenUpgradePage();
        }

        /// <summary>
        /// Displays the bug report dialog safely, ensuring execution on an STA thread.
        /// If called from a non-STA thread (such as a ThreadPool/MTA thread during unobserved task exceptions),
        /// an STA thread is automatically spawned to host the dialog.
        /// </summary>
        /// <param name="ex">The exception to display</param>
        /// <param name="fullReport">Optional pre-built report string</param>
        public static void ShowReport(Exception ex, string fullReport = null)
        {
            void DisplayDialog()
            {
                try
                {
                    var vm = new BugReportViewModel(ex, fullReport);
                    var window = new BugReportWindow(vm);
                    window.ShowDialog();
                }
                catch (Exception)
                {
                    // Fallback to basic message box if WPF window fails to initialize
                    MessageBox.Show(
                        $"An unexpected error occurred and the error reporter could not be initialized.\n\n{ex?.Message ?? fullReport}",
                        "Greenshot - Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                DisplayDialog();
            }
            else
            {
                var staThread = new Thread(DisplayDialog)
                {
                    Name = "GreenshotBugReportSTAThread",
                    IsBackground = true
                };
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
        }
    }
}
