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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Diagnostics;
using Greenshot.Editor.Standalone.Stamps;

namespace Greenshot.Editor.Standalone.ViewModels
{
    /// <summary>
    /// A stamp in the list of stamps, with a small picture of it
    /// </summary>
    public sealed class StampViewModel : ObservableObject
    {
        private Bitmap _thumbnail;

        public StampViewModel(Stamp stamp)
        {
            Stamp = stamp;
        }

        public Stamp Stamp { get; }

        public string Name => Stamp.Name;

        public string ToolTip => Stamp.Category.Length == 0 ? Stamp.Name : $"{Stamp.Name} ({Stamp.Category})";

        public Bitmap Thumbnail
        {
            get => _thumbnail;
            set => Set(ref _thumbnail, value);
        }
    }

    /// <summary>
    /// The stamps of the editor: images which are placed on a document with a click. They come from the stamps folder,
    /// the one the stamps of Greenshot use too.
    /// </summary>
    public sealed class StampsViewModel : ObservableObject
    {
        public const int ThumbnailSize = 64;
        private StampLibrary _library;
        private string _search = string.Empty;
        private int _loads;

        public ObservableCollection<StampViewModel> Stamps { get; } = new();

        public StampLibrary Library => _library ??= new StampLibrary(EditorPaths.StampsDirectory(EditorSettings.Current.StampsPath));

        public string Folder => Library.Directory;

        public bool IsEmpty => Stamps.Count == 0;

        public string Search
        {
            get => _search;
            set
            {
                if (Set(ref _search, value ?? string.Empty))
                {
                    Show();
                }
            }
        }

        /// <summary>
        /// Reads the stamps folder again and shows its stamps
        /// </summary>
        public void Refresh()
        {
            try
            {
                Library.Load();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                EditorLog.Warn($"The stamps folder {Library.Directory} could not be read", ex);
            }

            Show();
        }

        private void Show()
        {
            var known = Stamps.ToDictionary(stamp => stamp.Stamp.FullPath, StringComparer.OrdinalIgnoreCase);
            Stamps.Clear();
            foreach (var stamp in Library.Stamps.Where(stamp => stamp.Matches(_search)))
            {
                // A stamp which was shown keeps its picture
                Stamps.Add(known.TryGetValue(stamp.FullPath, out var shown) && shown.Stamp == stamp ? shown : new StampViewModel(stamp) { Thumbnail = shown?.Thumbnail });
            }

            OnPropertyChanged(nameof(IsEmpty));
            LoadThumbnailsAsync(Stamps.Where(stamp => stamp.Thumbnail == null).ToList()).Forget("Loading the pictures of the stamps");
        }

        /// <summary>
        /// Reads the pictures one after the other, the list is usable meanwhile
        /// </summary>
        private async Task LoadThumbnailsAsync(IReadOnlyList<StampViewModel> stamps)
        {
            int load = ++_loads;
            foreach (var stamp in stamps)
            {
                if (load != _loads)
                {
                    // Another search took over
                    return;
                }

                try
                {
                    var bytes = await File.ReadAllBytesAsync(stamp.Stamp.FullPath);
                    using var stream = new MemoryStream(bytes, false);
                    stamp.Thumbnail = Bitmap.DecodeToHeight(stream, ThumbnailSize);
                }
                catch (Exception ex)
                {
                    // Shown with its name only
                    EditorLog.Debug($"No picture for the stamp {stamp.Stamp.FullPath}: {ex.Message}");
                }
            }
        }
    }
}
