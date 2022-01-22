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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Greenshot.Editor.Controls
{
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
        public EmojiPicker()
        {
            InitializeComponent();
        }

        public IList<EmojiData.Group> EmojiGroups => EmojiData.AllGroups;

        // Backwards compatibility for when the backend was a TextBlock.
        public double FontSize
        {
            get => Image.Height * 0.75;
            set => Image.Height = value / 0.75;
        }

        public event PropertyChangedEventHandler SelectionChanged;

        public event EmojiPickedEventHandler Picked;

        private static void OnSelectionPropertyChanged(DependencyObject source,
                                                       DependencyPropertyChangedEventArgs e)
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
            var is_disabled = string.IsNullOrEmpty(s);
            Image.Emoji = is_disabled ? "???" : s;
            Image.Opacity = is_disabled ? 0.3 : 1.0;
            SelectionChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selection)));
        }

        private void OnEmojiPicked(object sender, RoutedEventArgs e)
        {
            if (sender is Control control && control.DataContext is EmojiData.Emoji emoji)
            {
                if (emoji.VariationList.Count == 0 || sender is Button)
                {
                    Selection = emoji.Text;
                    Button_INTERNAL.IsChecked = false;
                    e.Handled = true;
                    Picked?.Invoke(this, new EmojiPickedEventArgs(Selection));
                }
            }
        }

        public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(
            nameof(Selection), typeof(string), typeof(EmojiPicker),
                new FrameworkPropertyMetadata("☺", OnSelectionPropertyChanged));

        private void OnPopupKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && sender is Popup popup)
            {
                popup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void OnPopupLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is Popup popup))
                return;

            var child = popup.Child;
            IInputElement old_focus = null;
            child.Focusable = true;
            child.IsVisibleChanged += (o, ea) =>
            {
                if (child.IsVisible)
                {
                    old_focus = Keyboard.FocusedElement;
                    Keyboard.Focus(child);
                }
            };

            popup.Closed += (o, ea) => Keyboard.Focus(old_focus);
        }
    }
}
