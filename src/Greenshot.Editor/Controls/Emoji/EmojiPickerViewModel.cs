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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Languages;
using Greenshot.Editor.Configuration;

namespace Greenshot.Editor.Controls.Emoji
{
    /// <summary>
    /// The emoji picker: history, categories and search, used by the Selection panel and by the recipe editor.
    /// A pick goes to the history and raises <see cref="Picked"/>, the user of the picker decides what it changes. No UI toolkit here.
    /// </summary>
    public sealed class EmojiPickerViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// How many emoji the history keeps
        /// </summary>
        public const int HistorySize = 24;

        /// <summary>
        /// The most search results which are shown
        /// </summary>
        public const int MaxSearchResults = 240;

        private readonly IEditorConfiguration _configuration;
        private readonly IReadOnlyList<Emojis.Group> _categories;
        private readonly EmojiGroupViewModel _history;
        private readonly IReadOnlyList<EmojiGroupViewModel> _groups;
        private int _columns = 8;
        private int _selectedGroupIndex;
        private string _searchText = string.Empty;
        private string _currentEmoji;
        private IReadOnlyList<IEnumerable<Emojis.Emoji>> _searchRows = Array.Empty<IEnumerable<Emojis.Emoji>>();

        /// <param name="configuration">The history and the last emoji are stored there</param>
        /// <param name="categories">The emoji, null for the Unicode emoji list (emojis.xml)</param>
        public EmojiPickerViewModel(IEditorConfiguration configuration, IReadOnlyList<Emojis.Group> categories = null)
        {
            _configuration = configuration;
            _categories = categories ?? EmojiData.Data.Groups;
            _history = new EmojiGroupViewModel(Texts.Editor.EmojiHistory, "🕒", HistoryEmojis(), _columns);
            _groups = new[] { _history }
                .Concat(_categories.Select(group => new EmojiGroupViewModel(group.Name, group.Icon, group.EmojiList.ToList(), _columns)))
                .ToList();
            // Without history the first category, the history tab is still there
            _selectedGroupIndex = _history.Emojis.Count > 0 ? 0 : Math.Min(1, _groups.Count - 1);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// An emoji was picked, the bool is true when it was picked with the keyboard
        /// </summary>
        public event EventHandler<EmojiPickedEventArgs> Picked;

        /// <summary>
        /// The tabs: the history first, then the categories
        /// </summary>
        public IReadOnlyList<EmojiGroupViewModel> Groups => _groups;

        public int SelectedGroupIndex
        {
            get => _selectedGroupIndex;
            set
            {
                if (value == _selectedGroupIndex)
                {
                    return;
                }

                _selectedGroupIndex = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The emoji which is shown as the current one, e.g. of the selected element
        /// </summary>
        public string CurrentEmoji
        {
            get => _currentEmoji;
            set
            {
                if (value == _currentEmoji)
                {
                    return;
                }

                _currentEmoji = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// How many emoji fit next to each other, the view sets it from its width
        /// </summary>
        public int Columns
        {
            get => _columns;
            set
            {
                value = Math.Max(1, value);
                if (value == _columns)
                {
                    return;
                }

                _columns = value;
                foreach (var group in _groups)
                {
                    group.SetColumns(value);
                }

                UpdateSearch();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Words to find in the (English) names of the emoji and their categories
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                value ??= string.Empty;
                if (value == _searchText)
                {
                    return;
                }

                _searchText = value;
                UpdateSearch();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSearching));
                OnPropertyChanged(nameof(ShowGroups));
            }
        }

        public bool IsSearching => !string.IsNullOrWhiteSpace(_searchText);

        public bool ShowGroups => !IsSearching;

        /// <summary>
        /// The found emoji, in rows of <see cref="Columns"/>
        /// </summary>
        public IReadOnlyList<IEnumerable<Emojis.Emoji>> SearchRows => _searchRows;

        public bool HasNoResults => IsSearching && _searchRows.Count == 0;

        public string NoResultsText => Texts.Editor.EmojiSearchNoResults;

        /// <summary>
        /// The emoji was picked: it goes first in the history
        /// </summary>
        /// <param name="emoji">The emoji</param>
        /// <param name="fromKeyboard">true when it was picked with the keyboard</param>
        public void Pick(string emoji, bool fromKeyboard)
        {
            if (string.IsNullOrEmpty(emoji))
            {
                return;
            }

            _configuration.RecentEmojis = _configuration.RecentEmojis.Where(e => e != emoji).Prepend(emoji).Take(HistorySize).ToList();
            _history.SetEmojis(HistoryEmojis());
            CurrentEmoji = emoji;
            Picked?.Invoke(this, new EmojiPickedEventArgs(emoji, fromKeyboard));
        }

        private IReadOnlyList<Emojis.Emoji> HistoryEmojis()
            => _configuration.RecentEmojis.Take(HistorySize).Select(text => new Emojis.Emoji { Text = text }).ToList();

        /// <summary>
        /// Every word of the search is in the name of the emoji or of its category or sub category, or the emoji itself was typed
        /// </summary>
        internal static IEnumerable<Emojis.Emoji> Find(IEnumerable<Emojis.Group> categories, string searchText)
        {
            var words = (searchText ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return Enumerable.Empty<Emojis.Emoji>();
            }

            return categories
                .SelectMany(group => group.SubGroups.SelectMany(sub => sub.Emojis.Select(emoji => (emoji, names: $"{emoji.Name} {sub.Name} {group.Name}"))))
                .Where(entry => entry.emoji.Text == searchText.Trim()
                                || words.All(word => entry.names.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(entry => entry.emoji);
        }

        private void UpdateSearch()
        {
            _searchRows = IsSearching
                ? new ChunkHelper<Emojis.Emoji>(Find(_categories, _searchText).Take(MaxSearchResults).ToList(), _columns).ToList()
                : Array.Empty<IEnumerable<Emojis.Emoji>>();
            OnPropertyChanged(nameof(SearchRows));
            OnPropertyChanged(nameof(HasNoResults));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// The emoji which was picked
    /// </summary>
    public sealed class EmojiPickedEventArgs : EventArgs
    {
        public EmojiPickedEventArgs(string emoji, bool fromKeyboard)
        {
            Emoji = emoji;
            FromKeyboard = fromKeyboard;
        }

        public string Emoji { get; }

        /// <summary>
        /// Picked with the keyboard: the keyboard stays in the picker, a mouse user goes on with the canvas
        /// </summary>
        public bool FromKeyboard { get; }
    }

    /// <summary>
    /// A tab of the emoji picker: its emoji in rows which fit the width of the panel
    /// </summary>
    public sealed class EmojiGroupViewModel : INotifyPropertyChanged
    {
        private IReadOnlyList<Emojis.Emoji> _emojis;
        private int _columns;

        public EmojiGroupViewModel(string name, string icon, IReadOnlyList<Emojis.Emoji> emojis, int columns)
        {
            Name = name;
            Icon = icon;
            _emojis = emojis;
            _columns = columns;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; }

        public string Icon { get; }

        public IReadOnlyList<Emojis.Emoji> Emojis => _emojis;

        /// <summary>
        /// The emoji in rows, a screen reader reads a row by its emoji
        /// </summary>
        public IReadOnlyList<IEnumerable<Emojis.Emoji>> Rows => new ChunkHelper<Emojis.Emoji>(_emojis, _columns).ToList();

        internal void SetColumns(int columns)
        {
            _columns = columns;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rows)));
        }

        internal void SetEmojis(IReadOnlyList<Emojis.Emoji> emojis)
        {
            _emojis = emojis;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Emojis)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rows)));
        }

        /// <summary>
        /// A screen reader reads a tab by its name
        /// </summary>
        public override string ToString() => Name;
    }
}
