/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
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
using System.Windows.Interop;
using Greenshot.Base.Core;
using log4net;
using System.Linq;
using Greenshot.Base.Languages;

namespace Greenshot.Views
{
    public partial class LanguageWindow : Window
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(LanguageWindow));

        public string SelectedLanguage => LanguageComboBox.SelectedValue?.ToString() ?? Texts.Config.CurrentLanguage;

        public LanguageWindow()
        {
            InitializeComponent();
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch (Exception ex)
            {
                LOG.Debug("Could not set window icon", ex);
            }

            Loaded += LanguageWindow_Loaded;
        }

        private void LanguageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var languages = Texts.Config.GetLanguages().Where(l => l.HasBaseFile).ToList();
            LanguageComboBox.ItemsSource = languages;

            if (Texts.Config.CurrentLanguage != null)
            {
                LOG.DebugFormat("Selecting {0}", Texts.Config.CurrentLanguage);
                LanguageComboBox.SelectedValue = Texts.Config.CurrentLanguage;
            }
            else
            {
                LanguageComboBox.SelectedValue = Thread.CurrentThread.CurrentUICulture.Name;
            }

            if (LanguageComboBox.SelectedItem == null && languages.Count > 0)
            {
                LanguageComboBox.SelectedIndex = 0;
            }

            // Close again when there is only one language
            if (languages.Count == 1)
            {
                LanguageComboBox.SelectedValue = languages[0].Ietf;
                Texts.SetLanguage(SelectedLanguage);
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Show the window modal, owned by a window which is no WPF window
        /// </summary>
        /// <param name="ownerHandle">The handle of the owner, IntPtr.Zero for none</param>
        public bool? ShowDialog(IntPtr ownerHandle)
        {
            if (ownerHandle != IntPtr.Zero)
            {
                new WindowInteropHelper(this) { Owner = ownerHandle };
            }
            return ShowDialog();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Texts.SetLanguage(SelectedLanguage);
            DialogResult = true;
            Close();
        }
    }
}
