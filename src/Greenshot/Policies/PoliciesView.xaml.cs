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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Helpers;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Policies
{
    /// <summary>
    /// The Windows policies of Self-Service; the DataContext is a <see cref="PoliciesSectionViewModel"/>.
    /// </summary>
    public partial class PoliciesView : UserControl
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoliciesView));

        public PoliciesView()
        {
            InitializeComponent();
        }

        private PoliciesSectionViewModel ViewModel => DataContext as PoliciesSectionViewModel;

        /// <summary>
        /// Group Policy and Intune write the registry while Greenshot runs, Dapplo.Ini picks that up; this is for a check right away
        /// </summary>
        private void OnReloadClicked(object sender, RoutedEventArgs e)
        {
            ApplicationStartupHelper.ReloadConfig();
            ViewModel?.Refresh();
        }

        private void OnCopyClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ClipboardHelper.SetClipboardData(ViewModel.ToText());
            }
        }

        /// <summary>
        /// The administrative templates (greenshot.admx with the language folders) of the sections this Greenshot has, including the loaded plugins
        /// </summary>
        private void OnSaveTemplatesClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                FileName = "greenshot.admx",
                DefaultExt = ".admx",
                Filter = "ADMX (*.admx)|*.admx"
            };
            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(dialog.FileName);
                int count = PolicyTemplates.Write(directory, IniConfigRegistry.Get().GetSections());
                StatusText.Text = string.Format(Texts.SelfService.PoliciesTemplatesSaved, count, directory);
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't save the policy templates", ex);
                StatusText.Text = ex.Message;
            }
        }
    }
}
