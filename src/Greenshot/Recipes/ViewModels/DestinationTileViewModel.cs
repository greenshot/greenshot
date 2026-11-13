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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;

namespace Greenshot.Recipes.ViewModels
{
    /// <summary>
    /// A destination in the list of the destination window: its name, icon and the number which picks it with Ctrl
    /// </summary>
    public class DestinationTileViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The most entries which have a number: 1 to 9 and 0 for the tenth
        /// </summary>
        public const int MaxNumbered = 10;

        public event PropertyChangedEventHandler PropertyChanged;

        private ImageSource _iconSource;
        private int _position;
        private bool _shortcutHintVisible;
        private string _title;
        private bool _isExpanded;
        private bool _isShowingEntries;

        public IDestination Destination { get; set; }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        /// <summary>
        /// The entry this one belongs to (an open editor of "Editor", a printer of "Printer"), null for an entry of the list itself
        /// </summary>
        public DestinationTileViewModel Parent { get; set; }

        /// <summary>
        /// The entries below this one (the open editors, the installed printers), empty until they are loaded or when there are none
        /// </summary>
        public ObservableCollection<DestinationTileViewModel> SubEntries { get; } = new ObservableCollection<DestinationTileViewModel>();

        public bool HasSubEntries => SubEntries.Count > 0;

        /// <summary>
        /// False when only the entries below can be chosen (e.g. several printers and no default way to print): the entry itself opens
        /// </summary>
        public bool CanChooseItself => !(HasSubEntries && Destination?.Descriptor?.UseDynamicsOnly == true);

        /// <summary>
        /// True when the user opened the entries below
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                {
                    return;
                }

                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        /// <summary>
        /// True when the user closed the entries below while a search opened them: they stay closed until the search text changes
        /// </summary>
        public bool AutoOpenSuppressed { get; set; }

        /// <summary>
        /// True when the entries below are shown: opened by the user, or by a search which found its text in one of them
        /// </summary>
        public bool IsShowingEntries
        {
            get => _isShowingEntries;
            set
            {
                if (_isShowingEntries == value)
                {
                    return;
                }

                _isShowingEntries = value;
                OnPropertyChanged(nameof(IsShowingEntries));
                OnPropertyChanged(nameof(ToggleGlyph));
                OnPropertyChanged(nameof(ToggleToolTip));
            }
        }

        /// <summary>
        /// The sign at the right of the entry: "&gt;&gt;&gt;" opens the entries below, "&lt;&lt;&lt;" closes them; the entries below have none
        /// </summary>
        public string ToggleGlyph => IsShowingEntries ? "<<<" : ">>>";

        public string ToggleToolTip => IsShowingEntries ? Texts.Recipe.PickerCollapse : Texts.Recipe.PickerExpand;

        public Visibility ToggleVisibility => Parent == null && HasSubEntries ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// The space around the icon: the entries below a destination are indented (their number stays at the left)
        /// </summary>
        public Thickness IconMargin => Parent == null ? new Thickness(4, 0, 10, 0) : new Thickness(32, 0, 10, 0);

        /// <summary>
        /// Call when <see cref="SubEntries"/> was filled
        /// </summary>
        public void NotifySubEntriesChanged()
        {
            OnPropertyChanged(nameof(HasSubEntries));
            OnPropertyChanged(nameof(CanChooseItself));
            OnPropertyChanged(nameof(ToggleVisibility));
        }

        /// <summary>
        /// A short note at the entry, e.g. "Retry" for the destination which failed
        /// </summary>
        public string BadgeText { get; set; }

        public Visibility BadgeVisibility => !string.IsNullOrEmpty(BadgeText) ? Visibility.Visible : Visibility.Collapsed;

        public ImageSource IconSource
        {
            get => _iconSource;
            set
            {
                _iconSource = value;
                OnPropertyChanged(nameof(IconSource));
            }
        }

        /// <summary>
        /// The place in the list as shown (1 is the first), it changes when the list is filtered. 0 when it isn't in the list.
        /// </summary>
        public int Position
        {
            get => _position;
            set
            {
                if (_position == value)
                {
                    return;
                }

                _position = value;
                OnPropertyChanged(nameof(Position));
                OnPropertyChanged(nameof(NumberText));
                OnPropertyChanged(nameof(ShortcutText));
            }
        }

        /// <summary>
        /// The digit which picks the entry: 1 to 9, 0 for the tenth, empty for the others (their key cap shows without a digit)
        /// </summary>
        public string NumberText => Position >= 1 && Position <= MaxNumbered ? (Position % MaxNumbered).ToString() : string.Empty;

        /// <summary>
        /// The keys which pick the entry, e.g. "Ctrl+1"; null when it has no number
        /// </summary>
        public string ShortcutText => NumberText.Length > 0 ? "Ctrl+" + NumberText : null;

        /// <summary>
        /// True while Ctrl is held: the number is underlined to show it is the key
        /// </summary>
        public bool ShortcutHintVisible
        {
            get => _shortcutHintVisible;
            set
            {
                if (_shortcutHintVisible == value)
                {
                    return;
                }

                _shortcutHintVisible = value;
                OnPropertyChanged(nameof(ShortcutHintVisible));
            }
        }

        /// <summary>
        /// Load the icon of the destination, must be started on the UI thread
        /// </summary>
        public async System.Threading.Tasks.Task LoadIconAsync()
        {
            IconSource = await DestinationIcons.GetImageSourceAsync(Destination?.Descriptor?.IconKey).ConfigureAwait(true);
        }

        /// <summary>
        /// What a screen reader says for the entry
        /// </summary>
        public override string ToString() => Title;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
