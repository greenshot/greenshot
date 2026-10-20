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
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// A message box in Greenshot's light or dark theme, for the WPF windows (recipe editor, recipe manager, ...).
    /// Show works like <see cref="MessageBox.Show(string, string, MessageBoxButton, MessageBoxImage)"/>; ShowChoice takes own button texts.
    /// </summary>
    public sealed class ThemedMessageBox : Window
    {
        private int _choice = -1;
        private readonly int _cancelIndex;

        private readonly List<Button> _buttons = new List<Button>();
        private Button _defaultButton;

        private ThemedMessageBox(string caption, string text, MessageBoxImage icon, IReadOnlyList<string> buttons, int defaultIndex, int cancelIndex)
        {
            _cancelIndex = cancelIndex;
            ThemedControls.ApplyDialogLook(this, caption);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            MinWidth = 360;
            MaxWidth = 640;

            var header = ThemedControls.CreateHeader(this, caption);
            // A screen reader says the title and the focused button, the question itself only from here
            AutomationProperties.SetHelpText(this, text ?? string.Empty);

            var message = new TextBlock
            {
                Text = text ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
                Foreground = WpfThemeHelper.TextPrimary,
                VerticalAlignment = VerticalAlignment.Center
            };
            message.SetBinding(TextBlock.FontSizeProperty, FontSizeExtension.CreateBinding(12));

            var body = new DockPanel { LastChildFill = true };
            string glyph = GetGlyph(icon);
            if (glyph != null)
            {
                var iconText = new TextBlock
                {
                    Text = glyph,
                    FontSize = 24,
                    Margin = new Thickness(0, 0, 14, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    Foreground = icon == MessageBoxImage.Error ? WpfThemeHelper.ErrorText
                        : icon == MessageBoxImage.Warning ? WpfThemeHelper.WarningText
                        : WpfThemeHelper.Accent
                };
                // Not the emoji: its meaning
                AutomationProperties.SetName(iconText, GetIconName(icon));
                DockPanel.SetDock(iconText, Dock.Left);
                body.Children.Add(iconText);
            }
            body.Children.Add(new ScrollViewer
            {
                Content = message,
                MaxHeight = 420,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            });

            var buttonRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i;
                bool isDefault = i == defaultIndex;
                var button = ThemedControls.CreateButton(buttons[i], isDefault, i == cancelIndex);
                button.Click += (s, e) =>
                {
                    _choice = index;
                    Close();
                };
                buttonRow.Children.Add(button);
                _buttons.Add(button);
                if (isDefault)
                {
                    _defaultButton = button;
                    Loaded += (s, e) => button.Focus();
                }
            }

            var root = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
            root.Children.Add(header);
            root.Children.Add(body);
            root.Children.Add(buttonRow);
            Content = root;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_choice < 0)
            {
                // Closed with Escape or Alt+F4: the cancel button
                _choice = _cancelIndex;
            }
        }

        /// <summary>
        /// The buttons only react after this time: a keystroke or click meant for another program can't answer the question
        /// </summary>
        private void ArmButtonsAfter(TimeSpan delay)
        {
            foreach (var button in _buttons)
            {
                button.IsEnabled = false;
            }
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = delay };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                foreach (var button in _buttons)
                {
                    button.IsEnabled = true;
                }

                // A disabled button can't take the focus when the window is shown: the keyboard starts on the default button now
                _defaultButton?.Focus();
            };
            ContentRendered += (s, e) => timer.Start();
            Closed += (s, e) => timer.Stop();
        }

        /// <summary>
        /// Shows the message with own button texts; returns the index of the chosen button, or cancelIndex when the box was closed.
        /// With onTop the box is shown over all windows, also those of other programs (for questions which come from outside).
        /// </summary>
        /// <param name="owner">The owner window, found when null</param>
        /// <param name="caption">Caption</param>
        /// <param name="text">The message</param>
        /// <param name="icon">Icon</param>
        /// <param name="buttons">The button texts</param>
        /// <param name="defaultIndex">The button Enter presses, -1 for none</param>
        /// <param name="cancelIndex">The button Escape presses and the result when the box is closed; -1: Escape does nothing</param>
        /// <param name="onTop">Over all windows, also of other programs</param>
        /// <param name="takeFocus">With onTop: false shows the box without taking the keyboard from the program the user types in</param>
        /// <param name="armDelay">The buttons are disabled for this time after the box is shown</param>
        public static int ShowChoice(Window owner, string caption, string text, MessageBoxImage icon, IReadOnlyList<string> buttons, int defaultIndex = 0, int cancelIndex = -1,
            bool onTop = false, bool takeFocus = true, TimeSpan? armDelay = null)
        {
            var box = new ThemedMessageBox(caption, text, icon, buttons, defaultIndex, cancelIndex);
            if (armDelay > TimeSpan.Zero)
            {
                box.ArmButtonsAfter(armDelay.Value);
            }
            owner ??= onTop ? null : FindOwner();
            if (onTop)
            {
                box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                box.Topmost = true;
                box.ShowInTaskbar = true;
                if (takeFocus)
                {
                    box.Loaded += (s, e) => box.Activate();
                }
                else
                {
                    box.ShowActivated = false;
                }
            }
            else if (owner != null && owner.IsVisible)
            {
                box.Owner = owner;
            }
            else
            {
                box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                box.Topmost = true;
            }
            box.ShowDialog();
            return box._choice;
        }

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
            => Show(null, text, caption, button, icon);

        public static MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
        {
            (string Text, MessageBoxResult Result)[] choices = button switch
            {
                MessageBoxButton.OKCancel => new[] { ("OK", MessageBoxResult.OK), ("Cancel", MessageBoxResult.Cancel) },
                MessageBoxButton.YesNo => new[] { ("Yes", MessageBoxResult.Yes), ("No", MessageBoxResult.No) },
                MessageBoxButton.YesNoCancel => new[] { ("Yes", MessageBoxResult.Yes), ("No", MessageBoxResult.No), ("Cancel", MessageBoxResult.Cancel) },
                _ => new[] { ("OK", MessageBoxResult.OK) }
            };
            int cancelIndex = Array.FindIndex(choices, c => c.Result == MessageBoxResult.Cancel || c.Result == MessageBoxResult.No);
            if (button == MessageBoxButton.YesNoCancel)
            {
                cancelIndex = 2;
            }
            if (cancelIndex < 0)
            {
                cancelIndex = 0;
            }
            int choice = ShowChoice(owner, caption, text, icon, choices.Select(c => c.Text).ToList(), 0, cancelIndex);
            return choice >= 0 && choice < choices.Length ? choices[choice].Result : MessageBoxResult.None;
        }

        /// <summary>
        /// Shows the message with own button texts over a window which isn't a WPF window (e.g. a WinForms form);
        /// returns the index of the chosen button, or cancelIndex when the box was closed.
        /// </summary>
        /// <param name="ownerHandle">The handle of the owner window, the box is centered on it</param>
        /// <param name="caption">Caption</param>
        /// <param name="text">The message</param>
        /// <param name="icon">Icon</param>
        /// <param name="buttons">The button texts</param>
        /// <param name="defaultIndex">The button Enter presses, -1 for none</param>
        /// <param name="cancelIndex">The button Escape presses and the result when the box is closed; -1: Escape does nothing</param>
        public static int ShowChoice(IntPtr ownerHandle, string caption, string text, MessageBoxImage icon, IReadOnlyList<string> buttons, int defaultIndex = 0, int cancelIndex = -1)
        {
            var box = new ThemedMessageBox(caption, text, icon, buttons, defaultIndex, cancelIndex);
            if (ownerHandle != IntPtr.Zero)
            {
                _ = new System.Windows.Interop.WindowInteropHelper(box) { Owner = ownerHandle };
                box.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                box.Topmost = true;
            }
            box.ShowDialog();
            return box._choice;
        }

        private static Window FindOwner()
        {
            var windows = Application.Current?.Windows.OfType<Window>().Where(w => w.IsVisible && !(w is ThemedMessageBox)).ToList();
            return windows?.FirstOrDefault(w => w.IsActive) ?? windows?.LastOrDefault();
        }

        private static string GetIconName(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Error: return Texts.Core.Error;
                case MessageBoxImage.Warning: return Texts.Core.Warning;
                case MessageBoxImage.Question: return Texts.Core.AccessibleQuestion;
                default: return Texts.Core.AccessibleInformation;
            }
        }

        private static string GetGlyph(MessageBoxImage icon)
        {
            switch (icon)
            {
                case MessageBoxImage.Error: return "⛔";
                case MessageBoxImage.Warning: return "⚠";
                case MessageBoxImage.Question: return "❓";
                case MessageBoxImage.Information: return "ℹ";
                default: return null;
            }
        }
    }
}
