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
using GreenshotLanguage = Greenshot.Base.Core.Language;

namespace Greenshot.Forms.Wpf
{
    public partial class LanguageWindow : Window
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(LanguageWindow));

        public string SelectedLanguage => LanguageComboBox.SelectedValue?.ToString() ?? GreenshotLanguage.CurrentLanguage;

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
            LanguageComboBox.ItemsSource = GreenshotLanguage.SupportedLanguages;

            if (GreenshotLanguage.CurrentLanguage != null)
            {
                LOG.DebugFormat("Selecting {0}", GreenshotLanguage.CurrentLanguage);
                LanguageComboBox.SelectedValue = GreenshotLanguage.CurrentLanguage;
            }
            else
            {
                LanguageComboBox.SelectedValue = Thread.CurrentThread.CurrentUICulture.Name;
            }

            if (LanguageComboBox.SelectedItem == null && GreenshotLanguage.SupportedLanguages.Count > 0)
            {
                LanguageComboBox.SelectedIndex = 0;
            }

            // Close again when there is only one language
            if (GreenshotLanguage.SupportedLanguages.Count == 1)
            {
                LanguageComboBox.SelectedValue = GreenshotLanguage.SupportedLanguages[0].Ietf;
                GreenshotLanguage.CurrentLanguage = SelectedLanguage;
                DialogResult = true;
                Close();
            }
        }

        public bool? ShowDialog(System.Windows.Forms.IWin32Window owner)
        {
            if (owner != null)
            {
                new WindowInteropHelper(this) { Owner = owner.Handle };
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
            GreenshotLanguage.CurrentLanguage = SelectedLanguage;
            DialogResult = true;
            Close();
        }
    }
}
