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
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Ini;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Editor.Configuration;

namespace Greenshot.Editor.Controls.Emoji;

/// <summary>
/// The event which is created when the emoji is picked
/// </summary>
public class EmojiPickedEventArgs : EventArgs
{
    public EmojiPickedEventArgs() { }
    public EmojiPickedEventArgs(string emoji) => Emoji = emoji;

    public string Emoji;
}

public delegate void EmojiPickedEventHandler(object sender, EmojiPickedEventArgs e);

/// <summary>
/// Interaction logic for Picker.xaml
/// </summary>
public partial class EmojiPicker : StackPanel
{
    /// <summary>
    /// How many emoji the history keeps
    /// </summary>
    public const int HistorySize = 24;

    public EmojiPicker()
    {
        InitializeComponent();
        Groups = CreateGroups();
    }

    /// <summary>
    /// The tabs of the picker, the history is the first. Set again on every open, so the history is current.
    /// </summary>
    public static readonly DependencyProperty GroupsProperty = DependencyProperty.Register(
        nameof(Groups), typeof(IList<Emojis.Group>), typeof(EmojiPicker));

    public IList<Emojis.Group> Groups
    {
        get => (IList<Emojis.Group>)GetValue(GroupsProperty);
        set => SetValue(GroupsProperty, value);
    }

    /// <summary>
    /// The emoji picked recently, newest first
    /// </summary>
    public static IList<string> HistoryEmojis => (IniConfigRegistry.GetSection<IEditorConfiguration>().RecentEmojis ?? string.Empty)
        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
        .Take(HistorySize)
        .ToList();

    /// <summary>
    /// Put the emoji first in the history, without duplicates, and keep only the last HistorySize
    /// </summary>
    public static void AddToHistory(string emoji)
    {
        if (string.IsNullOrEmpty(emoji))
        {
            return;
        }

        var history = HistoryEmojis.Where(e => e != emoji).Prepend(emoji).Take(HistorySize);
        IniConfigRegistry.GetSection<IEditorConfiguration>().RecentEmojis = string.Join(" ", history);
    }

    private static IList<Emojis.Group> CreateGroups()
    {
        var history = new Emojis.Group
        {
            Name = Texts.Editor.EmojiHistory,
            CustomIcon = "🕒",
            SubGroups =
            {
                new Emojis.Group
                {
                    Emojis = HistoryEmojis.Select(text => new Emojis.Emoji { Text = text }).ToList()
                }
            }
        };

        return new List<Emojis.Group> { history }.Concat(EmojiData.Data.Groups).ToList();
    }

    /// <summary>
    /// Without history the first category is shown, the history tab is still there
    /// </summary>
    private bool HistoryIsEmpty => HistoryEmojis.Count == 0;

    // Backwards compatibility for when the backend was a TextBlock.
    public double FontSize
    {
        get => Image.Height * 0.75;
        set => Image.Height = value / 0.75;
    }

    public event PropertyChangedEventHandler SelectionChanged;

    public event EmojiPickedEventHandler Picked;

    /// <summary>
    /// The popup with the emoji closed, after a pick, Esc or a click somewhere else
    /// </summary>
    public event EventHandler Closed;

    private static void OnSelectionPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
    {
        (source as EmojiPicker)?.OnSelectionChanged(e.NewValue as string);
    }

    public string Selection
    {
        get => (string)GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    private void OnSelectionChanged(string s)
    {
        var isDisabled = string.IsNullOrEmpty(s);
        Image.Emoji = isDisabled ? "???" : s;
        Image.Opacity = isDisabled ? 0.3 : 1.0;
        SelectionChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selection)));
    }

    private void OnEmojiPicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: Emojis.Emoji emoji })
        {
            return;
        }

        // An emoji with variations opens them first, the buttons in that popup pick
        if (emoji.HasVariations && FindChild<Popup>((DependencyObject)sender) is { } variations)
        {
            variations.IsOpen = true;
            e.Handled = true;
            return;
        }

        Selection = emoji.Text;
        Button_INTERNAL.IsChecked = false;
        e.Handled = true;
        Picked?.Invoke(this, new EmojiPickedEventArgs(Selection));
    }

    public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(
        nameof(Selection), typeof(string), typeof(EmojiPicker),
        new FrameworkPropertyMetadata("☺", OnSelectionPropertyChanged));

    private void OnPopupKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || sender is not Popup popup) return;
        popup.IsOpen = false;
        e.Handled = true;
    }

    private bool _popupInitialized;
    private IInputElement _oldFocus;

    private void OnPopupLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Popup popup) return;

        // Only subscribe event handlers once to prevent accumulating lambda subscriptions
        if (_popupInitialized) return;
        _popupInitialized = true;

        var child = popup.Child;
        child.Focusable = true;
        child.IsVisibleChanged += OnPopupChildVisibilityChanged;
        popup.Closed += OnPopupClosed;
    }

    private void OnPopupChildVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not UIElement child || !child.IsVisible) return;
        _oldFocus = Keyboard.FocusedElement;

        // Start on the tab of the category, Tab goes on to its emoji.
        // The tabs exist only after the popup's first layout, which must not be forced while the popup is still opening.
        Dispatcher.InvokeAsync(() =>
        {
            var tabs = FindChild<TabControl>(child);
            if (tabs != null)
            {
                // The history is the first tab, with an empty history the first category (the second tab) is shown
                tabs.SelectedIndex = HistoryIsEmpty ? Math.Min(1, tabs.Items.Count - 1) : 0;
            }

            if (tabs?.ItemContainerGenerator.ContainerFromIndex(Math.Max(0, tabs.SelectedIndex)) is TabItem tab)
            {
                tab.Focus();
            }
            else
            {
                Keyboard.Focus(child);
            }
        }, DispatcherPriority.Loaded).Task.FireAndLog("Focus the emoji category");
    }

    private void OnPopupClosed(object sender, EventArgs e)
    {
        Keyboard.Focus(_oldFocus);
        Closed?.Invoke(this, EventArgs.Empty);
    }

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

        if (direction == null || Keyboard.Modifiers != ModifierKeys.None || e.OriginalSource is not UIElement focused)
        {
            return;
        }

        focused.MoveFocus(new TraversalRequest(direction.Value));
        e.Handled = true;
    }

    private static T FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
            {
                return found;
            }

            if (FindChild<T>(child) is { } deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    public void ShowPopup(bool show)
    {
        if (show)
        {
            // The history changes on every pick, so the tabs are built again on every open
            Groups = CreateGroups();
        }

        foreach (var child in Children)
        {
            if (child is ToggleButton button)
            {
                button.IsChecked = show;
            }
        }
    }
}
