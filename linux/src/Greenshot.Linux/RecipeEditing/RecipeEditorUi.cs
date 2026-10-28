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
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Editing;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.RecipeEditor;
using Greenshot.Linux.RecipeEditor.Helpers;
using Greenshot.Linux.RecipeEditor.ViewModels;
using Greenshot.Linux.Recipes;

namespace Greenshot.Linux.RecipeEditing;

/// <summary>
/// The recipe editor and the recipe manager, the Recipe Editor plugin of Windows: one editor window, the manager on its own
/// or from the editor
/// </summary>
internal sealed class RecipeEditorService : IRecipeEditorService
{
    private RecipeEditorWindow? _editor;

    public bool IsEditorOpen => _editor != null;

    public void OpenEditor(string? recipeId = null) => Dispatcher.UIThread.Post(() =>
    {
        try
        {
            if (_editor == null)
            {
                _editor = new RecipeEditorWindow(RecipeServices.Manager);
                _editor.Closed += (_, _) => _editor = null;
                _editor.Show();
            }
            else
            {
                if (_editor.WindowState == WindowState.Minimized)
                {
                    _editor.WindowState = WindowState.Normal;
                }

                _editor.Activate();
            }

            if (!string.IsNullOrEmpty(recipeId))
            {
                _editor.ViewModel.SelectRecipeById(recipeId);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Opening the recipe editor failed", ex);
        }
    });

    public void OpenRecipeManager() => Dispatcher.UIThread.Post(() =>
    {
        try
        {
            if (_editor != null)
            {
                _editor.Activate();
                _editor.ViewModel.OpenRecipeManager();
                return;
            }

            var viewModel = new RecipeManagerViewModel(RecipeServices.Manager, RecipeServices.Pipeline,
                selected => OpenEditor(selected.Id),
                () => OpenEditor(),
                extension => OpenEditor(extension.Id));
            var window = new RecipeManagerWindow(viewModel);
            window.Show();
        }
        catch (Exception ex)
        {
            Log.Error("Opening the recipe manager failed", ex);
        }
    });
}

/// <summary>
/// The dialogs of the recipe editor: they are modal, like ShowDialog of WPF, so the view models ported from Windows
/// get their answer right away
/// </summary>
internal sealed class AppRecipeEditorUi(GreenshotController controller) : IRecipeEditorUi
{
    public event Action? ThemeChanged;

    public void Attach()
    {
        if (Application.Current is { } application)
        {
            application.ActualThemeVariantChanged += (_, _) => ThemeChanged?.Invoke();
        }
    }

    private static Window? ActiveWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.FirstOrDefault(w => w.IsActive)
        ?? (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.LastOrDefault(w => w.IsVisible);

    /// <summary>
    /// Shows the window and waits for it on the UI thread, the UI keeps running
    /// </summary>
    internal static void RunModal(Window window)
    {
        var frame = new DispatcherFrame();
        window.Closed += (_, _) => frame.Continue = false;
        var owner = ActiveWindow;
        if (owner != null && owner != window)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _ = window.ShowDialog(owner);
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        window.Activate();
        Dispatcher.UIThread.PushFrame(frame);
    }

    /// <summary>
    /// Waits for the task on the UI thread, the UI keeps running
    /// </summary>
    private static T Wait<T>(Task<T> task)
    {
        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.Default);
            Dispatcher.UIThread.PushFrame(frame);
        }

        return task.GetAwaiter().GetResult();
    }

    public MessageBoxResult ShowMessage(string text, string caption, MessageBoxButton buttons, MessageBoxImage image)
    {
        (string Text, MessageBoxResult Result)[] choices = buttons switch
        {
            MessageBoxButton.YesNo => [(Texts.Linux("yes"), MessageBoxResult.Yes), (Texts.Linux("no"), MessageBoxResult.No)],
            MessageBoxButton.YesNoCancel => [(Texts.Linux("yes"), MessageBoxResult.Yes), (Texts.Linux("no"), MessageBoxResult.No), (Texts.Core("CANCEL"), MessageBoxResult.Cancel)],
            MessageBoxButton.OKCancel => [(Texts.Core("OK"), MessageBoxResult.OK), (Texts.Core("CANCEL"), MessageBoxResult.Cancel)],
            _ => [(Texts.Core("OK"), MessageBoxResult.OK)],
        };
        var closed = buttons switch
        {
            MessageBoxButton.YesNo => MessageBoxResult.No,
            MessageBoxButton.OK => MessageBoxResult.OK,
            _ => MessageBoxResult.Cancel,
        };
        int index = ShowChoice(caption, text, image, choices.Select(c => c.Text).ToList(), 0, -1);
        return index >= 0 && index < choices.Length ? choices[index].Result : closed;
    }

    public int ShowChoice(string caption, string text, MessageBoxImage image, IReadOnlyList<string> buttons, int defaultIndex, int cancelIndex)
    {
        int result = cancelIndex;
        var window = new Window
        {
            Title = caption,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 360,
            MaxWidth = 640,
            CanResize = false,
            ShowInTaskbar = false,
            Icon = AppIcons.Window,
        };
        string icon = image switch
        {
            MessageBoxImage.Error => "⛔",
            MessageBoxImage.Warning => "⚠️",
            MessageBoxImage.Question => "❓",
            MessageBoxImage.Information => "ℹ️",
            _ => string.Empty,
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            var button = new Button
            {
                Content = Texts.WithoutAccessKey(buttons[i]),
                MinWidth = 90,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsDefault = i == defaultIndex,
                IsCancel = i == cancelIndex,
            };
            if (i == defaultIndex)
            {
                button.Classes.Add("accent");
            }

            button.Click += (_, _) =>
            {
                result = index;
                window.Close();
            };
            row.Children.Add(button);
        }

        var message = new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 540, VerticalAlignment = VerticalAlignment.Center };
        var body = new DockPanel { Margin = new Thickness(20, 18, 20, 16) };
        DockPanel.SetDock(row, Dock.Bottom);
        row.Margin = new Thickness(0, 18, 0, 0);
        body.Children.Add(row);
        if (icon.Length > 0)
        {
            var iconBlock = new TextBlock { Text = icon, FontSize = 26, Margin = new Thickness(0, 0, 14, 0), VerticalAlignment = VerticalAlignment.Top };
            DockPanel.SetDock(iconBlock, Dock.Left);
            body.Children.Add(iconBlock);
        }

