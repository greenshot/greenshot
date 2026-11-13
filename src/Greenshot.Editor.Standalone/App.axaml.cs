/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Ipc;
using Greenshot.Editor.Standalone.ViewModels;
using Greenshot.Editor.Standalone.Views;

namespace Greenshot.Editor.Standalone
{
    /// <summary>
    /// The Avalonia application of the editor: one window with a tab per document
    /// </summary>
    public sealed class App : Application
    {
        private EditorWindow _window;

        /// <summary>
        /// What the command line asked to open, set before the application starts
        /// </summary>
        public static StartRequest Request { get; set; } = new();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            RequestedThemeVariant = EditorSettings.Current.Theme?.Trim().ToLowerInvariant() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var greenshot = GreenshotLink.Create();
                EditorSession.Current.Greenshot = greenshot;
                _window = new EditorWindow(new EditorWindowViewModel());
                desktop.MainWindow = _window;
                desktop.Exit += (_, _) =>
                {
                    greenshot?.Dispose();
                    EditorSettings.Save();
                };
                greenshot?.Start();
                // Another editor which is started hands its files to this one
                SingleInstance.Received += request => Dispatcher.UIThread.Post(() => Open(request));
                _window.Opened += (_, _) => Open(Request);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Open(StartRequest request)
        {
            if (_window == null || request == null)
            {
                return;
            }

            OpenAsync(request).Forget("Opening the files of the command line");
            _window.BringToFront();
        }

        private async System.Threading.Tasks.Task OpenAsync(StartRequest request)
        {
            foreach (string file in request.Handover ?? new List<string>())
            {
                if (EditorPaths.IsHandoverFile(file))
                {
                    await _window.ViewModel.OpenHandoverAsync(file);
                }
                else
                {
                    // Only files in the folder for it are handed over (and removed), any other is just opened
                    EditorLog.Warn($"{file} is not in the handover folder, it is opened like any file");
                    await _window.ViewModel.OpenFileAsync(file);
                }
            }

            foreach (string file in request.Files ?? new List<string>())
            {
                await _window.ViewModel.OpenFileAsync(file);
            }
        }
    }
}
