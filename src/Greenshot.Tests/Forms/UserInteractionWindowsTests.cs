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
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Wpf;
using Xunit;

namespace Greenshot.Tests.Forms
{
    /// <summary>
    /// The WPF windows of the interactive IUserInteraction: progress and output quality
    /// </summary>
    [Collection(TestCollections.WpfThemeState)]
    public class UserInteractionWindowsTests
    {
        public UserInteractionWindowsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static void RunOnSta(Action action)
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (threadEx != null)
            {
                throw new Exception("Failed on the STA thread", threadEx);
            }
        }

        /// <summary>
        /// Show the window outside of the visible screen, without activating it
        /// </summary>
        private static void ShowOffScreen(Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = -5000;
            window.Top = -5000;
            window.ShowActivated = false;
            window.Show();
        }

        private static T Find<T>(DependencyObject parent) where T : DependencyObject
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            {
                if (child is T found)
                {
                    return found;
                }

                var nested = Find<T>(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        [Fact]
        public void ProgressWindow_ClosedByUser_CancelsTheWork()
        {
            RunOnSta(() =>
            {
                int cancelled = 0;
                var window = new ProgressWindow("Uploading", () => cancelled++);
                ShowOffScreen(window);
                window.Close();
                Assert.Equal(1, cancelled);
            });
        }

        [Fact]
        public void ProgressWindow_ClosedWhenTheWorkEnded_DoesNotCancel()
        {
            RunOnSta(() =>
            {
                int cancelled = 0;
                var window = new ProgressWindow("Uploading", () => cancelled++);
                ShowOffScreen(window);
                window.DetachCancel();
                window.CloseByCode();
                Assert.Equal(0, cancelled);
            });
        }

        [Fact]
        public void ProgressWindow_CancelButton_CancelsOnce()
        {
            RunOnSta(() =>
            {
                int cancelled = 0;
                var window = new ProgressWindow("Uploading", () => cancelled++);
                ShowOffScreen(window);
                var cancelButton = Find<Button>(window);
                cancelButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Assert.False(cancelButton.IsEnabled);
                window.Close();
                Assert.Equal(1, cancelled);
            });
        }

        [Fact]
        public void ProgressWindow_Report_SwitchesBetweenPercentageAndIndeterminate()
        {
            RunOnSta(() =>
            {
                var window = new ProgressWindow("Uploading", null);
                var progressBar = Find<ProgressBar>(window);
                Assert.True(progressBar.IsIndeterminate);

                window.Report(new ProgressInfo("Half way", 50));
                Assert.False(progressBar.IsIndeterminate);
                Assert.Equal(50, progressBar.Value);

                window.Report(new ProgressInfo(null, 150));
                Assert.Equal(100, progressBar.Value);

                window.Report(new ProgressInfo("Waiting for the server"));
                Assert.True(progressBar.IsIndeterminate);
                window.CloseByCode();
            });
        }

        [Fact]
        public void QualityWindow_Ok_TakesTheValues()
        {
            RunOnSta(() =>
            {
                var settings = new SurfaceOutputSettings("jpg", 80, false);
                var window = new QualityWindow(settings);
                var slider = Find<Slider>(window);
                Assert.True(slider.IsEnabled);
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -5000;
                window.Top = -5000;
                window.ShowActivated = false;
                window.Loaded += (s, e) => window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    slider.Value = 55;
                    // The first check box is "reduce colors", the second "don't ask again"
                    Find<CheckBox>(window).IsChecked = true;
                    var ok = LogicalTreeHelper.GetChildren((StackPanel)window.Content).OfType<StackPanel>().Last().Children.OfType<Button>().First(b => b.IsDefault);
                    ok.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }), DispatcherPriority.Background);

                Assert.True(window.ShowDialog());
                Assert.Equal(55, settings.JPGQuality);
                Assert.True(settings.ReduceColors);
            });
        }

        [Fact]
        public void QualityWindow_Cancel_KeepsTheValues()
        {
            RunOnSta(() =>
            {
                var settings = new SurfaceOutputSettings("png", 80, false);
                var window = new QualityWindow(settings);
                var slider = Find<Slider>(window);
                // The quality only matters for JPEG
                Assert.False(slider.Parent is UIElement parent && parent.IsEnabled);
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -5000;
                window.Top = -5000;
                window.ShowActivated = false;
                window.Loaded += (s, e) => window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    slider.Value = 10;
                    window.Close();
                }), DispatcherPriority.Background);

                Assert.NotEqual(true, window.ShowDialog());
                Assert.Equal(80, settings.JPGQuality);
                Assert.Same(settings, window.Settings);
            });
        }
    }
}
