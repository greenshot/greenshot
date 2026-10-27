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
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace Greenshot.Linux.RecipeEditing;

/// <summary>
/// The hotkey of a trigger, the HotkeyDisplayView and HotkeyEditorView of Windows in one: a click into the box and the
/// keys pressed are taken ("Ctrl + Shift + R", the notation of the Windows version), Backspace or Delete clears it.
/// Text can also be typed, e.g. a chord sequence "Ctrl + K, C".
/// </summary>
public sealed class HotkeyBox : TextBox
{
    public static readonly StyledProperty<string?> HotkeyProperty =
        AvaloniaProperty.Register<HotkeyBox, string?>(nameof(Hotkey), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    protected override Type StyleKeyOverride => typeof(TextBox);

    public HotkeyBox()
    {
        PlaceholderText = "Ctrl + Shift + …";
        FontFamily = new Avalonia.Media.FontFamily("monospace");
        TextChanged += (_, _) =>
        {
            if (Text != Hotkey)
            {
                Hotkey = Text;
            }
        };
    }

    public string? Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HotkeyProperty && Text != Hotkey)
        {
            Text = Hotkey;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Tab or Key.Escape || (e.KeyModifiers == KeyModifiers.None && e.Key is not (Key.Back or Key.Delete) && !IsFunctionKey(e.Key)))
        {
            // Plain keys are typed, Tab moves on
            base.OnKeyDown(e);
            return;
        }

        if (e.Key is Key.Back or Key.Delete && e.KeyModifiers == KeyModifiers.None)
        {
            Text = string.Empty;
            e.Handled = true;
            return;
        }

        string? key = KeyName(e.Key);
        if (key == null)
        {
            // Only a modifier so far
            e.Handled = true;
            return;
        }

        var parts = new List<string>();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Meta)) parts.Add("Win");
        parts.Add(key);
        Text = string.Join(" + ", parts);
        CaretIndex = Text.Length;
        e.Handled = true;
    }

    private static bool IsFunctionKey(Key key) => key is >= Key.F1 and <= Key.F24 or Key.PrintScreen or Key.Pause or Key.Scroll;

    /// <summary>
    /// The name of the key like Windows writes it, null for a modifier
    /// </summary>
    private static string? KeyName(Key key) => key switch
    {
        Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.None => null,
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        Key.PrintScreen => "PrintScreen",
        Key.Return => "Enter",
        Key.Next => "PageDown",
        Key.Prior => "PageUp",
        _ => key.ToString(),
    };
}

/// <summary>
/// A button with the common emoji, the EmojiPicker of the Windows editor; the box next to it takes any emoji
/// </summary>
public sealed class EmojiPickerButton : Button
{
    public static readonly StyledProperty<string?> SelectionProperty =
        AvaloniaProperty.Register<EmojiPickerButton, string?>(nameof(Selection), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    private static readonly string[] Common =
    [
        "🛡️", "⚠️", "❗", "❓", "✅", "❌", "⭐", "💡", "🔥", "👍", "👎", "👉", "👆", "👇", "👈", "🎯", "📌", "📎", "🔒", "🔓",
        "🐞", "🐛", "🚀", "💬", "📷", "🖼️", "📝", "📋", "🔍", "⏱️", "🕒", "📅", "✏️", "🎨", "🔔", "💾", "📁", "🔗", "😀", "😉",
        "😂", "😍", "🤔", "😮", "😢", "😡", "🙏", "👏", "💯", "🆕",
    ];

    protected override Type StyleKeyOverride => typeof(Button);

    public EmojiPickerButton()
    {
        Padding = new Thickness(4, 0);
        MinHeight = 0;
        Background = Avalonia.Media.Brushes.Transparent;
        var panel = new WrapPanel { Width = 280 };
        foreach (string emoji in Common)
        {
            var button = new Button
            {
                Content = emoji,
                FontSize = 18,
                Padding = new Thickness(3),
                Background = Avalonia.Media.Brushes.Transparent,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };
            AutomationProperties.SetName(button, emoji);
            button.Click += (_, _) =>
            {
                Selection = emoji;
                Flyout?.Hide();
            };
            panel.Children.Add(button);
        }

        Flyout = new Flyout { Content = panel, Placement = PlacementMode.Bottom };
        UpdateContent();
    }

    public string? Selection
    {
        get => GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectionProperty)
        {
            UpdateContent();
        }
    }

    private void UpdateContent() => Content = new TextBlock { Text = string.IsNullOrEmpty(Selection) ? "🙂" : Selection, FontSize = 16 };
}