        body.Children.Add(message);
        window.Content = body;
        RunModal(window);
        return result;
    }

    public string PickFile(FileDialogRequest request)
    {
        var filters = ParseFilter(request.Filter);
        if (controller.FileChooser is { } portal)
        {
            return (request.Save
                ? Wait(portal.SaveFileAsync(request.Title ?? string.Empty, Path.Combine(request.InitialDirectory ?? DefaultFolder, request.FileName ?? string.Empty), filters))
                : Wait(portal.OpenFileAsync(request.Title ?? string.Empty, filters)))!;
        }

        // Without the portal: the dialog of Avalonia
        var storage = ActiveWindow?.StorageProvider;
        if (storage == null)
        {
            return null!;
        }

        var types = filters.Select(f => new FilePickerFileType(f.Name) { Patterns = f.Pattern.Split(';') }).ToList();
        if (request.Save)
        {
            var file = Wait(storage.SaveFilePickerAsync(new FilePickerSaveOptions { Title = request.Title, SuggestedFileName = request.FileName, FileTypeChoices = types }));
            return file?.TryGetLocalPath()!;
        }

        var files = Wait(storage.OpenFilePickerAsync(new FilePickerOpenOptions { Title = request.Title, FileTypeFilter = types }));
        return files.FirstOrDefault()?.TryGetLocalPath()!;
    }

    public string PickFolder(string title, string initialDirectory)
    {
        if (controller.FileChooser is { } portal)
        {
            return Wait(portal.OpenFolderAsync(title))!;
        }

        var storage = ActiveWindow?.StorageProvider;
        var folders = storage == null ? [] : Wait(storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title }));
        return folders.FirstOrDefault()?.TryGetLocalPath()!;
    }

    private static string DefaultFolder => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    /// "Name|*.a;*.b|Name|*" of Windows as filters of the portal
    /// </summary>
    internal static IReadOnlyList<(string Name, string Pattern)> ParseFilter(string? filter)
    {
        var parts = (filter ?? string.Empty).Split('|');
        var result = new List<(string, string)>();
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            // "*.*" of Windows is "*" here
            result.Add((parts[i], parts[i + 1] == "*.*" ? "*" : parts[i + 1]));
        }

        return result;
    }

    public string PickColor(string current) => ColorDialog.Ask(current);

    public void ShowRecipeManager(RecipeManagerViewModel viewModel) => RunModal(new RecipeManagerWindow(viewModel));

    public IReadOnlyList<IImageEditor> OpenEditors =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.OfType<EditorWindow>().SelectMany(w => w.Tabs).Cast<IImageEditor>().ToList() ?? [];

    public IImageEditor SelectEditor(IReadOnlyList<IImageEditor> editors) => SelectEditorDialog.Ask(editors)!;

    public (List<Dictionary<string, object>> Annotations, bool Replace)? ReviewImport(List<ImportableElementItem> items, string sourceTitle, string recipeName, bool hasExisting) =>
        ImportAnnotationsDialog.Ask(this, items, sourceTitle, recipeName, hasExisting);

    public void PlaySound(string file) => RecipeServices.Host?.PlaySound(file);

    public void SetClipboardText(string text) => Wait(SystemClipboard.SetTextAsync(text).ContinueWith(_ => true, TaskScheduler.Default));

    public bool IsDarkMode => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    /// <summary>
    /// Light or dark for all windows, stored like the theme of the settings
    /// </summary>
    public void ToggleTheme()
    {
        controller.Settings.Theme = IsDarkMode ? "Light" : "Dark";
        controller.Settings.Save();
        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Light : ThemeVariant.Dark;
        }
    }

    public void Post(Action action) => Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
}

/// <summary>
/// The color picker of the recipe editor: any color with transparency
/// </summary>
internal static class ColorDialog
{
    public static string Ask(string? current)
    {
        string result = null!;
        var initial = !string.IsNullOrWhiteSpace(current) && !current.Equals("transparent", StringComparison.OrdinalIgnoreCase) && Color.TryParse(current, out var parsed)
            ? parsed
            : Colors.Transparent;
        var view = new ColorView { Color = initial, IsAlphaVisible = true, Width = 330, Height = 380 };
        var window = new Window
        {
            Title = Texts.Get("RecipeEditor", "pick_color"),
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            Icon = AppIcons.Window,
        };
        var ok = new Button { Content = Texts.Linux("ok"), IsDefault = true, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        var transparent = new Button { Content = Texts.Editor("color_transparent"), MinWidth = 90 };
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
        ok.Click += (_, _) =>
        {
            var color = view.Color;
            result = color.A == 0 ? "transparent" : color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
            window.Close();
        };
        transparent.Click += (_, _) =>
        {
            result = "transparent";
            window.Close();
        };
        cancel.Click += (_, _) => window.Close();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        buttons.Children.Add(transparent);
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(view);
        panel.Children.Add(buttons);
        window.Content = panel;
        AppRecipeEditorUi.RunModal(window);
        return result;
    }
}
