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
using Avalonia.Layout;
using Avalonia.Media;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Editor.Emoji;

namespace Greenshot.Linux.Editing;

/// <summary>
/// The emoji picker of the editor: a tab per group, the emoji of the group, the skin tones of an emoji in a small popup.
/// The EmojiPicker of the Windows editor; it uses the Twemoji font which draws the emoji elements.
/// </summary>
internal sealed class EmojiPicker
{
    public static readonly FontFamily EmojiFont = new("avares://greenshot/Assets/Fonts#Twemoji Mozilla");

    private const int Columns = 10;

    private readonly Flyout _flyout;
    private Action<string>? _picked;

    public EmojiPicker()
    {
        var tabs = new TabControl { Width = 500, Height = 380, Padding = new Thickness(0) };
        foreach (var group in EmojiData.Groups)
        {
            var tab = new TabItem
            {
                Header = new TextBlock { Text = group.Icon, FontFamily = EmojiFont, FontSize = 20 },
                Padding = new Thickness(6, 2),
                MinHeight = 0,
            };
            AutomationProperties.SetName(tab, group.Name);
            ToolTip.SetTip(tab, group.Name);
            // The emoji of a group are made when it is shown the first time
            tab.Content = new LazyGroup(group, this);
            tabs.Items.Add(tab);
        }

        if (tabs.Items.Count == 0)
        {
            tabs.Items.Add(new TabItem { Header = Texts.Editor("emoji_picker"), Content = new TextBlock { Text = "emoji-test.txt is missing" } });
        }

        _flyout = new Flyout { Content = tabs, Placement = PlacementMode.Pointer, ShowMode = FlyoutShowMode.Standard };
    }

    /// <summary>
    /// Show the picker at the pointer; picked gets the chosen emoji
    /// </summary>
    public void Show(Control target, Action<string> picked)
    {
        _picked = picked;
        _flyout.ShowAt(target, showAtPointer: true);
    }

    private void Pick(string emoji)
    {
        _flyout.Hide();
        _picked?.Invoke(emoji);
    }

    private Button EmojiButton(EmojiEntry entry, Action<Button> click)
    {
        var button = new Button
        {
            Content = new TextBlock { Text = entry.Text, FontFamily = EmojiFont, FontSize = 24 },
            Padding = new Thickness(4),
            Width = 42,
            Height = 42,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = Brushes.Transparent,
        };
        AutomationProperties.SetName(button, entry.Name);
        ToolTip.SetTip(button, entry.Name);
        button.Click += (_, _) => click(button);
        return button;
    }

    /// <summary>
    /// The emoji of a group, made when the tab is shown
    /// </summary>
    private sealed class LazyGroup : ContentControl
    {
        private readonly EmojiGroup _group;
        private readonly EmojiPicker _picker;

        public LazyGroup(EmojiGroup group, EmojiPicker picker)
        {
            _group = group;
            _picker = picker;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (Content != null)
            {
                return;
            }

            var panel = new WrapPanel { Width = Columns * 42, HorizontalAlignment = HorizontalAlignment.Center };
            foreach (var entry in _group.Emojis)
            {
                panel.Children.Add(_picker.EmojiButton(entry, button =>
                {
                    if (entry.Variations.Count == 0)
                    {
                        _picker.Pick(entry.Text);
                        return;
                    }

                    // The skin tones and hair styles
                    var variations = new WrapPanel { MaxWidth = 6 * 42 };
                    var popup = new Flyout { Content = variations, Placement = PlacementMode.Bottom };
                    foreach (var variation in new[] { entry }.Concat(entry.Variations))
                    {
                        variations.Children.Add(_picker.EmojiButton(variation, _ =>
                        {
                            popup.Hide();
                            _picker.Pick(variation.Text);
                        }));
                    }

                    popup.ShowAt(button);
                }));
            }

            Content = new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }
    }
}
