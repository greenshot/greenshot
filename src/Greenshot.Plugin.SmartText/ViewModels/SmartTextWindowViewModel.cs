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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Plugin.SmartText.Detection;

namespace Greenshot.Plugin.SmartText.ViewModels;

/// <summary>
/// The Smart text window: the capture with the found items, the list to pick from, filters per kind. In copy mode the picked values
/// go to the clipboard, in pick mode (a recipe asks) they are the result of the window. Used on the UI thread.
/// </summary>
public sealed class SmartTextWindowViewModel : INotifyPropertyChanged
{
    private SmartTextItemViewModel _selectedItem;
    private SmartTextFilterViewModel _selectedFilter;
    private string _filterText = string.Empty;
    private string _status;

    /// <param name="items">the found items</param>
    /// <param name="preview">the capture, null: no preview</param>
    /// <param name="allText">the whole OCR text, for Copy all text</param>
    /// <param name="isPickMode">a recipe asks: Use instead of Copy</param>
    public SmartTextWindowViewModel(IReadOnlyList<SmartTextItem> items, ImageSource preview, string allText, bool isPickMode)
    {
        Preview = preview;
        AllText = allText ?? string.Empty;
        IsPickMode = isPickMode;
        int number = 1;
        foreach (var item in items ?? Array.Empty<SmartTextItem>())
        {
            var itemViewModel = new SmartTextItemViewModel(item, number++) { Count = items.Count };
            itemViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SmartTextItemViewModel.IsChecked)) OnCheckedChanged();
            };
            Items.Add(itemViewModel);
        }

        Filters.Add(new SmartTextFilterViewModel(null, string.Format(Language.FilterAll, Items.Count)));
        foreach (var group in Items.GroupBy(item => item.Item.DetectorId, StringComparer.OrdinalIgnoreCase))
        {
            Filters.Add(new SmartTextFilterViewModel(group.Key, string.Format(Language.FilterKind, group.First().Name, group.Count())));
        }

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = Matches;
        SelectedFilter = Filters[0];
        SelectedItem = Items.FirstOrDefault();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static ISmartTextLanguage Language => SmartTextPlugin.Language;

    public ObservableCollection<SmartTextItemViewModel> Items { get; } = new ObservableCollection<SmartTextItemViewModel>();

    /// <summary>The items of the filter, in the list</summary>
    public ICollectionView ItemsView { get; }

    public ObservableCollection<SmartTextFilterViewModel> Filters { get; } = new ObservableCollection<SmartTextFilterViewModel>();

    /// <summary>The chips only help with more than one kind</summary>
    public bool ShowFilters => Filters.Count > 2;

    public ImageSource Preview { get; }

    public bool HasPreview => Preview != null;

    /// <summary>The size of the capture in pixels: the boxes of the items are in pixels of the capture</summary>
    public double PreviewWidth => (Preview as BitmapSource)?.PixelWidth ?? Preview?.Width ?? 0;

    public double PreviewHeight => (Preview as BitmapSource)?.PixelHeight ?? Preview?.Height ?? 0;

    public string AllText { get; }

    public bool IsPickMode { get; }

    public bool HasItems => Items.Count > 0;

    public bool CanCopyAllText => AllText.Length > 0;

    /// <summary>Why the list is empty: nothing found, or no text at all</summary>
    public string EmptyText => AllText.Length == 0 ? Language.NoText : Language.NoItems;

    public SmartTextFilterViewModel SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter == value || value == null) return;
            if (_selectedFilter != null) _selectedFilter.IsSelected = false;
            _selectedFilter = value;
            _selectedFilter.IsSelected = true;
            OnPropertyChanged();
            Refilter();
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            _filterText = value ?? string.Empty;
            OnPropertyChanged();
            Refilter();
        }
    }

    public SmartTextItemViewModel SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value) return;
            if (_selectedItem != null) _selectedItem.IsHighlighted = false;
            _selectedItem = value;
            if (_selectedItem != null) _selectedItem.IsHighlighted = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(PrimaryText));
            OnPropertyChanged(nameof(CanPick));
        }
    }

    public bool HasSelectedItem => SelectedItem != null;

    public int CheckedCount => Items.Count(item => item.IsChecked);

    /// <summary>Copy / Use, or Copy selected (2) / Use selected (2) when items are checked</summary>
    public string PrimaryText
    {
        get
        {
            int count = CheckedCount;
            if (IsPickMode) return count > 0 ? string.Format(Language.UseSelected, count) : Language.Use;
            return count > 0 ? string.Format(Language.CopySelected, count) : Language.Copy;
        }
    }

    public bool CanPick => CheckedCount > 0 || SelectedItem != null;

    /// <summary>A short message for screen readers (live region), e.g. "Copied to the clipboard."</summary>
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// The checked items, in the order of the list, else the selected one; with the corrected values
    /// </summary>
    public IReadOnlyList<SmartTextItem> PickedItems()
    {
        var checkedItems = Items.Where(item => item.IsChecked).Select(item => item.Picked).ToList();
        if (checkedItems.Count > 0) return checkedItems;
        return SelectedItem == null ? Array.Empty<SmartTextItem>() : new[] { SelectedItem.Picked };
    }

    /// <summary>The picked values, one per line</summary>
    public string PickedText() => string.Join(Environment.NewLine, PickedItems().Select(item => item.Value));

    /// <summary>Check or uncheck all items of the filter</summary>
    public void CheckAll()
    {
        var visible = ItemsView.Cast<SmartTextItemViewModel>().ToList();
        bool check = visible.Any(item => !item.IsChecked);
        foreach (var item in visible) item.IsChecked = check;
    }

    private bool Matches(object candidate)
    {
        if (candidate is not SmartTextItemViewModel item) return false;
        if (SelectedFilter?.DetectorId != null && !string.Equals(item.Item.DetectorId, SelectedFilter.DetectorId, StringComparison.OrdinalIgnoreCase)) return false;
        return _filterText.Length == 0
               || item.Value.IndexOf(_filterText, StringComparison.CurrentCultureIgnoreCase) >= 0
               || item.Name.IndexOf(_filterText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    private void Refilter()
    {
        ItemsView?.Refresh();
        if (SelectedItem != null && !Matches(SelectedItem))
        {
            SelectedItem = ItemsView?.Cast<SmartTextItemViewModel>().FirstOrDefault();
        }
    }

    private void OnCheckedChanged()
    {
        OnPropertyChanged(nameof(CheckedCount));
        OnPropertyChanged(nameof(PrimaryText));
        OnPropertyChanged(nameof(CanPick));
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
