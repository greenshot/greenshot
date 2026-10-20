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
        Stamps.Filter = item => ((StampViewModel)item).Stamp.Matches(_searchText);
        Stamps.SortDescriptions.Add(new SortDescription(nameof(StampViewModel.Name), ListSortDirection.Ascending));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static IStampsLanguage Language => Texts.Get<IStampsLanguage>();

    internal StampLibrary Library { get; }

    /// <summary>
    /// The stamps which match the search
    /// </summary>
    public ICollectionView Stamps { get; }

    /// <summary>
    /// The search: every word must be in the name, a tag or the file name
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
        ShowCount();
    }

    /// <summary>
    /// Copy image files into the stamps folder, the last one is selected
    /// </summary>
    /// <returns>the number of files which were added</returns>
    public int AddFiles(IEnumerable<string> files)
    {
        Stamp added = null;
        int count = 0;
        foreach (string file in files ?? Enumerable.Empty<string>())
        {
            try
            {
                added = Library.Import(file);
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
    /// Add the image on the clipboard as PNG stamp
    /// </summary>
    public async Task PasteAsync(CancellationToken cancellationToken = default)
    {
        using var image = await ClipboardService.Current.GetImageAsync(cancellationToken);
        if (image == null)
        {
            return;
        }

        try
        {
            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            ShowAdded(Library.Add(Language.Title, ".png", stream.ToArray()));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or ExternalException)
        {
            Log.Warn("Can't add the image of the clipboard as stamp", ex);
            StatusText = string.Format(Language.AddFailed, Language.Paste, ex.Message);
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
    internal void Update(StampViewModel stamp, string name, string tags)
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
        // The search can match it now, or not anymore
        Stamps.Refresh();
        ShowCount();
    }

    private void ShowAdded(Stamp stamp)
    {
        var item = _items.FirstOrDefault(other => other.Stamp == stamp);
        if (item == null)
        {
            item = new StampViewModel(this, stamp);
            _items.Add(item);
        }

        // A new stamp is shown, also when it doesn't match the search
        SearchText = string.Empty;
        SelectedStamp = item;
        ShowCount();
    }

    private void ShowCount()
    {
        StatusText = string.Format(Language.StampCount, Stamps.Cast<object>().Count(), _items.Count);
        OnPropertyChanged(nameof(EmptyText));
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
