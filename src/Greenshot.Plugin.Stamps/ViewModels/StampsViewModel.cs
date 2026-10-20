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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces.Forms;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Stamps.Library;
using log4net;

namespace Greenshot.Plugin.Stamps.ViewModels;

/// <summary>
/// The stamps window: the stamps of the folder, the search, the selected stamp and what can be done with it
/// </summary>
public sealed class StampsViewModel : INotifyPropertyChanged
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(StampsViewModel));
    private readonly ObservableCollection<StampViewModel> _items = new();
    private readonly HashSet<string> _collapsedCategories = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly Func<IEditorWindow> _activeEditor;
    private string _searchText = string.Empty;
    private StampViewModel _selectedStamp;
    private string _statusText;

    /// <param name="library">the stamps folder</param>
    /// <param name="activeEditor">the editor window the stamps are inserted into, null when none is open</param>
    internal StampsViewModel(StampLibrary library, Func<IEditorWindow> activeEditor)
    {
        Library = library ?? throw new ArgumentNullException(nameof(library));
        _activeEditor = activeEditor ?? throw new ArgumentNullException(nameof(activeEditor));
        Stamps = CollectionViewSource.GetDefaultView(_items);
        Stamps.Filter = item => ((StampViewModel)item).Stamp.Matches(_searchText) && HasFilterTagsOf(((StampViewModel)item).Stamp);
        // The stamps without category first, as "General"
        Stamps.SortDescriptions.Add(new SortDescription(nameof(StampViewModel.Category), ListSortDirection.Ascending));
        Stamps.SortDescriptions.Add(new SortDescription(nameof(StampViewModel.Name), ListSortDirection.Ascending));
        Stamps.GroupDescriptions.Add(new PropertyGroupDescription(nameof(StampViewModel.CategoryName)));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static IStampsLanguage Language => Texts.Get<IStampsLanguage>();

    internal StampLibrary Library { get; }

    /// <summary>
    /// The stamps which match the search
    /// </summary>
    public ICollectionView Stamps { get; }

    /// <summary>
    /// The tags of the filter: only the stamps which have all of them are shown
    /// </summary>
    public ObservableCollection<TagViewModel> FilterTags { get; } = new();

    public bool HasFilterTags => FilterTags.Count > 0;

    /// <summary>
    /// The tags which can be added to the filter
    /// </summary>
    internal IReadOnlyList<string> AvailableFilterTags =>
        Library.Tags.Where(tag => !FilterTags.Any(filter => string.Equals(filter.Name, tag, StringComparison.CurrentCultureIgnoreCase))).ToList();

    /// <summary>
    /// Show only the stamps which also have this tag
    /// </summary>
    internal void AddFilterTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || FilterTags.Any(filter => string.Equals(filter.Name, tag, StringComparison.CurrentCultureIgnoreCase)))
        {
            return;
        }

        FilterTags.Add(new TagViewModel(tag));
        OnFilterTagsChanged();
    }

    internal void RemoveFilterTag(TagViewModel tag)
    {
        if (tag != null && FilterTags.Remove(tag))
        {
            OnFilterTagsChanged();
        }
    }

    internal void ClearFilterTags()
    {
        if (FilterTags.Count == 0) return;
        FilterTags.Clear();
        OnFilterTagsChanged();
    }

    private void OnFilterTagsChanged()
    {
        OnPropertyChanged(nameof(HasFilterTags));
        Stamps.Refresh();
        ShowCount();
    }

    private bool HasFilterTagsOf(Stamp stamp) =>
        FilterTags.All(filter => stamp.Tags.Contains(filter.Name, StringComparer.CurrentCultureIgnoreCase));

    /// <summary>
    /// The tags of the list which the stamp doesn't have yet
    /// </summary>
    internal IReadOnlyList<string> AvailableTagsFor(StampViewModel stamp) =>
        stamp == null ? Array.Empty<string>() : Library.Tags.Where(tag => !stamp.Stamp.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase)).ToList();

    /// <summary>
    /// Give the stamp a tag of the list
    /// </summary>
    internal void AddTag(StampViewModel stamp, string tag)
    {
        if (stamp == null || string.IsNullOrEmpty(tag)) return;
        Update(stamp, stamp.Name, stamp.Stamp.Tags.Append(tag));
    }

    internal void RemoveTag(StampViewModel stamp, TagViewModel tag)
    {
        if (stamp == null || tag == null) return;
        Update(stamp, stamp.Name, stamp.Stamp.Tags.Where(other => !string.Equals(other, tag.Name, StringComparison.CurrentCultureIgnoreCase)));
    }

    /// <summary>
    /// Add a new tag to the list of tags
    /// </summary>
    /// <returns>the tag as it is in the list, null when it can't be added</returns>
    internal string CreateTag(string tag)
    {
        try
        {
            return Library.AddTag(tag);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Can't add the tag to the list in stamps.json", ex);
            StatusText = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// The categories which have stamps, offered for the category of the selected stamp
    /// </summary>
    public ObservableCollection<string> Categories { get; } = new();

    /// <summary>
    /// The category new stamps go into when they are added with the buttons or pasted: the one of the selected stamp
    /// </summary>
    internal string CurrentCategory => _selectedStamp?.Stamp.Category ?? string.Empty;

    /// <summary>
    /// The search: every word must be in the name, a tag, the category or the file name
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            value ??= string.Empty;
            if (value == _searchText) return;
            _searchText = value;
            OnPropertyChanged();
            Stamps.Refresh();
            ShowCount();
        }
    }

    /// <summary>
    /// The stamp the buttons and the name and tags fields are for
    /// </summary>
    public StampViewModel SelectedStamp
    {
        get => _selectedStamp;
        set
        {
            if (value == _selectedStamp) return;
            _selectedStamp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanInsert));
        }
    }

    public bool HasSelection => _selectedStamp != null;

    /// <summary>
    /// Is the group of the category shown closed: only while there is no search, a search shows all it finds
    /// </summary>
    internal bool IsCollapsed(string categoryName) => string.IsNullOrWhiteSpace(_searchText) && categoryName != null && _collapsedCategories.Contains(categoryName);

    /// <summary>
    /// The user opened or closed the group of the category, kept while the window is open
    /// </summary>
    internal void SetCollapsed(string categoryName, bool collapsed)
    {
        // Opened by a search: that doesn't change what the user chose
        if (categoryName == null || !string.IsNullOrWhiteSpace(_searchText))
        {
            return;
        }

        if (collapsed)
        {
            _collapsedCategories.Add(categoryName);
        }
        else
        {
            _collapsedCategories.Remove(categoryName);
        }
    }

    /// <summary>
    /// A stamp is selected and an editor is open to insert it into
    /// </summary>
    public bool CanInsert => _selectedStamp != null && _activeEditor() != null;

    /// <summary>
    /// What the insert button does, or why it can't
    /// </summary>
    public string InsertToolTip => _activeEditor() != null ? Language.InsertTooltip : Language.NoEditor;

    /// <summary>
    /// Shown instead of the list when it is empty: there are no stamps yet, or none matches the search
    /// </summary>
    public string EmptyText => _items.Count == 0 ? Language.Empty : Stamps.IsEmpty ? Language.NoMatch : null;

    /// <summary>
    /// The number of stamps, or what was done last; read by a screen reader when it changes
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (value == _statusText) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Read the stamps folder again, the selection is kept when the stamp is still there
    /// </summary>
    public void Reload()
    {
        string selectedKey = _selectedStamp?.Stamp.Key;
        try
        {
            Library.Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't read the stamps folder {Library.Directory}", ex);
            StatusText = ex.Message;
        }

        _items.Clear();
        foreach (var stamp in Library.Stamps)
        {
            _items.Add(new StampViewModel(this, stamp));
        }

        SelectedStamp = _items.FirstOrDefault(item => item.Stamp.Key == selectedKey);
        UpdateCategories();
        ShowCount();
    }

    /// <summary>
    /// The editor window which was active last changed, or none is open anymore
    /// </summary>
    internal void OnActiveEditorChanged()
    {
        OnPropertyChanged(nameof(CanInsert));
        OnPropertyChanged(nameof(InsertToolTip));
    }

    /// <summary>
    /// Insert the stamp into the capture of the editor window which was active last, and give that window the keyboard
    /// </summary>
    /// <returns>true when it was inserted</returns>
    public bool Insert(StampViewModel stamp)
    {
        var editor = _activeEditor();
        var surface = editor?.CurrentSurface;
        if (stamp == null || surface == null)
        {
            StatusText = Language.NoEditor;
            return false;
        }

        try
        {
            var elements = StampImages.LoadElements(stamp.Stamp, surface);
            if (elements.Count == 0)
            {
                StatusText = string.Format(Language.InsertFailed, stamp.Name, Path.GetFileName(stamp.Stamp.FullPath));
                return false;
            }

            surface.InsertElements(elements);
        }
        catch (Exception ex)
        {
            Log.Warn($"Can't insert the stamp {stamp.Stamp.FullPath}", ex);
            StatusText = string.Format(Language.InsertFailed, stamp.Name, ex.Message);
            return false;
        }

        StatusText = string.Format(Language.Inserted, stamp.Name);
        // The new element is selected there, the keys move and size it
        editor.Activate();
        return true;
    }

    /// <summary>
    /// Put the stamp on the clipboard, for other programs
    /// </summary>
    public async Task CopyAsync(StampViewModel stamp, CancellationToken cancellationToken = default)
    {
        if (stamp == null) return;
        try
        {
            await StampImages.CopyToClipboardAsync(stamp.Stamp, cancellationToken);
            StatusText = string.Format(Language.Copied, stamp.Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warn($"Can't copy the stamp {stamp.Stamp.FullPath}", ex);
            StatusText = ex.Message;
        }
    }

    /// <summary>
    /// Delete the stamp and its file, the user confirmed it
    /// </summary>
    public void Delete(StampViewModel stamp)
    {
        if (stamp == null) return;
        int index = Stamps.Cast<StampViewModel>().ToList().IndexOf(stamp);
        try
        {
            Library.Delete(stamp.Stamp);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't delete the stamp {stamp.Stamp.FullPath}", ex);
            StatusText = ex.Message;
            return;
        }

        _items.Remove(stamp);
        // The next stamp, so Delete can be pressed again
        var visible = Stamps.Cast<StampViewModel>().ToList();
        SelectedStamp = visible.Count == 0 ? null : visible[Math.Min(Math.Max(index, 0), visible.Count - 1)];
        UpdateCategories();
        ShowCount();
    }

    /// <summary>
    /// Copy image files into the stamps folder, the last one is selected. A folder adds its images as category with its name,
    /// its sub folders as categories in it. Other files are skipped.
    /// </summary>
    /// <param name="paths">the files and folders</param>
    /// <param name="category">the category the files (and the categories of the folders) go into, empty for none</param>
    /// <returns>the number of files which were added</returns>
    public int AddFiles(IEnumerable<string> paths, string category = "")
    {
        Stamp added = null;
        int count = 0;
        foreach (var (file, fileCategory) in ExpandFolders(paths, category))
        {
            try
            {
                added = Library.Import(file, fileCategory);
                count++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                Log.Warn($"Can't add {file} as stamp", ex);
                StatusText = string.Format(Language.AddFailed, Path.GetFileName(file), ex.Message);
            }
        }

        if (added != null)
        {
            ShowAdded(added);
        }

        return count;
    }

    /// <summary>
    /// The stamp files of the paths with their category: a file in the category, the files of a folder in the category of the folder
    /// </summary>
    private IEnumerable<(string File, string Category)> ExpandFolders(IEnumerable<string> paths, string category)
    {
        foreach (string path in paths ?? Enumerable.Empty<string>())
        {
            if (!Directory.Exists(path))
            {
                if (Library.IsStampFile(path))
                {
                    yield return (path, category);
                }

                continue;
            }

            string folder = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string folderCategory = StampLibrary.NormalizeCategory($"{category}/{Path.GetFileName(folder)}");
            List<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(Library.IsStampFile).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn($"Can't read the folder {folder}", ex);
                StatusText = string.Format(Language.AddFailed, Path.GetFileName(folder), ex.Message);
                continue;
            }

            foreach (string file in files)
            {
                string relativeFolder = Path.GetDirectoryName(file)?.Substring(folder.Length) ?? string.Empty;
                yield return (file, StampLibrary.NormalizeCategory($"{folderCategory}/{relativeFolder}"));
            }
        }
    }

    /// <summary>
    /// Add image files in their own format, e.g. dropped from a browser, the last one is selected
    /// </summary>
    /// <returns>the number of files which were added</returns>
    public int AddContents(IEnumerable<ImageFileContent> files, string category = "")
    {
        Stamp added = null;
        int count = 0;
        foreach (var file in files ?? Enumerable.Empty<ImageFileContent>())
        {
            try
            {
                added = Library.Add(Path.GetFileNameWithoutExtension(file.FileName), file.Extension, file.Content, category);
                count++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                Log.Warn($"Can't add {file.FileName} as stamp", ex);
                StatusText = string.Format(Language.AddFailed, file.FileName, ex.Message);
            }
        }

        if (added != null)
        {
            ShowAdded(added);
        }

        return count;
    }

    /// <summary>
    /// Add what was dropped: the files and folders, else the image files of a browser, else the image downloaded from its url
    /// in its own format, else the image as PNG.
    /// </summary>
    /// <param name="drop">what was dropped, read during the drop; disposed here</param>
    /// <param name="category">the category of the group it was dropped on, empty for none</param>
    /// <returns>the number of stamps which were added</returns>
    internal async Task<int> AddDropAsync(DroppedImages drop, string category, CancellationToken cancellationToken = default)
    {
        using (drop)
        {
            if (drop.Paths.Count > 0)
            {
                return AddFiles(drop.Paths, category);
            }

            if (drop.Files.Count > 0)
            {
                return AddContents(drop.Files, category);
            }

            if (drop.Urls.Count > 0)
            {
                StatusText = Language.Downloading;
                foreach (string url in drop.Urls)
                {
                    var downloaded = await ImageDownloader.DownloadImageFileAsync(url, cancellationToken);
                    if (downloaded != null && Library.Extensions.Contains(downloaded.Extension))
                    {
                        return AddContents(new[] { downloaded }, category);
                    }
                }
            }

            if (drop.Image != null)
            {
                string name = drop.Urls.Count > 0 && Uri.TryCreate(drop.Urls[0], UriKind.Absolute, out var uri)
                    ? Path.GetFileNameWithoutExtension(ImageFileContent.FileNameOf(uri, ".png"))
                    : Language.Title;
                return AddImage(drop.Image, name, category) ? 1 : 0;
            }

            StatusText = Language.DropFailed;
            return 0;
        }
    }

    /// <summary>
    /// Move the stamp into another category, see <see cref="StampLibrary.NormalizeCategory"/>
    /// </summary>
    internal void Move(StampViewModel stamp, string category)
    {
        try
        {
            Library.Move(stamp.Stamp, category);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't move the stamp {stamp.Stamp.FullPath} to the category {category}", ex);
            StatusText = ex.Message;
        }

        stamp.Refresh();
        // Into the group of its new category, still selected
        SetCollapsed(stamp.CategoryName, false);
        RefreshKeepingSelection(stamp);
        UpdateCategories();
        ShowCount();
    }

    /// <summary>
    /// Add the image on the clipboard as PNG stamp
    /// </summary>
    public async Task PasteAsync(CancellationToken cancellationToken = default)
    {
        using var image = await ClipboardService.Current.GetImageAsync(cancellationToken);
        if (image == null)
        {
            return;
        }

        AddImage(image, Language.Title, CurrentCategory);
    }

    /// <summary>
    /// Add the image as PNG stamp
    /// </summary>
    /// <returns>true when it was added</returns>
    private bool AddImage(Image image, string name, string category)
    {
        try
        {
            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            ShowAdded(Library.Add(name, ".png", stream.ToArray(), category));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException)
        {
            Log.Warn("Can't add the image as stamp", ex);
            StatusText = string.Format(Language.AddFailed, name, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Show the stamps folder in Explorer, with the file of the selected stamp selected
    /// </summary>
    public void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(Library.Directory);
            ExplorerHelper.OpenInExplorer(_selectedStamp?.Stamp.FullPath ?? Library.Directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException)
        {
            Log.Warn($"Can't open the stamps folder {Library.Directory}", ex);
            StatusText = ex.Message;
        }
    }

    /// <summary>
    /// Save a new name or new tags of the stamp
    /// </summary>
    internal void Update(StampViewModel stamp, string name, IEnumerable<string> tags)
    {
        try
        {
            Library.Update(stamp.Stamp, name, tags);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Can't save the name and tags of the stamp {stamp.Stamp.FullPath}", ex);
            StatusText = ex.Message;
        }

        stamp.Refresh();
        // The search and the tag filter can match it now, or not anymore
        RefreshKeepingSelection(stamp);
        ShowCount();
    }

    /// <summary>
    /// Filter, sort and group again; the refresh can take the selection of the list, so the stamp is selected again when it is still shown
    /// </summary>
    private void RefreshKeepingSelection(StampViewModel stamp)
    {
        Stamps.Refresh();
        SelectedStamp = null;
        SelectedStamp = Stamps.Cast<StampViewModel>().Contains(stamp) ? stamp : null;
    }

    private void ShowAdded(Stamp stamp)
    {
        var item = _items.FirstOrDefault(other => other.Stamp == stamp);
        if (item == null)
        {
            item = new StampViewModel(this, stamp);
            _items.Add(item);
        }

        // A new stamp is shown, also when it doesn't match the search or its group is closed
        SearchText = string.Empty;
        SetCollapsed(item.CategoryName, false);
        UpdateCategories();
        Stamps.Refresh();
        SelectedStamp = item;
        ShowCount();
    }

    private void UpdateCategories()
    {
        var categories = Library.Categories;
        if (categories.SequenceEqual(Categories))
        {
            return;
        }

        Categories.Clear();
        foreach (string category in categories)
        {
            Categories.Add(category);
        }
    }

    private void ShowCount()
    {
        StatusText = string.Format(Language.StampCount, Stamps.Cast<object>().Count(), _items.Count);
        OnPropertyChanged(nameof(EmptyText));
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
