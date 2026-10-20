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
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Threading;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Stamps.Library;
using log4net;

namespace Greenshot.Plugin.Stamps.ViewModels;

/// <summary>
/// A stamp in the list: its preview, name and tags. The preview is made when the list shows the stamp first.
/// </summary>
public sealed class StampViewModel : INotifyPropertyChanged
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(StampViewModel));

    /// <summary>
    /// The side of the previews in the list, in pixels
    /// </summary>
    internal const int ThumbnailSize = 64;

    private readonly StampsViewModel _owner;
    private ImageSource _thumbnail;
    private bool _thumbnailRequested;

    internal StampViewModel(StampsViewModel owner, Stamp stamp)
    {
        _owner = owner;
        Stamp = stamp;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    internal Stamp Stamp { get; }

    /// <summary>
    /// The name, a change is saved in stamps.json
    /// </summary>
    public string Name
    {
        get => Stamp.Name;
        set
        {
            if (value == Stamp.Name) return;
            _owner.Update(this, value, TagsText);
        }
    }

    /// <summary>
    /// The tags separated by commas, a change is saved in stamps.json
    /// </summary>
    public string TagsText
    {
        get => StampLibrary.FormatTags(Stamp.Tags);
        set
        {
            if (value == TagsText) return;
            _owner.Update(this, Name, value);
        }
    }

    /// <summary>
    /// The category: the folder in the stamps folder, empty for none. A change moves the file.
    /// </summary>
    public string Category
    {
        get => Stamp.Category;
        set
        {
            if (StampLibrary.NormalizeCategory(value) == Stamp.Category) return;
            _owner.Move(this, value);
        }
    }

    /// <summary>
    /// The category as shown, the stamps without one are in "General"
    /// </summary>
    public string CategoryName => Stamp.Category.Length == 0 ? Texts.Get<IStampsLanguage>().General : Stamp.Category;

    /// <summary>
    /// The format of the file, e.g. SVG or PNG
    /// </summary>
    public string Format => Stamp.Extension.TrimStart('.').ToUpperInvariant();

    /// <summary>
    /// The name, the format and the tags, for the tooltip in the list
    /// </summary>
    public string Description => Stamp.Tags.Count == 0 ? $"{Name} ({Format})" : $"{Name} ({Format}){Environment.NewLine}{TagsText}";

    /// <summary>
    /// The preview, null until it was made (or when the file can't be read)
    /// </summary>
    public ImageSource Thumbnail
    {
        get
        {
            if (!_thumbnailRequested)
            {
                _thumbnailRequested = true;
                // After the list was shown, one preview after the other: a big folder doesn't block the window
                Dispatcher.CurrentDispatcher.InvokeAsync(LoadThumbnail, DispatcherPriority.Background).Task.FireAndLog("Make the preview of a stamp", Log);
            }

            return _thumbnail;
        }
    }

    private void LoadThumbnail()
    {
        try
        {
            _thumbnail = StampImages.CreateThumbnail(Stamp, ThumbnailSize);
        }
        catch (Exception ex)
        {
            Log.Warn($"Can't make the preview of the stamp {Stamp.FullPath}", ex);
        }

        OnPropertyChanged(nameof(Thumbnail));
    }

    /// <summary>
    /// The name, the tags or the category changed
    /// </summary>
    internal void Refresh()
    {
        OnPropertyChanged(nameof(Category));
        OnPropertyChanged(nameof(CategoryName));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(TagsText));
        OnPropertyChanged(nameof(Description));
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// What a screen reader says for the stamp in the list: the name and the format, as shown
    /// </summary>
    public override string ToString() => $"{Name}, {Format}";
}
