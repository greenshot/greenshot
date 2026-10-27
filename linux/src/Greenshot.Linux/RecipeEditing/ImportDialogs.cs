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

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.RecipeEditor;
using Greenshot.Linux.RecipeEditor.Helpers;
using Greenshot.Linux.Recipes;

namespace Greenshot.Linux.RecipeEditing;

/// <summary>
/// Which of the open image editors the annotations come from, the SelectEditorWindow of Windows: the list on the left,
/// a preview of the chosen one on the right
/// </summary>
internal static class SelectEditorDialog
{
    private static string T(string key) => Texts.Get("RecipeEditor", key);

    public static IImageEditor? Ask(IReadOnlyList<IImageEditor> editors)
    {
        IImageEditor? result = null;
        var window = new Window
        {
            Title = T("select_editor_title"),
            Width = 820,
            Height = 540,
            MinWidth = 680,
            MinHeight = 440,
            Icon = AppIcons.Window,
        };
        window.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://greenshot/")) { Source = new Uri("avares://greenshot/RecipeEditing/RecipeEditorStyles.axaml") });

        var items = editors.Select(e => new EditorItem(e)).ToList();
        var list = new ListBox { ItemsSource = items, SelectedIndex = items.Count > 0 ? 0 : -1 };
        AutomationProperties.SetName(list, T("select_editor_list_header"));
        list.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<EditorItem>((item, _) =>
        {
            var panel = new StackPanel { Margin = new Thickness(2, 4) };
            panel.Children.Add(new TextBlock { Text = item?.Title, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            panel.Children.Add(new TextBlock { Text = item?.Description, FontSize = 11, Opacity = 0.7 });
            return panel;
        });

        var previewTitle = new TextBlock { FontWeight = FontWeight.SemiBold, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis };
        var previewSize = new TextBlock { FontSize = 11, Opacity = 0.7, Margin = new Thickness(0, 2, 0, 8) };
        var preview = new Image { Stretch = Stretch.Uniform };
        var noPreview = new TextBlock { Text = T("select_editor_no_preview"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7 };
        void UpdatePreview()
        {
            var item = list.SelectedItem as EditorItem;
            previewTitle.Text = item?.Title;
            previewSize.Text = item?.Dimensions;
            preview.Source = item?.Preview;
            noPreview.IsVisible = item?.Preview == null;
        }

        list.SelectionChanged += (_, _) => UpdatePreview();
        list.DoubleTapped += (_, _) =>
        {
            if (list.SelectedItem is EditorItem item)
            {
                result = item.Editor;
                window.Close();
            }
        };
        UpdatePreview();

        var import = new Button { Content = T("select_editor_btn_import"), IsDefault = true, Classes = { "primary" } };
        import.Click += (_, _) =>
        {
            result = (list.SelectedItem as EditorItem)?.Editor;
            window.Close();
        };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, Classes = { "secondary" } };
        cancel.Click += (_, _) => window.Close();

        var left = new Border { Classes = { "card" }, Padding = new Thickness(6), Child = new DockPanel() };
        var header = new TextBlock { Text = T("select_editor_list_header"), FontWeight = FontWeight.SemiBold, FontSize = 11, Margin = new Thickness(6, 4, 6, 8) };
        DockPanel.SetDock(header, Dock.Top);
        ((DockPanel)left.Child).Children.Add(header);
        ((DockPanel)left.Child).Children.Add(list);

        var right = new Border { Classes = { "card" }, Padding = new Thickness(12), Margin = new Thickness(12, 0, 0, 0), Child = new DockPanel() };
        var rightPanel = (DockPanel)right.Child;
        var previewHeader = new TextBlock { Text = T("select_editor_preview_header"), FontWeight = FontWeight.SemiBold, FontSize = 11, Margin = new Thickness(0, 0, 0, 6) };
        DockPanel.SetDock(previewHeader, Dock.Top);
        DockPanel.SetDock(previewTitle, Dock.Top);
        DockPanel.SetDock(previewSize, Dock.Top);
        rightPanel.Children.Add(previewHeader);
        rightPanel.Children.Add(previewTitle);
        rightPanel.Children.Add(previewSize);
        rightPanel.Children.Add(new Panel { Children = { preview, noPreview } });

        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("320,*"), Margin = new Thickness(16, 0, 16, 12) };
        Grid.SetColumn(right, 1);
        content.Children.Add(left);
        content.Children.Add(right);

        var intro = new TextBlock { Text = T("select_editor_intro"), TextWrapping = TextWrapping.Wrap, FontSize = 11, Margin = new Thickness(16, 12, 16, 10) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16, 0, 16, 14) };
        buttons.Children.Add(import);
        buttons.Children.Add(cancel);
        var root = new DockPanel();
        root.Bind(Panel.BackgroundProperty, window.GetResourceObservable("RE.WindowBackground"));
        DockPanel.SetDock(intro, Dock.Top);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(intro);
        root.Children.Add(buttons);
        root.Children.Add(content);
        window.Content = root;
        AppRecipeEditorUi.RunModal(window);
        return result;
    }

