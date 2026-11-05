/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System.ComponentModel;
using System.Windows.Media;
using Greenshot.Base.Interfaces;

namespace Greenshot.Settings.ViewModels
{
    public class DestinationViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isAllowed = true;
        private ImageSource _iconSource;

        /// <summary>
        /// The registered destination, null for a name which was typed in or is only in the settings
        /// </summary>
        public IDestination Destination { get; set; }

        /// <summary>
        /// The name in the settings, e.g. FileNoDialog
        /// </summary>
        public string Designation { get; set; }
        public string Description { get; set; }
        public ImageSource IconSource
        {
            get => _iconSource;
            set
            {
                if (_iconSource != value)
                {
                    _iconSource = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSource)));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        /// <summary>
        /// The destination is available, unchecked it is in ExcludeDestinations. Not available it can't be selected.
        /// </summary>
        public bool IsAllowed
        {
            get => _isAllowed;
            set
            {
                if (_isAllowed != value)
                {
                    _isAllowed = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAllowed)));
                    if (!value)
                    {
                        IsSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// False when the administrator blocked the destination, or doesn't let the user switch destinations off
        /// </summary>
        public bool CanChangeAllowed { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The text a screen reader reads for the row of the destination list
        /// </summary>
        public override string ToString() => Description ?? base.ToString();
    }
}
