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
using Image = System.Drawing.Image;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.SmartText.Detection;
using Greenshot.Plugin.SmartText.ViewModels;
using log4net;

namespace Greenshot.Plugin.SmartText.Views;

/// <summary>
/// The Smart text window: pick web addresses, phone numbers, own patterns and more from the text of a capture.
/// In copy mode (the destination, the built-in recipe) the picked values go to the clipboard, in pick mode (a recipe asks) they
/// are the result. See docs/vNext/smart-text.md.
/// </summary>
public partial class SmartTextWindow : Window
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextWindow));
    private readonly SmartTextWindowViewModel _viewModel;

    internal SmartTextWindow(SmartTextWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        InitializeComponent();
        Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
        Loaded += (_, _) => FocusStart();
    }

    /// <summary>What was picked with Use, null: closed without</summary>
    internal IReadOnlyList<SmartTextItem> Result { get; private set; }

    /// <summary>
    /// Let the user pick items, on the UI thread; the window is modal and a cancelled flow closes it
    /// </summary>
    /// <returns>the picked items with the corrected values, null when the window was closed without</returns>
    internal static IReadOnlyList<SmartTextItem> Pick(Image capture, TextLayout layout, IReadOnlyList<SmartTextItem> items, CancellationToken cancellationToken)
    {
        var window = new SmartTextWindow(new SmartTextWindowViewModel(items, PreviewOf(capture), AllTextOf(layout), isPickMode: true));
        using (cancellationToken.Register(() => window.Dispatcher.InvokeAsync(() =>
               {
                   if (window.IsVisible) window.Close();
               }).Task.FireAndLog("Close the Smart text window", Log)))
        {
            window.ShowDialog();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return window.Result;
    }

    /// <summary>
    /// Show the items to copy or open, on the UI thread; the window stays open next to Greenshot
    /// </summary>
    internal static void ShowToCopy(Image capture, TextLayout layout, IReadOnlyList<SmartTextItem> items)
    {
        var window = new SmartTextWindow(new SmartTextWindowViewModel(items, PreviewOf(capture), AllTextOf(layout), isPickMode: false));
        window.Show();
        window.Activate();
    }

    /// <summary>The whole OCR text, line by line</summary>
    internal static string AllTextOf(TextLayout layout) =>
        layout == null ? string.Empty : string.Join(Environment.NewLine, layout.Lines.Select(line => line.Text)).Trim();

    /// <summary>A frozen copy of the capture: the window doesn't depend on the lifetime of the image</summary>
    private static ImageSource PreviewOf(Image capture)
    {
        if (capture == null) return null;
        try
        {
            var source = capture.ToBitmapSource();
            source?.Freeze();
            return source;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or OutOfMemoryException)
        {
            Log.Warn("Can't show the capture in the Smart text window", ex);
            return null;
        }
    }

    /// <summary>Focus starts on the first item, or on the text which says why there is none</summary>
    private void FocusStart()
    {
        if (!_viewModel.HasItems)
        {
            EmptyBlock.Focus();
            return;
        }

        FocusSelected();
    }

    private void FocusSelected()
    {
        ItemList.UpdateLayout();
        if (_viewModel.SelectedItem == null) return;
        ItemList.ScrollIntoView(_viewModel.SelectedItem);
        if (ItemList.ItemContainerGenerator.ContainerFromItem(_viewModel.SelectedItem) is ListBoxItem container)
        {
            container.Focus();
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled || Keyboard.FocusedElement is TextBox) return;
        bool control = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        if (control && e.Key == Key.A)
        {
            _viewModel.CheckAll();
            e.Handled = true;
        }
        else if (control && e.Key == Key.C)
        {
            CopyPicked();
            e.Handled = true;
        }
        else if (control && e.Key == Key.O)
        {
            OpenAction(_viewModel.SelectedItem);
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.None && e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            // 1-9 jump to the item with the number
            int number = e.Key - Key.D0;
            var item = _viewModel.ItemsView.Cast<SmartTextItemViewModel>().FirstOrDefault(candidate => candidate.Number == number);
            if (item != null)
            {
                _viewModel.SelectedItem = item;
                FocusSelected();
                e.Handled = true;
            }
        }
    }

    private void ItemList_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space when _viewModel.SelectedItem != null:
                _viewModel.SelectedItem.IsChecked = !_viewModel.SelectedItem.IsChecked;
                e.Handled = true;
                break;
            case Key.Enter:
                Take();
                e.Handled = true;
                break;
            case Key.F2 when _viewModel.SelectedItem != null:
                ValueBox.Focus();
                ValueBox.SelectAll();
                e.Handled = true;
                break;
        }
    }

    private void ValueBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Take();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Back to the list, Esc again closes the window
            FocusSelected();
            e.Handled = true;
        }
    }

    private void ItemList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(ItemList, source) is ListBoxItem)
        {
            Take();
        }
    }

    private void Box_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SmartTextItemViewModel item)
        {
            if (!_viewModel.ItemsView.Contains(item)) _viewModel.SelectedFilter = _viewModel.Filters[0];
            _viewModel.SelectedItem = item;
            FocusSelected();
            if (e.ClickCount == 2) Take();
            e.Handled = true;
        }
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is SmartTextFilterViewModel filter)
        {
            _viewModel.SelectedFilter = filter;
        }
    }

    private void Action_Click(object sender, RoutedEventArgs e)
    {
        OpenAction((sender as FrameworkElement)?.DataContext as SmartTextItemViewModel);
    }

    private void OpenAction(SmartTextItemViewModel item)
    {
        if (item?.ActionTarget != null && !SmartTextActions.Open(item.ActionTarget))
        {
            ThemedMessageBox.Show(this, item.ActionTarget, SmartTextPlugin.Language.WindowTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Pick_Click(object sender, RoutedEventArgs e) => Take();

    /// <summary>
    /// Use (pick mode): the picked items are the result; Copy: the picked values go to the clipboard and the window closes
    /// </summary>
    private void Take()
    {
        if (!_viewModel.CanPick) return;
        if (_viewModel.IsPickMode)
        {
            Result = _viewModel.PickedItems();
            Close();
            return;
        }

        if (CopyPicked())
        {
            Close();
        }
    }

    private bool CopyPicked()
    {
        if (!_viewModel.CanPick) return false;
        return Copy(_viewModel.PickedText());
    }

    private void CopyAllText_Click(object sender, RoutedEventArgs e) => Copy(_viewModel.AllText);

    private bool Copy(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        ClipboardService.Current.SetTextAsync(text).FireAndLog("Copy Smart text to the clipboard", Log);
        _viewModel.Status = SmartTextPlugin.Language.Copied;
        return true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
