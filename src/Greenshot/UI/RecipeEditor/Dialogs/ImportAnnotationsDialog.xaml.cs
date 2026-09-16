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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Greenshot.UI.RecipeEditor.Helpers;

namespace Greenshot.UI.RecipeEditor.Dialogs
{
    /// <summary>
    /// Modern interactive dialog for importing annotations into a recipe step.
    /// </summary>
    public partial class ImportAnnotationsDialog : Window
    {
        private List<ImportableElementItem> _items;
        private bool _isSortAscending = true;

        public bool ShouldReplaceExisting => ReplaceRadio.IsChecked == true;

        public List<Dictionary<string, object>> ResultAnnotations { get; private set; } = new List<Dictionary<string, object>>();

        public ImportAnnotationsDialog(
            IEnumerable<ImportableElementItem> items,
            string sourceName,
            string recipeName,
            bool hasExistingAnnotations)
        {
            InitializeComponent();

            // Default sorted by A-Z
            _items = (items ?? Enumerable.Empty<ImportableElementItem>())
                .OrderBy(i => i.Type)
                .ThenBy(i => i.Description)
                .ToList();

            ElementsListView.ItemsSource = _items;

            SourceTextBlock.Text = $"Source: {sourceName ?? "Image Source"} • {_items.Count} element{(_items.Count == 1 ? "" : "s")} found";

            // Target Step Mode: pre-select based on whether step already has annotations, but keep both responsive
            if (hasExistingAnnotations)
            {
                AppendRadio.IsChecked = true;
            }
            else
            {
                ReplaceRadio.IsChecked = true;
            }

            AppendRadio.Checked += (s, e) => UpdateCardHighlights();
            ReplaceRadio.Checked += (s, e) => UpdateCardHighlights();
            UpdateCardHighlights();

            string defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (string.IsNullOrWhiteSpace(defaultDir) || !Directory.Exists(defaultDir))
            {
                defaultDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            DirectoryTextBox.Text = defaultDir;

            string safeRecipeName = string.Join("_", (string.IsNullOrWhiteSpace(recipeName) ? "annotation" : recipeName).Split(Path.GetInvalidFileNameChars()));
            BaseFileNameTextBox.Text = safeRecipeName;

            SourceInitialized += (s, e) => ApplyImmersiveDarkMode();

            UpdateUIState();
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnCloseTitleBarClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnAppendCardClicked(object sender, MouseButtonEventArgs e)
        {
            AppendRadio.IsChecked = true;
            UpdateCardHighlights();
        }

        private void OnReplaceCardClicked(object sender, MouseButtonEventArgs e)
        {
            ReplaceRadio.IsChecked = true;
            UpdateCardHighlights();
        }

        private void UpdateCardHighlights()
        {
            if (AppendCard != null && ReplaceCard != null)
            {
                bool isAppend = AppendRadio.IsChecked == true;
                AppendCard.BorderBrush = isAppend ? WpfThemeHelper.Accent : WpfThemeHelper.CardBorder;
                ReplaceCard.BorderBrush = !isAppend ? WpfThemeHelper.Accent : WpfThemeHelper.CardBorder;
            }
        }

        private void OnSortClicked(object sender, RoutedEventArgs e)
        {
            _isSortAscending = !_isSortAscending;
            if (_isSortAscending)
            {
                _items = _items.OrderBy(i => i.Type).ThenBy(i => i.Description).ToList();
                SortButton.Content = "Sort A-Z ▲";
            }
            else
            {
                _items = _items.OrderByDescending(i => i.Type).ThenByDescending(i => i.Description).ToList();
                SortButton.Content = "Sort Z-A ▼";
            }
            ElementsListView.ItemsSource = null;
            ElementsListView.ItemsSource = _items;
        }

        private void UpdateUIState()
        {
            int selectedCount = _items.Count(i => i.IsSelected);
            StatusTextBlock.Text = $"{selectedCount} of {_items.Count} element{(selectedCount == 1 ? "" : "s")} selected";
            ImportBtn.IsEnabled = selectedCount > 0;

            if (_items.Count > 0)
            {
                if (selectedCount == _items.Count)
                {
                    SelectAllCheckBox.IsChecked = true;
                }
                else if (selectedCount == 0)
                {
                    SelectAllCheckBox.IsChecked = false;
                }
                else
                {
                    SelectAllCheckBox.IsChecked = null; // Indeterminate
                }
            }

            bool anySaveToFile = _items.Any(i => i.IsSelected && i.CanSaveAsFile && i.StorageMode == AssetStorageMode.SaveToFile);
            AssetOutputPanel.Visibility = anySaveToFile ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnSelectAllClicked(object sender, RoutedEventArgs e)
        {
            bool check = SelectAllCheckBox.IsChecked == true;
            foreach (var item in _items)
            {
                item.IsSelected = check;
            }
            UpdateUIState();
        }

        private void OnItemSelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateUIState();
        }

        private void OnStorageModeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateUIState();
        }

        private void OnBrowseDirectoryClicked(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Destination Folder for Exported Assets";
                if (!string.IsNullOrWhiteSpace(DirectoryTextBox.Text) && Directory.Exists(DirectoryTextBox.Text))
                {
                    dialog.SelectedPath = DirectoryTextBox.Text;
                }
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnProceedClicked(object sender, RoutedEventArgs e)
        {
            var selectedItems = _items.Where(i => i.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one element to import.", "Import Annotations", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool anySaveToFile = selectedItems.Any(i => i.CanSaveAsFile && i.StorageMode == AssetStorageMode.SaveToFile);
            string targetDir = DirectoryTextBox.Text?.Trim();
            if (anySaveToFile)
            {
                if (string.IsNullOrWhiteSpace(targetDir))
                {
                    MessageBox.Show(this, "Please specify a destination folder for elements to be saved on disk.", "Invalid Directory", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Could not create or access directory '{targetDir}':\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            string baseName = BaseFileNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "asset";
            }

            ResultAnnotations = EditorAnnotationImporter.ProcessImport(selectedItems, targetDir, baseName);
            DialogResult = true;
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int useImmersiveDarkMode = WpfThemeHelper.IsDarkMode ? 1 : 0;
                    int hr = DwmSetWindowAttribute(helper.Handle, 20, ref useImmersiveDarkMode, sizeof(int));
                    if (hr != 0)
                    {
                        DwmSetWindowAttribute(helper.Handle, 19, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
            }
            catch
            {
                // Silently ignore if DWM call is unsupported on older OS
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
