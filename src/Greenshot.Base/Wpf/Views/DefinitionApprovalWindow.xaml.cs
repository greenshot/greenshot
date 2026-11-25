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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Greenshot.Base.Definitions;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Wpf.Views
{
    /// <summary>
    /// Shows a <see cref="DefinitionApproval"/>: icon, name and type, what it does, notes, and the choices of the caller. The last
    /// choice cancels: it is the default of Enter and Escape and the result when the window is closed. The choices react only after
    /// a moment, so a double click elsewhere doesn't decide.
    /// </summary>
    public partial class DefinitionApprovalWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DefinitionApprovalWindow));
        private readonly List<Button> _buttons = new List<Button>();
        private readonly DefinitionApproval _approval;
        private int _choice = -1;

        public DefinitionApprovalWindow(DefinitionApproval approval, IReadOnlyList<string> buttons, TimeSpan armDelay)
        {
            _approval = approval ?? throw new ArgumentNullException(nameof(approval));
            if (buttons == null || buttons.Count < 2) throw new ArgumentException("At least one choice and Cancel", nameof(buttons));
            DataContext = approval;
            InitializeComponent();
            int cancel = buttons.Count - 1;
            for (int index = 0; index < buttons.Count; index++)
            {
                int choice = index;
                var button = new Button
                {
                    Content = buttons[index],
                    Style = (Style)FindResource(index == 0 ? "PrimaryButtonStyle" : "SecondaryButtonStyle"),
                    Margin = new Thickness(index == 0 ? 0 : 6, 0, 0, 0),
                    IsDefault = index == cancel,
                    IsCancel = index == cancel,
                    IsEnabled = armDelay <= TimeSpan.Zero
                };
                button.Click += (_, _) =>
                {
                    _choice = choice;
                    DialogResult = choice != cancel;
                };
                _buttons.Add(button);
                ButtonPanel.Children.Add(button);
            }

            if (armDelay > TimeSpan.Zero)
            {
                var timer = new DispatcherTimer { Interval = armDelay };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    _buttons.ForEach(button => button.IsEnabled = true);
                    // A disabled button can't take the focus when the window opens: the keyboard starts on Cancel now
                    _buttons.Last().Focus();
                };
                ContentRendered += (_, _) => timer.Start();
            }

            Loaded += (_, _) =>
            {
                Activate();
                LoadIconAsync().FireAndLog("Load the icon of the definition", Log);
            };
        }

        private async Task LoadIconAsync()
        {
            if (_approval.Icon != null || _approval.LoadIcon == null) return;
            var icon = await _approval.LoadIcon().ConfigureAwait(true);
            if (icon != null)
            {
                _approval.Icon = icon;
            }
        }

        /// <summary>
        /// Show the window and wait for the choice (on the UI thread)
        /// </summary>
        /// <param name="owner">the window it belongs to, null: on top of the other windows, centered on the screen</param>
        /// <returns>the index of the chosen button, the last index for Cancel or a closed window</returns>
        public static int Ask(Window owner, DefinitionApproval approval, IReadOnlyList<string> buttons, TimeSpan armDelay)
        {
            var window = new DefinitionApprovalWindow(approval, buttons, armDelay);
            if (owner != null && owner.IsVisible)
            {
                window.Owner = owner;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Topmost = true;
                window.ShowInTaskbar = true;
            }

            window.ShowDialog();
            return window._choice < 0 ? buttons.Count - 1 : window._choice;
        }
    }
}