    private sealed class EditorItem
    {
        public EditorItem(IImageEditor editor)
        {
            Editor = editor;
            Title = editor.CaptureDetails?.Title ?? T("editor_untitled");
            int count = editor.Surface?.Elements.Count ?? 0;
            int width = editor.Surface?.Width ?? 0, height = editor.Surface?.Height ?? 0;
            Dimensions = width > 0 && height > 0 ? $"{width} × {height} px" : T("editor_unknown_size");
            Description = $"{Dimensions} • " + string.Format(count == 1 ? T("annotation_count_one") : T("annotation_count_many"), count);
            try
            {
                using var flat = editor.Surface?.Flatten();
                Preview = flat == null ? null : BitmapConverter.ToAvalonia(flat);
            }
            catch (Exception ex)
            {
                Log.Warn("No preview of the editor", ex);
            }
        }

        public IImageEditor Editor { get; }

        public string Title { get; }

        public string Dimensions { get; }

        public string Description { get; }

        public Bitmap? Preview { get; }

        public override string ToString() => Title;
    }
}

/// <summary>
/// Which elements become annotations of the step, the ImportAnnotationsWindow of Windows: the elements with a check box,
/// images embedded or saved as files, appended to the annotations of the step or replacing them
/// </summary>
internal static class ImportAnnotationsDialog
{
    private static string T(string key) => Texts.Get("RecipeEditor", key);

    public static (List<Dictionary<string, object>> Annotations, bool Replace)? Ask(IRecipeEditorUi ui, List<ImportableElementItem> elements, string sourceTitle, string recipeName, bool hasExisting)
    {
        (List<Dictionary<string, object>>, bool)? result = null;
        bool ascending = true;
        var items = elements.OrderBy(i => i.Type).ThenBy(i => i.Description).ToList();
        var window = new Window
        {
            Title = T("import_title"),
            Width = 760,
            Height = 600,
            MinWidth = 600,
            MinHeight = 440,
            Icon = AppIcons.Window,
        };
        window.Styles.Add(new Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://greenshot/")) { Source = new Uri("avares://greenshot/RecipeEditing/RecipeEditorStyles.axaml") });

        var source = new TextBlock
        {
            Text = string.Format(items.Count == 1 ? T("import_source_one") : T("import_source_many"), sourceTitle ?? T("import_image_source"), items.Count),
            FontSize = 11,
            Opacity = 0.75,
        };

        // Append or replace
        var append = new RadioButton { Content = T("import_mode_append"), GroupName = "mode", IsChecked = hasExisting };
        var replace = new RadioButton { Content = T("import_mode_replace"), GroupName = "mode", IsChecked = !hasExisting, Margin = new Thickness(16, 0, 0, 0) };
        var mode = new StackPanel { Spacing = 4 };
        mode.Children.Add(new TextBlock { Text = T("import_target_step_mode"), Classes = { "section" } });
        mode.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { append, replace } });

