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
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Greenshot.Editor.Standalone.Views
{
    /// <summary>
    /// A question or a message of the editor with its answers as buttons
    /// </summary>
    public sealed class MessageDialog : Window
    {
        private int _answer;

        private MessageDialog(string title, string text, IReadOnlyList<string> buttons, int defaultButton, int cancelButton)
        {
            _answer = cancelButton;
            Title = title;
            SizeToContent = SizeToContent.WidthAndHeight;
            CanResize = false;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            MinWidth = 320;
            MaxWidth = 560;
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(0, 20, 0, 0) };
            for (int i = 0; i < buttons.Count; i++)
            {
                int index = i;
                var button = new Button { Content = buttons[i], MinWidth = 88, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = i == defaultButton, IsCancel = i == cancelButton };
                if (i == defaultButton)
                {
                    button.Classes.Add("accent");
                }

                button.Click += (_, _) =>
                {
                    _answer = index;
                    Close();
                };
                row.Children.Add(button);
            }

            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Children =
                {
                    new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
                    row
                }
            };
        }

        /// <summary>
        /// Shows the dialog over the owner and returns the index of the button which was chosen (closing it is the cancel button)
        /// </summary>
        public static async Task<int> ShowAsync(Window owner, string title, string text, IReadOnlyList<string> buttons, int defaultButton = 0, int cancelButton = -1)
        {
            var dialog = new MessageDialog(title, text, buttons, defaultButton, cancelButton < 0 ? buttons.Count - 1 : cancelButton);
            await dialog.ShowDialog(owner);
            return dialog._answer;
        }
    }
}
