/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Greenshot.UI.SelfService
{
    /// <summary>
    /// Base class for a self-service section in the master/detail navigation.
    /// </summary>
    public abstract class SelfServiceSectionViewModel : INotifyPropertyChanged
    {
        private string _badgeText;
        private Brush _badgeBrush;
        private bool _isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Subtitle { get; }
        public abstract string Icon { get; }

        public string BadgeText
        {
            get => _badgeText;
            set
            {
                if (_badgeText != value)
                {
                    _badgeText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasBadge));
                }
            }
        }

        public Brush BadgeBrush
        {
            get => _badgeBrush;
            set
            {
                if (_badgeBrush != value)
                {
                    _badgeBrush = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasBadge => !string.IsNullOrEmpty(BadgeText);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public virtual void OnNavigatedTo()
        {
        }

        public virtual void OnNavigatedFrom()
        {
        }

        public virtual void Refresh()
        {
        }

        public virtual void OnLanguageChanged()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Subtitle));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
