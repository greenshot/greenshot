/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Windows.Input;
using Greenshot.Base.Interfaces.Editor;

namespace Greenshot.Editor.Panels.Views
{
    /// <summary>
    /// The WPF part of the panel area: header and the view of the shown panel
    /// </summary>
    public partial class PanelDockView : UserControl
    {
        private bool _updating;

        public PanelDockView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The user picked another panel in the list
        /// </summary>
        public event EventHandler<string> PanelPicked;

        /// <summary>
        /// The user closed the panel area
        /// </summary>
        public event EventHandler CloseRequested;

        /// <summary>
        /// Show the panels and the view of the active one
        /// </summary>
        internal void ShowPanel(IReadOnlyList<IEditorPanel> panels, IEditorPanel active, object view)
        {
            _updating = true;
            try
            {
                bool several = panels.Count > 1;
                PanelList.Visibility = several ? Visibility.Visible : Visibility.Collapsed;
                TitleText.Visibility = several ? Visibility.Collapsed : Visibility.Visible;
                TitleText.Text = active?.Title ?? string.Empty;
                PanelList.ItemsSource = panels.Select(panel => new PanelEntry(panel.Id, panel.Title)).ToList();
                PanelList.SelectedIndex = panels.ToList().IndexOf(active);
                PanelContent.Content = view;
                AutomationProperties.SetName(PanelContent, active?.Title ?? string.Empty);
            }
            finally
            {
                _updating = false;
            }
        }

        /// <summary>
        /// Move the keyboard into the panel: the first control which takes it
        /// </summary>
        internal void FocusContent()
        {
            if (PanelContent.Content is UIElement content && content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
            {
                return;
            }

            PanelContent.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        private void OnPanelListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updating && PanelList.SelectedItem is PanelEntry entry)
            {
                PanelPicked?.Invoke(this, entry.Id);
            }
        }

        /// <summary>
        /// An entry of the panel list, a screen reader reads its title
        /// </summary>
        private sealed class PanelEntry
        {
            public PanelEntry(string id, string title)
            {
                Id = id;
                Title = title;
            }

            public string Id { get; }

            public string Title { get; }

            public override string ToString() => Title;
        }
    }
}
