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

using System.Windows;
using Greenshot.Document;
using Greenshot.WpfEditor.ViewModels;
using Greenshot.WpfEditor.Views;

namespace Greenshot.WpfEditor
{
    /// <summary>
    /// Opens documents in the editor: in a new tab of the open editor window, or in a new window when none is open.
    /// Call it on the UI thread.
    /// </summary>
    public static class WpfEditorLauncher
    {
        /// <summary>
        /// The host gives the editor the destinations of Greenshot, set it once before the first window opens
        /// </summary>
        public static void Configure(IWpfEditorHost host) => EditorSession.Current.Host = host;

        public static void Open(GreenshotDocument document, string title, string filePath = null)
        {
            var window = Window();
            window.ViewModel.Add(document, title, filePath);
            Show(window);
        }

        public static void OpenFile(string path)
        {
            var window = Window();
            window.ViewModel.OpenFile(path);
            Show(window);
        }

        // Greenshot is a WinForms application, it has no WPF Application which lists the windows
        private static EditorWindow _window;

        private static EditorWindow Window()
        {
            EditorTexts.Register();
            if (_window == null)
            {
                _window = new EditorWindow(new EditorWindowViewModel());
                _window.Closed += (_, _) => _window = null;
            }

            return _window;
        }

        private static void Show(EditorWindow window)
        {
            if (!window.IsVisible)
            {
                window.Show();
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
        }
    }
}
