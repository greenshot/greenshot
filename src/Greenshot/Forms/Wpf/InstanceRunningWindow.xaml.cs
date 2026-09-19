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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;

namespace Greenshot.Forms.Wpf
{
    public class RunningInstanceItem
    {
        public int Index { get; set; }
        public int ProcessId { get; set; }
        public string Path { get; set; }

        public string DisplayIndex => $"{Index}:";
    }

    /// <summary>
    /// Interaction logic for InstanceRunningWindow.xaml
    /// Modern WPF dialog notifying the user that another Greenshot instance is running.
    /// </summary>
    public partial class InstanceRunningWindow : Window
    {
        public InstanceRunningWindow(IEnumerable<RunningInstanceItem> instances)
        {
            InitializeComponent();
            InstancesItemsControl.ItemsSource = instances;

            try
            {
                Icon = GreenshotResources.GetGreenshotIcon()?.ToBitmapSource();
            }
            catch
            {
                // Ignore in headless/test environments
            }

            Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
            System.ComponentModel.PropertyChangedEventHandler themeHandler = (s, e) =>
            {
                if (Dispatcher.CheckAccess())
                {
                    Resources.MergedDictionaries.Clear();
                    Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
                }
                else if (!Dispatcher.HasShutdownStarted)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            Resources.MergedDictionaries.Clear();
                            Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
                        }
                        catch
                        {
                            // Window or dispatcher shutting down
                        }
                    });
                }
            };
            ThemeManager.Instance.PropertyChanged += themeHandler;
            Closed += (s, e) => ThemeManager.Instance.PropertyChanged -= themeHandler;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowInExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    ExplorerHelper.OpenInExplorer(path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning("Failed to show in explorer: {0}", ex);
                }
            }
        }
    }
}