        // The elements
        var selectAll = new CheckBox { Content = T("import_select_all"), IsThreeState = true };
        var sort = new Button { Content = T("import_sort_ascending"), Classes = { "secondary", "small" } };
        ToolTip.SetTip(sort, T("import_sort_tooltip"));
        var rows = new StackPanel { Spacing = 2 };
        var status = new TextBlock { FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.75 };
        var importButton = new Button { Content = T("import_btn_proceed"), IsDefault = true, Classes = { "primary" } };
        var folder = new TextBox { Text = DefaultFolder(), MinWidth = 320 };
        var prefix = new TextBox { Text = string.Join("_", (string.IsNullOrWhiteSpace(recipeName) ? "annotation" : recipeName).Split(Path.GetInvalidFileNameChars())), MinWidth = 200 };
        ToolTip.SetTip(prefix, T("import_filename_prefix_tooltip"));
        var browse = new Button { Content = "📁 " + T("import_btn_browse"), Classes = { "secondary" } };
        browse.Click += (_, _) =>
        {
            var picked = ui.PickFolder(T("import_select_folder"), folder.Text ?? string.Empty);
            if (!string.IsNullOrEmpty(picked))
            {
                folder.Text = picked;
            }
        };
        var assetPanel = new StackPanel { Spacing = 4 };
        assetPanel.Children.Add(new TextBlock { Text = T("import_file_destination"), Classes = { "section" } });
        assetPanel.Children.Add(new TextBlock { Text = T("import_save_to_folder"), Classes = { "label" } });
        var folderRow = new DockPanel();
        DockPanel.SetDock(browse, Dock.Right);
        browse.Margin = new Thickness(6, 0, 0, 0);
        folderRow.Children.Add(browse);
        folderRow.Children.Add(folder);
        assetPanel.Children.Add(folderRow);
        assetPanel.Children.Add(new TextBlock { Text = T("import_filename_prefix"), Classes = { "label" }, Margin = new Thickness(0, 6, 0, 2) });
        assetPanel.Children.Add(prefix);
        assetPanel.Children.Add(new TextBlock { Text = T("import_file_naming_hint"), Classes = { "hint" } });

        void UpdateState()
        {
            int selected = items.Count(i => i.IsSelected);
            status.Text = string.Format(selected == 1 ? T("import_selected_one") : T("import_selected_many"), selected, items.Count);
            importButton.IsEnabled = selected > 0;
            selectAll.IsChecked = selected == items.Count ? true : selected == 0 ? false : null;
            assetPanel.IsVisible = items.Any(i => i.IsSelected && i.CanSaveAsFile && i.StorageMode == AssetStorageMode.SaveToFile);
        }

        void FillRows()
        {
            rows.Children.Clear();
            var header = new Grid { ColumnDefinitions = new ColumnDefinitions("32,120,*,150"), Margin = new Thickness(4, 0, 4, 4) };
            header.Children.Add(Cell(T("import_column_type"), 1, true));
            header.Children.Add(Cell(T("import_column_description"), 2, true));
            header.Children.Add(Cell(T("import_column_storage"), 3, true));
            rows.Children.Add(header);
            foreach (var item in items)
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("32,120,*,150"), Margin = new Thickness(4, 1) };
                var check = new CheckBox { IsChecked = item.IsSelected, VerticalAlignment = VerticalAlignment.Center };
                AutomationProperties.SetName(check, item.ToString());
                check.IsCheckedChanged += (_, _) =>
                {
                    item.IsSelected = check.IsChecked == true;
                    UpdateState();
                };
                row.Children.Add(check);
                row.Children.Add(Cell(item.Type, 1, false));
                var description = Cell(item.Description, 2, false);
                ToolTip.SetTip(description, item.Dimensions);
                row.Children.Add(description);
                if (item.CanSaveAsFile)
                {
                    var storage = new ComboBox
                    {
                        ItemsSource = new[] { T("import_storage_embed"), T("import_storage_file") },
                        SelectedIndex = item.StorageMode == AssetStorageMode.SaveToFile ? 1 : 0,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                    };
                    AutomationProperties.SetName(storage, T("import_column_storage"));
                    storage.SelectionChanged += (_, _) =>
                    {
                        item.StorageMode = storage.SelectedIndex == 1 ? AssetStorageMode.SaveToFile : AssetStorageMode.EmbedBase64;
                        UpdateState();
                    };
                    Grid.SetColumn(storage, 3);
                    row.Children.Add(storage);
                }

