//
//  Emoji.Wpf — Emoji support for WPF
//
//  Copyright © 2017—2021 Sam Hocevar <sam@hocevar.net>
//
//  This program is free software. It comes without any warranty, to
//  the extent permitted by applicable law. You can redistribute it
//  and/or modify it under the terms of the Do What the Fuck You Want
//  to Public License, Version 2, as published by the WTFPL Task Force.
//  See http://www.wtfpl.net/ for more details.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Greenshot.Base.Threading;

namespace Greenshot.Base.Wpf.Emoji;

/// <summary>
/// The emoji picker, its data and the pick are in <see cref="EmojiPickerViewModel"/>
/// </summary>
public partial class EmojiPicker : UserControl
{
    /// <summary>
    /// The width of an emoji button
    /// </summary>
    private const double CellWidth = 40;

    public EmojiPicker()
    {
        InitializeComponent();
    }

    private EmojiPickerViewModel ViewModel => DataContext as EmojiPickerViewModel;

    /// <summary>
    /// As many emoji in a row as fit, the vertical scroll bar takes some room
    /// </summary>
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ViewModel is { } viewModel && e.WidthChanged)
        {
            viewModel.Columns = Math.Max(4, (int)((ActualWidth - SystemParameters.VerticalScrollBarWidth - 4) / CellWidth));
        }
    }

    private void OnEmojiPicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: Emojis.Emoji emoji })
        {
            return;
        }

        e.Handled = true;
        // An emoji with variations opens them first, the buttons in that popup pick
        if (emoji.HasVariations && FindChild<Popup>((DependencyObject)sender) is { } variations)
        {
            variations.IsOpen = true;
            return;
        }

        // The variation popup closes after a pick
        if (FindParent<Popup>((DependencyObject)sender) is { } popup)
        {
            popup.IsOpen = false;
        }

        bool fromKeyboard = InputManager.Current.MostRecentInputDevice is KeyboardDevice;
        ViewModel?.Pick(emoji.Text, fromKeyboard);
    }

    /// <summary>
    /// Enter in the search picks the first emoji found, the down arrow goes to the found emoji
    /// </summary>
    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel == null || !viewModel.IsSearching)
        {
            return;
        }

        if (e.Key == Key.Enter && viewModel.SearchRows.FirstOrDefault()?.FirstOrDefault() is { } first)
        {
            viewModel.Pick(first.Text, fromKeyboard: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Down && FindVisibleEmojiButton() is { } button)
        {
            button.Focus();
            e.Handled = true;
        }
    }

    private ButtonBase FindVisibleEmojiButton()
        => FindChildren<ListView>(this).Where(list => list.IsVisible).Select(FindChild<ButtonBase>).FirstOrDefault(button => button != null);

    /// <summary>
    /// The arrow keys move between the emoji of a category, Home and End go to the start and end of the row.
    /// The rows are virtualized, so the target row is scrolled into view before it gets the focus.
    /// </summary>
    private void OnEmojiGridKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not ListView list || Keyboard.Modifiers != ModifierKeys.None
            || e.OriginalSource is not FrameworkElement { DataContext: Emojis.Emoji emoji } source
            // The keys of the variations popup also pass here, it has its own handler
            || PresentationSource.FromVisual(source) != PresentationSource.FromVisual(list))
        {
            return;
        }

        int columns = 0, total = 0, index = -1;
        foreach (var item in list.Items)
        {
            if (item is not IList<Emojis.Emoji> row) continue;
            columns = Math.Max(columns, row.Count);
            int column = row.IndexOf(emoji);
            if (column >= 0 && index < 0)
            {
                index = total + column;
            }

            total += row.Count;
        }

        if (index < 0 || columns == 0)
        {
            return;
        }

        int target = e.Key switch
        {
            Key.Left => index - 1,
            Key.Right => index + 1,
            Key.Up => index - columns,
            Key.Down => index + columns,
            Key.Home => index - index % columns,
            Key.End => Math.Min(total - 1, index - index % columns + columns - 1),
            _ => int.MinValue
        };

        if (target == int.MinValue)
        {
            return;
        }

        // Also at the edges, so the list doesn't scroll or move the focus somewhere else
        e.Handled = true;
        if (target < 0 || target >= total || target == index)
        {
            // Up from the first row of the search results goes back to the search field
            if (target < 0 && e.Key == Key.Up && ViewModel?.IsSearching == true)
            {
                SearchBox.Focus();
            }

            return;
        }

        int rowIndex = target / columns;
        list.ScrollIntoView(list.Items[rowIndex]);
        list.UpdateLayout();
        if (list.ItemContainerGenerator.ContainerFromIndex(rowIndex) is DependencyObject rowContainer
            && FindChild<ItemsControl>(rowContainer) is { } cells
            && cells.ItemContainerGenerator.ContainerFromIndex(target % columns) is DependencyObject cell
            && FindChild<ButtonBase>(cell) is { } button)
        {
            button.Focus();
        }
    }

    /// <summary>
    /// The variations of an emoji (skin tones) open in their own popup, the first one gets the focus
    /// </summary>
    private void OnVariationPopupOpened(object sender, EventArgs e)
    {
        if (sender is not Popup { Child: UIElement child }) return;
        Dispatcher.InvokeAsync(() => FindChild<ButtonBase>(child)?.Focus(), DispatcherPriority.Loaded).Task.FireAndLog("Focus the emoji variation");
    }

    private void OnVariationKeyDown(object sender, KeyEventArgs e)
    {
        var direction = e.Key switch
        {
            Key.Left => FocusNavigationDirection.Left,
            Key.Right => FocusNavigationDirection.Right,
            Key.Up => FocusNavigationDirection.Up,
            Key.Down => FocusNavigationDirection.Down,
            _ => (FocusNavigationDirection?)null
        };

        if (e.Key == Key.Escape && FindParent<Popup>((DependencyObject)sender) is { } popup)
        {
            popup.IsOpen = false;
            e.Handled = true;
            return;
        }

        if (direction == null || Keyboard.Modifiers != ModifierKeys.None || e.OriginalSource is not UIElement focused)
        {
            return;
        }

        focused.MoveFocus(new TraversalRequest(direction.Value));
        e.Handled = true;
    }

    private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        => FindChildren<T>(parent).FirstOrDefault();

    private static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
            {
                yield return found;
                continue;
            }

            foreach (var deeper in FindChildren<T>(child))
            {
                yield return deeper;
            }
        }
    }

    /// <summary>
    /// The popup of a button inside a popup: the popup's content has no visual parent, its logical parent is the popup
    /// </summary>
    private static T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        for (var current = child; current != null;)
        {
            if (current is T found)
            {
                return found;
            }

            var logical = LogicalTreeHelper.GetParent(current);
            if (logical is T logicalFound)
            {
                return logicalFound;
            }

            current = (current is Visual ? VisualTreeHelper.GetParent(current) : null) ?? logical;
        }

        return null;
    }
}
