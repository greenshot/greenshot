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
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Helpers;
using Greenshot.Linux.Recipes.Pipeline;
using SkiaSharp;

using Greenshot.Linux.Recipes;

namespace Greenshot.Linux.RecipeUi;

/// <summary>
/// The windows of recipe steps: the prompt (UserPrompt), the destination flyout (DynamicDestination) and questions
/// </summary>
internal static class RecipeWindows
{
    private static string T(string key, params object?[] args) => Texts.Get("Recipe", key, args);

    private static Window NewWindow(string title) => new()
    {
        Title = title,
        Icon = AppIcons.Window,
        SizeToContent = SizeToContent.WidthAndHeight,
        CanResize = false,
        Topmost = true,
        WindowStartupLocation = WindowStartupLocation.CenterScreen,
    };

    private static Image? Preview(SKBitmap? preview, out IDisposable? owned)
    {
        owned = null;
        if (preview == null)
        {
            return null;
        }

        var bitmap = BitmapConverter.ToAvalonia(preview);
        owned = bitmap;
        return new Image { Source = bitmap, MaxWidth = 420, MaxHeight = 260, Stretch = Stretch.Uniform };
    }

    /// <summary>
    /// Counts down in the text and answers when the time is up
    /// </summary>
    private static void StartTimeout(Window window, int seconds, TextBlock label, Action expired)
    {
        if (seconds <= 0)
        {
            return;
        }

        int left = seconds;
        label.Text = T("prompt_timeout", left);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            left--;
            label.Text = T("prompt_timeout", left);
            if (left <= 0)
            {
                timer.Stop();
                expired();
            }
        };
        window.Closed += (_, _) => timer.Stop();
        timer.Start();
    }

    public static Task<string> PromptAsync(string title, string message, IReadOnlyList<PromptChoice> choices, SKBitmap? preview, int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var result = new TaskCompletionSource<string>();
        var window = NewWindow(string.IsNullOrEmpty(title) ? T("prompt_window_title") : title);
        var fallback = choices.FirstOrDefault(c => c.IsDefault) ?? choices.FirstOrDefault();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var choice in choices.Count > 0 ? choices : [new PromptChoice("Yes", T("prompt_yes"), isDefault: true), new PromptChoice("No", T("prompt_no"), isCancel: true)])
        {
            var button = new Button { Content = choice.Label ?? choice.Key, IsDefault = choice.IsDefault, IsCancel = choice.IsCancel };
            if (string.Equals(choice.Style, "Danger", StringComparison.OrdinalIgnoreCase))
            {
                button.Foreground = Brushes.IndianRed;
            }

            button.Click += (_, _) =>
            {
                result.TrySetResult(choice.Key);
                window.Close();
            };
            buttons.Children.Add(button);
        }

        var countdown = Ui.Note(string.Empty);
        var page = Ui.Page(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 480 });
        var image = Preview(preview, out var owned);
        if (image != null)
        {
            page.Children.Add(image);
        }

        page.Children.Add(countdown);
        page.Children.Add(buttons);
        window.Content = page;
        window.Closed += (_, _) =>
        {
            owned?.Dispose();
            result.TrySetResult(fallback?.Key!);
        };
        StartTimeout(window, timeoutSeconds, countdown, () => window.Close());
        var registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(window.Close));
        _ = result.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        window.Show();
        window.Activate();
        return result.Task;
    }

    /// <summary>
    /// The DynamicDestinationWindow of Windows: the destinations, the editor and the recipes to forward the capture to
    /// </summary>
    public static Task<FlyoutChoice> FlyoutAsync(string title, SKBitmap? preview, IReadOnlyList<IDestination> destinations, IReadOnlyList<CaptureRecipe> recipes,
        string? lastError, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var result = new TaskCompletionSource<FlyoutChoice>();
        var window = NewWindow(title);
        void Choose(FlyoutChoice choice)
        {
            result.TrySetResult(choice);
            window.Close();
        }

        var page = Ui.Page();
        if (!string.IsNullOrEmpty(lastError))
        {
            page.Children.Add(new TextBlock { Text = lastError, Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap, MaxWidth = 480 });
            page.Children.Add(Ui.Note(T("picker_error_subtitle")));
        }

        var image = Preview(preview, out var owned);
        if (image != null)
        {
            page.Children.Add(image);
        }

        var destinationPanel = new WrapPanel { MaxWidth = 520 };
        foreach (var destination in destinations)
        {
            var button = new Button { Content = destination.DisplayName, Margin = new Thickness(0, 0, 6, 6), IsEnabled = destination.IsAvailable };
            button.Click += (_, _) => Choose(new FlyoutChoice(destination, null!, false));
            destinationPanel.Children.Add(button);
        }

        page.Children.Add(Ui.Section(T("picker_destinations"), destinationPanel));
        if (recipes.Count > 0)
        {
            var recipePanel = new WrapPanel { MaxWidth = 520 };
            foreach (var recipe in recipes)
            {
                var button = new Button { Content = recipe.Name ?? recipe.Id, Margin = new Thickness(0, 0, 6, 6) };
                ToolTip.SetTip(button, recipe.Description);
                button.Click += (_, _) => Choose(new FlyoutChoice(null!, recipe, false));
                recipePanel.Children.Add(button);
            }

            page.Children.Add(Ui.Section(T("picker_run_recipe"), recipePanel));
        }

        var countdown = Ui.Note(string.Empty);
        page.Children.Add(countdown);
        var cancel = new Button { Content = Texts.Linux("cancel"), IsCancel = true, HorizontalAlignment = HorizontalAlignment.Right };
        cancel.Click += (_, _) => window.Close();
        page.Children.Add(cancel);
        window.Content = page;
        window.Closed += (_, _) =>
        {
            owned?.Dispose();
            result.TrySetResult(null!);
        };
        StartTimeout(window, timeoutSeconds, countdown, () => window.Close());
        var registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(window.Close));
        _ = result.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        window.Show();
        window.Activate();
        return result.Task;
    }

    public static async Task<bool?> ConfirmAsync(string title, string message)
    {
        int choice = await ChooseAsync(null, title, message, [Texts.Linux("yes"), Texts.Linux("no")]);
        return choice switch
        {
            0 => true,
            1 => false,
            _ => null,
        };
    }

    /// <summary>
    /// A question with the choices; the index of the choice, -1 when the window was closed
    /// </summary>
    public static Task<int> ChooseAsync(Window? owner, string title, string message, IReadOnlyList<string> choices)
    {
        var result = new TaskCompletionSource<int>();
        var window = NewWindow(title);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        for (int i = 0; i < choices.Count; i++)
        {
            int index = i;
            var button = new Button { Content = choices[i], IsCancel = i == choices.Count - 1 };
            button.Click += (_, _) =>
            {
                result.TrySetResult(index);
                window.Close();
            };
            buttons.Children.Add(button);
        }

        window.Content = Ui.Page(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 480 }, buttons);
        window.Closed += (_, _) => result.TrySetResult(-1);
        if (owner != null)
        {
            window.ShowDialog(owner);
        }
        else
        {
            window.Show();
            window.Activate();
        }

        return result.Task;
    }
}