                rows.Children.Add(row);
            }
        }

        static TextBlock Cell(string text, int column, bool header)
        {
            var cell = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontWeight = header ? FontWeight.SemiBold : FontWeight.Normal,
                FontSize = header ? 11 : 12,
            };
            Grid.SetColumn(cell, column);
            return cell;
        }

        selectAll.IsCheckedChanged += (_, _) =>
        {
            if (selectAll.IsChecked == null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.IsSelected = selectAll.IsChecked == true;
            }

            FillRows();
            UpdateState();
        };
        sort.Click += (_, _) =>
        {
            ascending = !ascending;
            items = ascending
                ? items.OrderBy(i => i.Type).ThenBy(i => i.Description).ToList()
                : items.OrderByDescending(i => i.Type).ThenByDescending(i => i.Description).ToList();
            sort.Content = ascending ? T("import_sort_ascending") : T("import_sort_descending");
            FillRows();
        };
        importButton.Click += (_, _) =>
        {
            var selected = items.Where(i => i.IsSelected).ToList();
            bool anySaveToFile = selected.Any(i => i.CanSaveAsFile && i.StorageMode == AssetStorageMode.SaveToFile);
            string? targetDirectory = folder.Text?.Trim();
            if (anySaveToFile)
            {
                if (string.IsNullOrWhiteSpace(targetDirectory))
                {
                    ui.ShowMessage(T("import_no_folder"), T("title_invalid_directory"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    ui.ShowMessage(string.Format(T("import_folder_error"), targetDirectory) + "\n" + ex.Message, Texts.Core("error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            string baseName = string.IsNullOrWhiteSpace(prefix.Text) ? "asset" : prefix.Text.Trim();
            result = (EditorAnnotationImporter.ProcessImport(selected, targetDirectory, baseName), replace.IsChecked == true);
            window.Close();
        };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, Classes = { "secondary" } };
        cancel.Click += (_, _) => window.Close();

        FillRows();
        UpdateState();

        var listHeader = new DockPanel { Margin = new Thickness(0, 10, 0, 4) };
        DockPanel.SetDock(sort, Dock.Right);
        listHeader.Children.Add(sort);
        listHeader.Children.Add(selectAll);

        var top = new StackPanel { Spacing = 6, Margin = new Thickness(16, 12, 16, 0) };
        top.Children.Add(source);
        top.Children.Add(mode);
        top.Children.Add(listHeader);

        var footer = new DockPanel { Margin = new Thickness(16, 8, 16, 14) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        buttons.Children.Add(importButton);
        buttons.Children.Add(cancel);
        DockPanel.SetDock(buttons, Dock.Right);
        footer.Children.Add(buttons);
        footer.Children.Add(status);

        var bottom = new StackPanel { Margin = new Thickness(16, 8, 16, 0) };
        bottom.Children.Add(assetPanel);

        var root = new DockPanel();
        root.Bind(Panel.BackgroundProperty, window.GetResourceObservable("RE.WindowBackground"));
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(top);
        root.Children.Add(footer);
        root.Children.Add(bottom);
        root.Children.Add(new Border
        {
            Classes = { "card" },
            Margin = new Thickness(16, 0),
            Padding = new Thickness(6),
            Child = new ScrollViewer { Content = rows },
        });
        window.Content = root;
        AppRecipeEditorUi.RunModal(window);
        return result;
    }

    private static string DefaultFolder()
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        return string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder) ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) : folder;
    }
}
