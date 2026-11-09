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

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Greenshot.Plugin.SmartText.ViewModels;

/// <summary>
/// A filter chip of the Smart text window: all items, or the items of one detector
/// </summary>
public sealed class SmartTextFilterViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public SmartTextFilterViewModel(string detectorId, string text)
    {
        DetectorId = detectorId;
        Text = text;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>The detector, null: all items</summary>
    public string DetectorId { get; }

    public string Text { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public override string ToString() => Text;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
