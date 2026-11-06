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
using System.Windows.Data;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Expressions;

namespace Greenshot.Base.Wpf.Views
{
    /// <summary>
    /// What a ${...} can use: the values of Greenshot and the functions (<see cref="ExpressionHelp"/>), in a recipe also its options
    /// and variables. A search filters the list, Enter or a double-click copies what to write. Used by the settings and the recipe editor.
    /// </summary>
    public partial class ExpressionHelpWindow : Window
    {
        /// <summary>One line of the list, with the name of its group in the language of Greenshot</summary>
        public sealed class Item
        {
            public Item(ExpressionHelpEntry entry, string groupName)
            {
                Entry = entry;
                GroupName = groupName;
            }

            public ExpressionHelpEntry Entry { get; }
            public string GroupName { get; }
            public string Expression => Entry.Expression;
            public string Description => Entry.Description;
            public string SpokenText => string.IsNullOrEmpty(Description) ? Expression : Expression + ", " + Description;
        }

        private static ExpressionHelpWindow _open;
        private readonly ListCollectionView _view;

        /// <summary>
        /// Shows the help with these entries; one window at a time, an open one is replaced
        /// </summary>
        /// <param name="entries">What to list</param>
        /// <param name="owner">The window it belongs to</param>
        /// <param name="introduction">The text above the list: what the values are for here, null for none</param>
        public static void ShowFor(IEnumerable<ExpressionHelpEntry> entries, Window owner, string introduction = null)
        {
            _open?.Close();
            var window = new ExpressionHelpWindow(entries, introduction) { Owner = owner };
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_open, window))
                {
                    _open = null;
                }
            };
            _open = window;
            window.Show();
        }

        public ExpressionHelpWindow(IEnumerable<ExpressionHelpEntry> entries, string introduction = null)
        {
            InitializeComponent();
            IntroTextBlock.Text = introduction?.Trim() ?? string.Empty;
            IntroTextBlock.Visibility = string.IsNullOrEmpty(IntroTextBlock.Text) ? Visibility.Collapsed : Visibility.Visible;
            var language = Texts.Get<ICoreLanguage>();
            var items = (entries ?? Enumerable.Empty<ExpressionHelpEntry>()).Select(e => new Item(e, GroupName(e.Group, language))).ToList();
            _view = new ListCollectionView(items);
            _view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Item.GroupName)));
            _view.Filter = o => Matches((Item)o, SearchBox.Text);
            EntryList.ItemsSource = _view;
            Loaded += (s, e) => SearchBox.Focus();
        }

        private static string GroupName(ExpressionHelpGroup group, ICoreLanguage language)
        {
            switch (group)
            {
                case ExpressionHelpGroup.RecipeOption:
                    return language.ExpressionHelpGroupOptions;
                case ExpressionHelpGroup.RecipeVariable:
                    return language.ExpressionHelpGroupVariables;
                case ExpressionHelpGroup.Function:
                    return language.ExpressionHelpGroupFunctions;
                default:
                    return language.ExpressionHelpGroupValues;
            }
        }

        private static bool Matches(Item item, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            return search.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                .All(word => item.Expression.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0
                             || item.Description.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _view.Refresh();
            StatusTextBlock.Text = string.Format(Texts.Get<ICoreLanguage>().ExpressionHelpFound, _view.Count);
            LiveRegion.Announce(StatusTextBlock);
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Down goes from the search into the list, Enter copies the first match
            if (e.Key == Key.Down && _view.Count > 0)
            {
                EntryList.SelectedIndex = 0;
                (EntryList.ItemContainerGenerator.ContainerFromIndex(0) as UIElement)?.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && _view.Count > 0)
            {
                EntryList.SelectedIndex = 0;
                Copy();
                e.Handled = true;
            }
        }

        private void EntryList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Copy();
                e.Handled = true;
            }
        }

        private void EntryList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Copy();

        private void CopyButton_Click(object sender, RoutedEventArgs e) => Copy();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Copy()
        {
            if (!(EntryList.SelectedItem is Item item)) return;
            var language = Texts.Get<ICoreLanguage>();
            StatusTextBlock.Text = ClipboardHelper.TrySetClipboardData(item.Expression, out var error)
                ? string.Format(language.ExpressionHelpCopied, item.Expression)
                : string.Format(language.ExpressionHelpCopyFailed, error);
            LiveRegion.Announce(StatusTextBlock);
        }
    }
}
