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
using System.ComponentModel;
using System.Windows.Input;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;

namespace Greenshot.Recipes.ViewModels
{
    /// <summary>
    /// The countdown of the Delay step: the seconds left and a button to cancel
    /// </summary>
    public sealed class DelayCountdownViewModel : INotifyPropertyChanged
    {
        private int _secondsLeft;
        private string _announcement = string.Empty;

        public DelayCountdownViewModel(int seconds, Action cancel)
        {
            _secondsLeft = seconds;
            CancelCommand = new RelayCommand(cancel ?? throw new ArgumentNullException(nameof(cancel)));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int SecondsLeft
        {
            get => _secondsLeft;
            set
            {
                if (_secondsLeft == value)
                {
                    return;
                }
                _secondsLeft = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SecondsLeft)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CountdownText)));
            }
        }

        /// <summary>
        /// What the window shows, e.g. "Capture in 3 s"
        /// </summary>
        public string CountdownText => string.Format(Texts.Recipe.DelayCountdown, _secondsLeft);

        /// <summary>
        /// Said once by a screen reader when the countdown starts (a live region): the window never gets the focus
        /// </summary>
        public string Announcement
        {
            get => _announcement;
            private set
            {
                _announcement = value ?? string.Empty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Announcement)));
            }
        }

        public ICommand CancelCommand { get; }

        /// <summary>
        /// Set the text of the live region, once the window is shown
        /// </summary>
        public void Announce() => Announcement = string.Format(Texts.Recipe.DelayCountdownAnnouncement, _secondsLeft);
    }
}
