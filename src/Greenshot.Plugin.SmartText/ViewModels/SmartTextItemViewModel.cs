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
using Greenshot.Plugin.SmartText.Detection;

namespace Greenshot.Plugin.SmartText.ViewModels;

/// <summary>
/// One found item in the Smart text window: its number (also the badge in the preview), the value the user may correct and
/// whether it is picked
/// </summary>
public sealed class SmartTextItemViewModel : INotifyPropertyChanged
{
    private string _value;
    private bool _isChecked;
    private bool _isHighlighted;
    private int _count;

    public SmartTextItemViewModel(SmartTextItem item, int number)
    {
        Item = item;
        Number = number;
        _value = item.Value;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static ISmartTextLanguage Language => SmartTextPlugin.Language;

    public SmartTextItem Item { get; }

    /// <summary>The number of the item, from 1, as on its badge</summary>
    public int Number { get; }

    public string Name => Item.Name;

    /// <summary>The value, changed by the user when OCR misread it</summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AccessibleName));
            OnPropertyChanged(nameof(ActionTarget));
            OnPropertyChanged(nameof(HasAction));
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The item is the selected one of the list: its box in the preview stands out</summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted == value) return;
            _isHighlighted = value;
            OnPropertyChanged();
        }
    }

    /// <summary>How many items there are, for the name a screen reader reads</summary>
    internal int Count
    {
        get => _count;
        set
        {
            _count = value;
            OnPropertyChanged(nameof(AccessibleName));
        }
    }

    /// <summary>The item with the corrected value</summary>
    public SmartTextItem Picked => Item.WithValue(Value);

    /// <summary>What the action opens, null: copy-only</summary>
    public string ActionTarget
    {
        get
        {
            string target = Picked.ActionTarget;
            return SmartTextActions.CanOpen(target) ? target : null;
        }
    }

    public bool HasAction => ActionTarget != null;

    public string ActionText => Item.Action switch
    {
        SmartTextAction.Mail => Language.ActionMail,
        SmartTextAction.Call => Language.ActionCall,
        _ => Language.ActionOpen
    };

    // The box in the preview, in pixels of the capture: the text as if it weren't rotated, turned by Angle around its centre
    public double Left => Item.Box.Unrotated.Left;
    public double Top => Item.Box.Unrotated.Top;
    public double Width => Item.Box.Width;
    public double Height => Item.Box.Height;
    public double Angle => Item.Box.Angle;

    /// <summary>"Phone number: +49301234567, 1 of 9"</summary>
    public string AccessibleName => string.Format(Language.ItemName, Name, Value, Number, Count);

    public override string ToString() => AccessibleName;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
